using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour
{
    [Header("Input Actions")]
    [SerializeField] private InputActionAsset _inputActions;

    private InputActionMap _playerMap;
    private InputActionMap _uiMap;

    private InputAction _move;
    private InputAction _look;
    private InputAction _jump;
    private InputAction _sprint;
    private InputAction _aim;
    private InputAction _fire;
    private InputAction _reload;
    private InputAction _holster;
    private InputAction _weapon1;
    private InputAction _weapon2;
    private InputAction _pause;

    private bool _gameplayEnabled = true;

    public Vector2 Move => _move != null ? _move.ReadValue<Vector2>() : Vector2.zero;
    public Vector2 Look => _look != null ? _look.ReadValue<Vector2>() : Vector2.zero;
    public bool JumpPressed => _jump != null && _jump.WasPressedThisFrame();
    public bool SprintHeld => _sprint != null && _sprint.IsPressed();
    public bool AimHeld => _aim != null && _aim.IsPressed();
    public bool FireHeld => _fire != null && _fire.IsPressed();
    public bool ReloadPressed => _reload != null && _reload.WasPressedThisFrame();
    public bool HolsterPressed => _holster != null && _holster.WasPressedThisFrame();
    
    public bool Weapon1Pressed =>
        _gameplayEnabled &&
        ((_weapon1 != null && _weapon1.WasPressedThisFrame()) ||
         (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame));

    public bool Weapon2Pressed =>
        _gameplayEnabled &&
        ((_weapon2 != null && _weapon2.WasPressedThisFrame()) ||
         (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame));

    public bool PausePressed => _pause != null && _pause.WasPressedThisFrame();

    private void Awake()
    {
        if (_inputActions == null)
        {
            Debug.LogError("TPSControls.inputactions is not assigned to PlayerInputReader.", this);
            enabled = false;
            return;
        }

        _playerMap = _inputActions.FindActionMap("Player", true);
        _uiMap = _inputActions.FindActionMap("UI", true);
        _move = _playerMap.FindAction("Move", true);
        _look = _playerMap.FindAction("Look", true);
        _jump = _playerMap.FindAction("Jump", true);
        _sprint = _playerMap.FindAction("Sprint", true);
        _aim = _playerMap.FindAction("Aim", true);
        _fire = _playerMap.FindAction("Fire", true);
        _reload = _playerMap.FindAction("Reload", true);
        _holster = _playerMap.FindAction("Holster", true);
        _weapon1 = _playerMap.FindAction("Weapon1", true);
        _weapon2 = _playerMap.FindAction("Weapon2", true);
        _pause = _uiMap.FindAction("Pause", true);
    }

    private void OnEnable()
    {
        if (_gameplayEnabled) _playerMap?.Enable();
        _uiMap?.Enable();
    }

    private void OnDisable()
    {
        _playerMap?.Disable();
        _uiMap?.Disable();
    }

    public void SetGameplayEnabled(bool enabled)
    {
        _gameplayEnabled = enabled;
        if (_playerMap == null) return;
        if (enabled && isActiveAndEnabled) _playerMap.Enable();
        else _playerMap.Disable();
    }
}
