using System;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using AutoGen.Core;
using StageEngine.Core.AI.Agents;
using StageEngine.Core.AI.Agents.Prompts;
using StageEngine.Core.Conversations;
using StageEngine.Core.Data;
using StageEngine.Core.Game.Session;
using StageEngine.Core.Game.Staging.Serialization;
using StageEngine.Core.Players;
using StageEngine.Core.Snapshots;
using StageEngine.Core.Utility;

namespace StageEngine.Core.Game.Staging
{
    public abstract class Turn<T> : ISerializableTurn where T : SessionData
    {
        [JsonIgnore]
        protected T SessionData { get; private set; }
        [JsonIgnore]
        protected SignedLogger Logger { get; private set; }
        [JsonIgnore]
        protected AgentsManager AgentsManager { get; private set; }
        protected PromptsManager PromptsManager { get; private set; }
        protected ConversationsManager ConversationsManager { get; private set; }
        //protected PresentationManager presentationManager;
        //protected SoundManager soundManager;

        public string SceneName { get; protected set; }

        protected Player actingPlayer;
        protected Player lastSpeaker;

        protected Func<string, bool> Validator { get; set; }
        protected Action PostMessage { get; set; }

        public Turn()
        {
            
        }
        
        public Turn(Player actingPlayer) : this()
        {
            this.actingPlayer = actingPlayer;
        }

        public virtual void Initialize()
        {
            SceneName = "Pending";
            SessionData = SessionManager.Instance.GetSessionData<T>();
            Logger = SignedLogger.GetInstance(GetType());
            AgentsManager = AgentsManager.Instance;
            PromptsManager = PromptsManager.Instance;
            ConversationsManager = ConversationsManager.Instance;
            //presentationManager = PresentationManager.Instance;
            //soundManager = SoundManager.Instance;
        }

        public virtual async Task ExecuteAsync()
        {
            Logger.Log($"Starting execution of the task(s)");

            SubsribeToMessages();

            try
            {
                await Process();
            }
            finally
            {
                UnsubscriveFromMessages();
            }
        }

        public abstract Task Process();

        protected virtual async Task<bool> SendTask(string taskType, string taskText, int maxRounds)
        {
            bool success = await AgentsManager.SendTaskAsync(taskText, maxRounds);

            if (success)
            {
                Logger.Log($"Successfully completed {taskType} task for {actingPlayer.Name}");
            }
            else
            {
                Logger.LogError($"Failed to complete {taskType} task for {actingPlayer.Name}");
            }

            return success;
        }

        private void SubsribeToMessages()
        {
            AgentsManager.OnMessageReceivedAsync += OnMessageReceivedAsync;
            AgentsManager.OnTaskCompleted += OnTaskCompleted;
        }
        private void UnsubscriveFromMessages()
        {
            AgentsManager.OnMessageReceivedAsync -= OnMessageReceivedAsync;
            AgentsManager.OnTaskCompleted -= OnTaskCompleted;
        }

        protected virtual async Task OnMessageReceivedAsync(IMessage message)
        {
            Logger.Log($"Processing message from {message?.From}");

            if (message?.From == null) return;

            lastSpeaker = SessionData.Players.Where(p => p.Name.Equals(message?.From)).FirstOrDefault();
            if (lastSpeaker == null)
            {
                Logger.LogError($"No player found with name '{message.From}' in SessionData.Players");
                return;
            }
            var text = message.GetContent();

            if (Validator?.Invoke(text) ?? true)
            {
                await ConversationsManager.AddMessage(lastSpeaker.Name, text, lastSpeaker);
                PostMessage?.Invoke();

                Logger.Log($"Finished processing dialogue for {lastSpeaker.Name}");
            }
            else
            {
                Logger.LogError($"Processing dialogue for {lastSpeaker.Name} failed validation");
            }
        }

        protected virtual void OnTaskCompleted()
        {
            Logger.Log("Task completed - all messages have been fully processed");
        }

        public virtual TurnState GetState()
        {
            return new TurnState
            {
                TypeName = GetType().Name,
                SceneName = SceneName,
                ActingPlayerName = actingPlayer?.Name
            };
        }

        public virtual void RestoreState(TurnState state)
        {
            SceneName = state.SceneName;
            
            if (!string.IsNullOrEmpty(state.ActingPlayerName))
            {
                actingPlayer = SessionManager.Instance.Data.Players.FirstOrDefault(p => p.Name == state.ActingPlayerName);
            }
        }
    }
}
