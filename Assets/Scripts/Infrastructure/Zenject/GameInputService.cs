using UnityEngine;

public sealed class GameInputService : IGameInputService
{
    private readonly PlayerInputReader _reader;

    public GameInputService(PlayerInputReader reader)
    {
        _reader = reader;
    }

    public Vector2 Move => _reader.Move;
    public bool SprintHeld => _reader.SprintHeld;
    public bool JumpPressed => _reader.JumpPressed;
}
