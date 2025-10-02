namespace StageEngine.Core.Game.Staging.Serialization
{
    public interface ISerializableStage
    {
        StageState GetState();
        void RestoreState(StageState state);
    }
}