using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using StageEngine.Core.Utility;
using StageEngine.Core.AI.Tts.ElevenLabs;
using StageEngine.Core.Players;
using System.Linq;

namespace StageEngine.Core.Conversations
{
    public class ConversationsManager : Singleton<ConversationsManager>
    {
        private List<ConversationMessage> messages = new List<ConversationMessage>();

        [SerializeField]
        private float typingSpeedCharsPerSecond = 30f;
        [SerializeField]
        private float afterMessageDelay = 1;
        [SerializeField]
        private bool useTts;

        private ElevenLabsManager ElevenLabsManager { get; set; }

        public event Action<ConversationMessage> OnMessageAdded;
        public event Action<int, ConversationMessage> OnMessageSet;
        public event Action<int> OnMessageRemoved;
        public event Action OnMessagesCleared;
        public event Action<int, string> OnMessageContentUpdated;
        
        public IReadOnlyList<ConversationMessage> Messages => messages.AsReadOnly();

        public void Start()
        {
            ElevenLabsManager = ElevenLabsManager.Instance;   
        }

        public async Task AddMessage(string sender, string content, Player player = null, bool skipTts = false)
        {
            // Clean content from junk.
            content = StringHelper.RemoveTagsAndCleanupPreserveParagraphs(content);

            var message = new ConversationMessage
            {
                Sender = sender,
                FlairColor = player == null ? new Color(0,0,0,0) : player.Color,
                Content = "",
                Timestamp = DateTime.Now
            };

            float typewritingTime = 0;
            if (!skipTts && useTts)
            {
                VoiceConfiguration voiceConfiguration = null;
                if (player != null)
                {
                    voiceConfiguration = ElevenLabsManager.vocies.Where(v => v.name.Equals(player.VoiceId)).First();
                }
                else
                {
                    voiceConfiguration = ElevenLabsManager.vocies.Where(v => v.directorOnly == true).First();
                }
                typewritingTime = await ElevenLabsManager.GenerateAndPlaySpeech(content, voiceConfiguration) * 0.9f;
            }

            messages.Add(message);
            Logger.Log($"Added message from {message.Sender}: {content}");
            OnMessageAdded?.Invoke(message);
            
            await StartTypewritingEffect(messages.Count - 1, content, typewritingTime);
            await Task.Delay((int)(afterMessageDelay * 1000));
        }
        
        public void SetMessage(int index, ConversationMessage message)
        {
            if (index < 0 || index >= messages.Count || message == null) return;
            
            messages[index] = message;
            Logger.Log($"Set message at index {index}");
            OnMessageSet?.Invoke(index, message);
        }
        
        public void RemoveMessage(int index)
        {
            if (index < 0 || index >= messages.Count) return;
            
            messages.RemoveAt(index);
            Logger.Log($"Removed message at index {index}");
            OnMessageRemoved?.Invoke(index);
        }
        
        public void RemoveMessage(ConversationMessage message)
        {
            int index = messages.IndexOf(message);
            if (index >= 0)
            {
                RemoveMessage(index);
            }
        }
        
        public void ClearMessages()
        {
            messages.Clear();
            Logger.Log("Cleared all messages");
            OnMessagesCleared?.Invoke();
        }
        
        public ConversationMessage GetMessage(int index)
        {
            if (index < 0 || index >= messages.Count) return null;
            return messages[index];
        }
        
        public int GetMessageCount()
        {
            return messages.Count;
        }
        
        private async Task StartTypewritingEffect(int messageIndex, string fullContent, float customPrintTime = 0)
        {
            if (messageIndex < 0 || messageIndex >= messages.Count || string.IsNullOrEmpty(fullContent))
                return;

            float delayPerChar = customPrintTime == 0 ? 1f / typingSpeedCharsPerSecond : customPrintTime / fullContent.Length;
            
            for (int i = 0; i <= fullContent.Length; i++)
            {
                string currentContent = fullContent.Substring(0, i);
                messages[messageIndex].Content = currentContent;
                OnMessageContentUpdated?.Invoke(messageIndex, currentContent);
                
                if (i < fullContent.Length)
                {
                    await Task.Delay((int)(delayPerChar * 1000));
                }
            }
        }
    }
}