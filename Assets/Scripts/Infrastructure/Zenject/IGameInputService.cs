using UnityEngine;

public interface IGameInputService
{
    Vector2 Move { get; }
    bool SprintHeld { get; }
    bool JumpPressed { get; }
}
