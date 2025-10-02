using System;
using UnityEngine;

namespace StageEngine.Core.AI.Agents.Prompts
{
    [Serializable]
    public struct Prompt
    {
        public string name;
        
        [TextArea(5, 100)]
        public string text;
    }
}
