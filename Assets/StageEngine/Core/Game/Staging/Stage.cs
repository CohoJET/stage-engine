using System.Collections.Generic;
using System.Threading.Tasks;
using StageEngine.Core.Data;
using StageEngine.Core.Game.Session;
using StageEngine.Core.Game.Staging.Serialization;
using StageEngine.Core.Snapshots;

namespace StageEngine.Core.Game.Staging
{
    public abstract class Stage<T> : ISerializableStage where T : SessionData
    {
        protected T sessionData;

        public List<Turn<T>> Turns { get; set; }
        public int CurrentTurn { get; set; }
        public bool IsComplete => CurrentTurn >= Turns.Count;

        public Stage()
        {
            Turns = new List<Turn<T>>();
            CurrentTurn = 0;
        }

        public virtual void Initialize()
        {
            sessionData = SessionManager.Instance.GetSessionData<T>();

            foreach (var turn in Turns)
            {
                turn.Initialize();
            }
        }

        public abstract void Setup();

        public async Task<bool> ExecuteNextTurn()
        {
            if (IsComplete) return false;

            //PresentationManager.Instance.UpdateScene(CurrentTurn + 1, Turns[CurrentTurn].SceneName, 0, 0);
            await Turns[CurrentTurn].ExecuteAsync();
            CurrentTurn++;
            
            // Create snapshot if requested.
            var snapshotsManager = SnapshotsManager.Instance;
            if (snapshotsManager.AutomaticSnapshots || snapshotsManager.SnapshotArmed)
            {
                snapshotsManager.CreateSnapshot();
                snapshotsManager.SnapshotArmed = false;
            }

            return true;
        }

        public virtual StageState GetState()
        {
            var state = new StageState
            {
                TypeName = GetType().Name,
                CurrentTurn = CurrentTurn
            };

            foreach (var turn in Turns)
            {
                if (turn is ISerializableTurn serializableTurn)
                {
                    state.Turns.Add(serializableTurn.GetState());
                }
            }

            return state;
        }

        public virtual void RestoreState(StageState state)
        {
            CurrentTurn = state.CurrentTurn;

            for (int i = 0; i < Turns.Count && i < state.Turns.Count; i++)
            {
                if (Turns[i] is ISerializableTurn serializableTurn)
                {
                    serializableTurn.RestoreState(state.Turns[i]);
                }
            }
        }
    }
}
