public interface IGameLevelState
{
    bool IsFinished { get; }
    bool IsVictory { get; }
    bool TryComplete(bool isVictory);
}
