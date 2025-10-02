using AutoGen;
using AutoGen.Anthropic;
using AutoGen.Anthropic.Extensions;
using AutoGen.Anthropic.Utils;
using AutoGen.Core;
using AutoGen.Gemini;
using AutoGen.OpenAI;
using AutoGen.OpenAI.Extension;
using OpenAI;
using StageEngine.Core.AI.Agents.Prompts;
using StageEngine.Core.Players;
using StageEngine.Core.Utility;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace StageEngine.Core.AI.Agents
{
    public class AgentsManager : Singleton<AgentsManager>
    {
        private static readonly string CONFIGURATION_FILE = "agents.json";

        [Header("API Configuration")]
        [SerializeField] private string anthropicApiKey = "";
        [SerializeField] private string openAiApiKey = "";
        [SerializeField] private string googleApi = "";

        [Header("Timeout Configuration")]
        [SerializeField] private int messageTimeoutSeconds = 30;
        [SerializeField] private int maxRetries = 3;
        [SerializeField] private int retryDelayMs = 1000;

        private GroupChat gameChat;
        private IAgent proxyAgent;
        private bool isProcessingTask = false;
        private CancellationTokenSource currentTaskCancellation;
        private readonly List<HttpClient> httpClients = new List<HttpClient>();

        // Message history
        private readonly List<IMessage> messageHistory = new List<IMessage>();
        private readonly object historyLock = new object();

        // Sequential processing queue
        private readonly ConcurrentQueue<IMessage> incomingMessages = new ConcurrentQueue<IMessage>();
        private volatile bool receivingComplete = false;
        private TaskCompletionSource<bool> messageProcessingCompletion;

        // Events
        public event Func<IMessage, Task> OnMessageReceivedAsync;
        public event Action<string, string> OnAgentMessage; // sender, content
        public event Action OnTaskCompleted;

        public bool IsProcessingTask => isProcessingTask;
        public int HistoryCount => messageHistory.Count;
        public int PendingMessages => incomingMessages.Count;

        private void Start()
        {
            LoadConfiguration();
        }

        private void LoadConfiguration()
        {
            try
            {
                if (File.Exists(CONFIGURATION_FILE))
                {
                    var config = JsonSerializer.Deserialize<AgentsConfiguration>(File.ReadAllText(CONFIGURATION_FILE));
                    if (config != null)
                    {
                        anthropicApiKey = config.AnthropicApiKey ?? anthropicApiKey;
                        openAiApiKey = config.OpenAIApiKey ?? openAiApiKey;
                        googleApi = config.GoogleApiKey ?? googleApi;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to load configuration: {ex.Message}");
            }
        }

        public void Initialize(List<Player> players)
        {
            // Ensure configuration is loaded
            LoadConfiguration();
            
            if (string.IsNullOrEmpty(anthropicApiKey) || string.IsNullOrEmpty(openAiApiKey))
            {
                Logger.LogError("API keys not set!");
                return;
            }

            try
            {
                var agents = new List<IAgent>();
                var playerList = string.Join(", ", players.Select(p => p.Name));

                // Create proxy agent for system routing
                proxyAgent = CreateProxyAgent(playerList);
                agents.Add(proxyAgent);

                // Create player agents
                foreach (var player in players)
                {
                    var systemMessage = PromptsManager.Instance.GeneratePrompt("system", player.Name, player.Personality, playerList);

                    try
                    {
                        var agent = player.ModelProvider switch
                        {
                            ModelProvider.Anthropic => CreateAnthropicPlayerAgent(player.Name, player.ModelId, systemMessage),
                            ModelProvider.OpenAI => CreateOpenAIPlayerAgent(player.Name, player.ModelId, systemMessage),
                            ModelProvider.Google => CreateGooglePlayerAgent(player.Name, player.ModelId, systemMessage),
                            _ => throw new ArgumentException($"Unknown agent type: {player.ModelProvider}")
                        };
                        agents.Add(agent);
                        Logger.Log($"Created {player.ModelProvider} agent: {player.Name}");
                    }
                    catch (Exception ex)
                    {
                        Logger.LogError($"Failed to create agent {player.Name}: {ex.Message}");
                        throw;
                    }
                }

                var workflow = AgentsGraphHelper.GenerateWorkflow(agents);
                gameChat = new GroupChat(agents, workflow: workflow);

                Logger.Log($"Initialized with {agents.Count} agents");
            }
            catch (Exception ex)
            {
                Logger.LogError($"Initialization failed: {ex.Message}");
                throw;
            }
        }

        public async Task<bool> SendTaskAsync(string content, int maxRounds = 5, CancellationToken cancellationToken = default)
        {
            if (gameChat == null || proxyAgent == null)
            {
                Logger.LogError("Not initialized");
                return false;
            }

            if (isProcessingTask)
            {
                Logger.LogWarning("Task already running");
                return false;
            }

            isProcessingTask = true;
            receivingComplete = false;
            messageProcessingCompletion = new TaskCompletionSource<bool>();
            currentTaskCancellation?.Dispose();
            currentTaskCancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            try
            {
                // Purge [SYSTEM] messages and add new task
                PurgeSystemMessages();
                var taskMessage = new TextMessage(Role.Assistant, content, from: proxyAgent.Name);
                AddToHistory(taskMessage);

                Logger.Log($"Sent: '{content}' from {proxyAgent.Name}");

                // Start concurrent receiving and sequential processing
                var receivingTask = ReceiveMessagesAsync(maxRounds, currentTaskCancellation.Token);
                var processingTask = ProcessMessageQueueAsync(currentTaskCancellation.Token);

                await Task.WhenAll(receivingTask, processingTask);
                return receivingTask.Result;
            }
            catch (OperationCanceledException)
            {
                Logger.Log("Task cancelled");
                return false;
            }
            catch (Exception ex)
            {
                Logger.LogError($"Task error: {ex.GetType()} {ex.Message} | {ex.StackTrace}");
                return false;
            }
            finally
            {
                CleanupTask();
            }
        }

        private async Task<bool> ReceiveMessagesAsync(int maxRounds, CancellationToken cancellationToken)
        {
            List<IMessage> currentHistory;
            lock (historyLock)
            {
                currentHistory = new List<IMessage>(messageHistory);
            }
            
            Exception lastException = null;
            
            for (int attempt = 0; attempt < maxRetries; attempt++)
            {
                if (attempt > 0)
                {
                    Logger.Log($"Retrying... Attempt {attempt + 1}/{maxRetries}");
                    try
                    {
                        await Task.Delay(retryDelayMs, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        receivingComplete = true;
                        throw;
                    }
                }


                try
                {
                    var messageReceived = false;
                    using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                    timeoutCts.CancelAfter(TimeSpan.FromSeconds(messageTimeoutSeconds));

                    await foreach (var response in gameChat.SendAsync(currentHistory, maxRound: maxRounds)
                        .WithCancellation(timeoutCts.Token))
                    {
                        if (response != null)
                        {
                            messageReceived = true;

                            // Reset timeout on each message received
                            timeoutCts.CancelAfter(TimeSpan.FromSeconds(messageTimeoutSeconds));

                            AddToHistory(response);
                            incomingMessages.Enqueue(response);
                            Logger.Log($"Received: '{response.GetContent()}' from {response.From}");
                        }
                    }

                    if (messageReceived)
                    {
                        receivingComplete = true;
                        return true;
                    }

                    Logger.LogWarning($"No messages received on attempt {attempt + 1}");
                }
                catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
                {
                    Logger.LogWarning($"Timeout on attempt {attempt + 1}");
                    lastException = new TimeoutException($"Message timeout after {messageTimeoutSeconds} seconds");
                }
                catch (OperationCanceledException)
                {
                    receivingComplete = true;
                    throw;
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error on attempt {attempt + 1}: {ex.GetType()} {ex.Message} | {ex.StackTrace}");

                    lastException = ex;
                    if (attempt == maxRetries - 1)
                        break;
                }
            }

            receivingComplete = true;
            var errorMsg = "All retry attempts exhausted with no response";
            if (lastException != null)
                errorMsg += $". Last error: {lastException.Message}";
            Logger.LogError(errorMsg);
            return false;
        }

        private async Task ProcessMessageQueueAsync(CancellationToken cancellationToken)
        {
            try
            {
                // Wait for messages to arrive - timeout is handled by ReceiveMessagesAsync
                while (!receivingComplete && incomingMessages.IsEmpty)
                {
                    await Task.Delay(10, cancellationToken);
                }

                while (!cancellationToken.IsCancellationRequested)
                {
                    if (incomingMessages.TryDequeue(out IMessage message))
                    {
                        try
                        {
                            await ProcessMessageAsync(message, cancellationToken);
                        }
                        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                        {
                            Logger.Log("Message processing cancelled");
                            break;
                        }
                        catch (Exception ex)
                        {
                            Logger.LogError($"Error processing message: {ex.Message}");
                        }
                    }
                    else if (receivingComplete)
                    {
                        break;
                    }
                    else
                    {
                        await Task.Delay(10, cancellationToken);
                    }
                }
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                Logger.Log("Message queue processing cancelled");
            }
            finally
            {
                // Ensure task completion is signaled
                UnityMainThreadDispatcher.Instance.Enqueue(() => OnTaskCompleted?.Invoke());
            }
        }

        private async Task ProcessMessageAsync(IMessage message, CancellationToken cancellationToken)
        {
            var sender = message.From ?? "Unknown";
            var content = message.GetContent();

            var completion = new TaskCompletionSource<bool>();

            // Register cancellation callback for the main task cancellation only
            using var registration = cancellationToken.Register(() =>
            {
                completion.TrySetCanceled();
            });

            UnityMainThreadDispatcher.Instance.Enqueue(async () =>
            {
                try
                {
                    OnAgentMessage?.Invoke(sender, content);

                    if (OnMessageReceivedAsync != null)
                        await OnMessageReceivedAsync.Invoke(message);

                    completion.TrySetResult(true);
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Message processing error for {sender}: {ex.Message}");
                    completion.TrySetException(ex);
                }
            });

            // Wait for receiver to finish processing this message
            await completion.Task;
        }

        private void PurgeSystemMessages()
        {
            lock (historyLock)
            {
                messageHistory.RemoveAll(msg => msg.GetContent().Contains("[SYSTEM]"));
            }
        }

        public void AddToHistory(IMessage message)
        {
            lock (historyLock)
            {
                messageHistory.Add(message);
            }
        }

        public void RemoveFromHistory(IMessage message)
        {
            lock (historyLock)
            {
                messageHistory.Remove(message);
            }
        }

        public List<IMessage> GetHistory()
        {
            lock (historyLock)
            {
                return new List<IMessage>(messageHistory);
            }
        }

        public void ClearHistory()
        {
            lock (historyLock)
            {
                messageHistory.Clear();
            }
        }

        public AgentsMessageHistory ExtractMessageHistory()
        {
            lock (historyLock)
            {
                var history = new AgentsMessageHistory();
                foreach (var message in messageHistory)
                {
                    history.Messages.Add(new SerializableMessage(message));
                }
                return history;
            }
        }

        public void RestoreMessageHistory(AgentsMessageHistory history)
        {
            if (history?.Messages == null) return;

            lock (historyLock)
            {
                messageHistory.Clear();
                foreach (var serializableMessage in history.Messages)
                {
                    messageHistory.Add(serializableMessage.ToMessage());
                }
            }
            Logger.Log($"Finished message history restoration with: {messageHistory.Count} messages");
        }

        public void CancelCurrentTask()
        {
            try
            {
                currentTaskCancellation?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // Ignore - already disposed
            }
        }

        private void CleanupTask()
        {
            isProcessingTask = false;
            receivingComplete = false;

            // Clear remaining messages
            while (incomingMessages.TryDequeue(out _)) { }

            currentTaskCancellation?.Dispose();
            currentTaskCancellation = null;
            messageProcessingCompletion = null;
        }

        private IAgent CreateProxyAgent(string playerList)
        {
            var defaultResponse = $"Routing to: {playerList.Split(',')[0].Trim()}";
            return new UserProxyAgent(
                name: "Proxy",
                humanInputMode: HumanInputMode.NEVER,
                defaultReply: defaultResponse);
        }

        private IAgent CreateAnthropicPlayerAgent(string agentName, string modelId, string systemMessage)
        {
            var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
            httpClients.Add(httpClient);
            
            var client = new AnthropicClient(httpClient, AnthropicConstants.Endpoint, anthropicApiKey);
            return new AnthropicClientAgent(client, agentName, modelId,
                systemMessage: systemMessage,
                temperature: 0.9m)
                .RegisterMessageConnector()
                .RegisterPrintMessage();
        }

        private IAgent CreateOpenAIPlayerAgent(string agentName, string modelId, string systemMessage)
        {
            var client = new OpenAIClient(openAiApiKey);
            return new OpenAIChatAgent(client.GetChatClient(modelId), agentName,
                systemMessage: systemMessage,
                temperature: 0.9f)
                .RegisterMessageConnector()
                .RegisterPrintMessage();
        }

        private IAgent CreateGooglePlayerAgent(string agentName, string modelId, string systemMessage)
        {
            var client = new GoogleGeminiClient(googleApi);
            return new GeminiChatAgent(client, agentName, modelId,
                systemMessage: systemMessage)
                .RegisterMessageConnector()
                .RegisterPrintMessage();
        }

        private void OnDestroy()
        {
            CancelCurrentTask();
            CleanupTask();
            
            // Dispose all HttpClients
            foreach (var client in httpClients)
            {
                try
                {
                    client?.Dispose();
                }
                catch (Exception ex)
                {
                    Logger.LogError($"Error disposing HttpClient: {ex.Message}");
                }
            }
            httpClients.Clear();
        }
    }
}