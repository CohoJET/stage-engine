namespace StageEngine.Core.Game.Staging.Serialization
{
    public interface ISerializableTurn
    {
        TurnState GetState();
        void RestoreState(TurnState state);
    }
}