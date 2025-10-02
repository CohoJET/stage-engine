namespace StageEngine.Core.Game.Staging.Serialization
{
    public interface ISerializableRunner
    {
        RunnerState GetState();
        void RestoreState(RunnerState state);
    }
}