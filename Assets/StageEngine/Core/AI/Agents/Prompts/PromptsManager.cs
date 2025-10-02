using System.Collections.Generic;
using System.Linq;
using StageEngine.Core.Utility;
using UnityEngine;

namespace StageEngine.Core.AI.Agents.Prompts
{
    public class PromptsManager : Singleton<PromptsManager>
    {
        [SerializeField]
        private List<Prompt> registeredPrompts;

        public string GeneratePrompt(string name, params object[] args)
        {
            return string.Format(registeredPrompts.Where(p => p.name.Equals(name)).First().text, args);
        }
    }
}
