using System.Collections.Generic;
using System.Text.Json.Serialization;
using StageEngine.Core.AI.Agents;
using StageEngine.Core.AI.Tts;
using StageEngine.Core.Utility;
using UnityEngine;

namespace StageEngine.Core.Players
{
    public class Player
    {
        public string Id { get; set; }

        public string Name { get; set; }

        [JsonIgnore]
        public Color Color { get; set; }

        public string ColorHex
        {
            get
            {
                return ColorsHelper.ColorToHex(Color);
            }
            set
            {
                Color = ColorsHelper.HexToColor(value);
            }
        }

        public string Introduction { get; set; }

        // Loaded from a separate file.
        [JsonIgnore]
        public string Personality { get; set; }

        [JsonIgnore]
        public List<Memory> Memories { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public ModelProvider ModelProvider { get; set; }

        public string ModelId { get; set; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public VoiceProvider VoiceProvider { get; set; }

        public string VoiceId { get; set; }
    }
}
