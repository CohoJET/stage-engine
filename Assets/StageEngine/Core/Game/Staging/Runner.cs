using System.Collections.Generic;
using System.Threading.Tasks;
using StageEngine.Core.Data;
using StageEngine.Core.Game.Staging.Serialization;

namespace StageEngine.Core.Game.Staging
{
    public class Runner<T> : ISerializableRunner where T : SessionData
    {
        private List<Stage<T>> Stages { get; set; }
        private int CurrentStage { get; set; }

        public Runner()
        {
            Stages = new List<Stage<T>>();
        }

        public void Initialize()
        {
            foreach (var stage in Stages)
            {
                stage.Initialize();
            }
        }

        public void AddStage(Stage<T> stage)
        {
            stage.Setup();
            Stages.Add(stage);
        }

        public async Task RunAsync()
        {
            while (CurrentStage < Stages.Count)
            {
                var stage = Stages[CurrentStage];

                while (!stage.IsComplete)
                {
                    await stage.ExecuteNextTurn();
                }

                CurrentStage++;
            }
        }
        public async Task ExecuteNextTurn()
        {
            if (CurrentStage >= Stages.Count) return;

            var stage = Stages[CurrentStage];
            var executed = await stage.ExecuteNextTurn();

            if (!executed)
            {
                CurrentStage++;
            }
        }

        public RunnerState GetState()
        {
            var state = new RunnerState
            {
                CurrentStage = CurrentStage
            };

            foreach (var stage in Stages)
            {
                if (stage is ISerializableStage serializableStage)
                {
                    state.Stages.Add(serializableStage.GetState());
                }
            }

            return state;
        }

        public void RestoreState(RunnerState state)
        {
            CurrentStage = state.CurrentStage;

            for (int i = 0; i < Stages.Count && i < state.Stages.Count; i++)
            {
                if (Stages[i] is ISerializableStage serializableStage)
                {
                    serializableStage.RestoreState(state.Stages[i]);
                }
            }
        }
    }
}
