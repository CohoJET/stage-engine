using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using StageEngine.Core.Utility;
using UnityEngine;

namespace StageEngine.Core.Players
{
    public class PlayersManager : Singleton<PlayersManager>
    {
        private readonly static string DEFAULT_PLAYERS_FOLDER = "DefaultPlayers";
        private readonly static string PLAYERS_FOLDER = "Players";

        public List<Player> Players { get; protected set; }

        public void Start()
        {
            LoadPlayers();
        }

        private void LoadPlayers()
        {
            Players = new List<Player>();

            bool forceDefaultPlayers = false;

#if UNITY_EDITOR
            forceDefaultPlayers = true;
#endif

            if (!forceDefaultPlayers && Directory.Exists(PLAYERS_FOLDER) && Directory.GetFiles(PLAYERS_FOLDER, "*.json").Length > 0)
            {
                Logger.Log("Loading PRODUCTION player files.");
                throw new NotImplementedException();
            }
            else
            {
                Logger.Log("Loading DEFAULT player files.");
                LoadDefaultPlayers();
            }
        }
        private void LoadDefaultPlayers()
        {
            var files = Resources.LoadAll<TextAsset>(DEFAULT_PLAYERS_FOLDER);

            foreach (var file in files)
            {
                if (file.name.EndsWith("-data"))
                {
                    try
                    {
                        var player = JsonSerializer.Deserialize<Player>(file.text);
                        player.Personality = files.Where(f => f.name.Contains($"{player.Id}-personality")).First().text;
                        Players.Add(player);
                        Logger.Log($"Loaded {player.Name}'s data.");
                    }
                    catch (Exception e)
                    {
                        Logger.LogError($"Failed to load {file.name}: {e.Message}");
                    }
                }
            }
        }
    }
}
