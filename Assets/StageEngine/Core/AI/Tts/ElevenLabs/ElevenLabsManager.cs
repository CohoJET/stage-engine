using ElevenLabs;
using StageEngine.Core.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using UnityEngine;

namespace StageEngine.Core.AI.Tts.ElevenLabs
{
    public class ElevenLabsManager : Singleton<ElevenLabsManager>
    {
        private static readonly string CONFIGURATION_FILE = "elevenlabs.json";

        [Header("API")]
        public string apiKey;
        public string modelId;

        [Header("Library")]
        public List<VoiceConfiguration> vocies = new List<VoiceConfiguration>();

        [Header("Audio Settings")]
        public TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat outputFormat =
            TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Mp344100128;
        [Range(0, 4)]
        public int optimizeStreamingLatency = 1;

        private ElevenLabsClient elevenLabsClient;
        private TextToSpeechClient client;
        private AudioSource audioSource;
        private bool isProcessing = false;

        private void Start()
        {
            if (File.Exists(CONFIGURATION_FILE))
            {
                var config = JsonSerializer.Deserialize<ElevenLabsConfiguration>(File.ReadAllText(CONFIGURATION_FILE));
                apiKey = config.ApiKey;
                modelId = config.ModelId;
            }

            // Get or create AudioSource
                audioSource = GetComponent<AudioSource>();
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            // Initialize ElevenLabs client
            InitializeClient();
        }

        private void InitializeClient()
        {
            try
            {
                if (apiKey.Equals(string.Empty) || modelId.Equals(string.Empty))
                {
                    throw new Exception("Configuration is invalid");
                }

                elevenLabsClient = new ElevenLabsClient(apiKey);
                client = elevenLabsClient.TextToSpeech;
                Logger.Log("ElevenLabs client initialized successfully");
            }
            catch (Exception e)
            {
                Logger.LogError($"Failed to initialize ElevenLabs client: {e.Message}");
            }
        }

        public async Task<float> GenerateAndPlaySpeech(string text, VoiceConfiguration voice, string previousText = null, string nextText = null)
        {
            isProcessing = true;
            Logger.Log($"Generating speech for: \"{text.Substring(0, Mathf.Min(50, text.Length))}...\"");

            try
            {
                // Create voice settings
                var voiceSettings = new VoiceSettingsResponseModel
                {
                    Speed = voice.speed,
                    Stability = voice.stability,
                    SimilarityBoost = voice.similarity,
                    Style = voice.style,
                    UseSpeakerBoost = voice.useSpeakerBoost,
                };

                // Generate speech
                var audioBytes = await client.CreateTextToSpeechByVoiceIdStreamAsync(
                    voiceId: voice.voiceId,
                    previousText: previousText,
                    text: text,
                    nextText: nextText,
                    enableLogging: true,
                    optimizeStreamingLatency: optimizeStreamingLatency,
                    outputFormat: outputFormat,
                    modelId: modelId,
                    voiceSettings: voiceSettings
                );

                Logger.Log($"Speech generated successfully! Audio size: {audioBytes.Length} bytes");

                // Convert and play audio
                return await PlayAudioBytes(audioBytes);
            }
            catch (Exception e)
            {
                Logger.LogError($"Speech generation failed: {e.Message}");
            }
            finally
            {
                isProcessing = false;
            }
            return 0;
        }

        private async Task<float> PlayAudioBytes(byte[] audioBytes)
        {
            try
            {
                AudioClip audioClip = null;

                if (IsMP3Format(outputFormat))
                {
                    audioClip = await ConvertMP3ToAudioClip(audioBytes);
                }
                else if (IsPCMFormat(outputFormat))
                {
                    audioClip = ConvertPCMToAudioClip(audioBytes, GetSampleRate(outputFormat));
                }
                else
                {
                    Logger.LogWarning($"Unsupported audio format: {outputFormat}");
                    return 0;
                }

                if (audioClip != null)
                {
                    audioSource.clip = audioClip;
                    audioSource.Play();
                    Logger.Log($"Playing audio clip - Duration: {audioClip.length:F2}s");
                    return audioClip.length;
                }
                else
                {
                    Logger.LogError("Failed to convert audio bytes to AudioClip");
                }
            }
            catch (Exception e)
            {
                Logger.LogError($"Audio playback failed: {e.Message}");
            }
            return 0;
        }

        private async Task<AudioClip> ConvertMP3ToAudioClip(byte[] mp3Bytes)
        {
            string tempPath = System.IO.Path.Combine(Application.temporaryCachePath, $"elevenlabs_audio_{System.DateTime.Now.Ticks}.mp3");
            System.IO.File.WriteAllBytes(tempPath, mp3Bytes);

            using (var www = UnityEngine.Networking.UnityWebRequestMultimedia.GetAudioClip($"file://{tempPath}", AudioType.MPEG))
            {
                var operation = www.SendWebRequest();

                while (!operation.isDone)
                {
                    await Task.Delay(10);
                }

                try
                {
                    if (www.result == UnityEngine.Networking.UnityWebRequest.Result.Success)
                    {
                        var audioClip = UnityEngine.Networking.DownloadHandlerAudioClip.GetContent(www);
                        return audioClip;
                    }
                    else
                    {
                        Logger.LogError($"Failed to load MP3: {www.error}");
                        return null;
                    }
                }
                finally
                {
                    try { System.IO.File.Delete(tempPath); } catch { }
                }
            }
        }
        private AudioClip ConvertPCMToAudioClip(byte[] pcmBytes, int sampleRate)
        {
            float[] samples = new float[pcmBytes.Length / 2];

            for (int i = 0; i < samples.Length; i++)
            {
                short sample = (short)((pcmBytes[i * 2 + 1] << 8) | pcmBytes[i * 2]);
                samples[i] = sample / 32768.0f;
            }

            AudioClip clip = AudioClip.Create("ElevenLabsAudio", samples.Length, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        private bool IsMP3Format(TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat format)
        {
            return format.ToString().StartsWith("Mp3");
        }
        private bool IsPCMFormat(TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat format)
        {
            return format.ToString().StartsWith("Pcm");
        }
        private int GetSampleRate(TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat format)
        {
            return format switch
            {
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Mp32205032 => 22050,
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Pcm8000 => 8000,
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Pcm16000 => 16000,
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Pcm22050 => 22050,
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Pcm24000 => 24000,
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Pcm44100 => 44100,
                TextToSpeechStreamingV1TextToSpeechVoiceIdStreamPostOutputFormat.Pcm48000 => 48000,
                _ => 44100
            };
        }

        void OnDestroy()
        {
            elevenLabsClient?.Dispose();
        }
    }
}
