public sealed class GameLevelState : IGameLevelState
{
    public bool IsFinished { get; private set; }
    public bool IsVictory { get; private set; }

    public bool TryComplete(bool isVictory)
    {
        if (IsFinished) return false;
        IsFinished = true;
        IsVictory = isVictory;
        return true;
    }
}
