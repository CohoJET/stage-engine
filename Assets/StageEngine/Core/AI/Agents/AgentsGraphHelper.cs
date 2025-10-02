using System.Collections.Generic;
using System.Linq;
using AutoGen.Core;

namespace StageEngine.Core.AI.Agents
{
    public static class AgentsGraphHelper
    {
        public static Graph GenerateWorkflow(IEnumerable<IAgent> agents)
        {
            var agentsList = agents.ToList();
            var transitions = new List<Transition>();

            // Create transitions from every agent to every other agent
            foreach (var fromAgent in agentsList)
            {
                foreach (var toAgent in agentsList)
                {
                    var transition = Transition.Create(
                        from: fromAgent,
                        to: toAgent,
                        canTransitionAsync: async (from, to, messages) =>
                        {
                            var lastMessage = messages.LastOrDefault();
                            if (lastMessage is TextMessage textMessage)
                            {
                                // Check if the message contains the tag for the target agent
                                var targetTag = $"[N:{to.Name}]";
                                return textMessage.Content.Contains(targetTag);
                            }
                            return false;
                        });

                    transitions.Add(transition);
                }
            }

            return new Graph(transitions);
        }
    }
}
