using System.Collections.Generic;

namespace StageEngine.Core.Game.Staging.Serialization
{
    public class RunnerState
    {
        public int CurrentStage { get; set; }
        public List<StageState> Stages { get; set; }

        public RunnerState()
        {
            Stages = new List<StageState>();
        }
    }

    public class StageState
    {
        public string TypeName { get; set; }
        public int CurrentTurn { get; set; }
        public List<TurnState> Turns { get; set; }

        public StageState()
        {
            Turns = new List<TurnState>();
        }
    }

    public class TurnState
    {
        public string TypeName { get; set; }
        public string SceneName { get; set; }
        public string ActingPlayerName { get; set; }
    }
}