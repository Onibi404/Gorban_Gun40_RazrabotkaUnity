using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseMenuController : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject _pausePanel;

    [Header("Player")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private CharacterAiming _aiming;
    [SerializeField] private CharacterLocomotion _locomotion;
    [SerializeField] private ActiveWeapon _activeWeapon;
    [SerializeField] private ReloadWeapon _reloadWeapon;

    private PlayerInputReader _inputReader;
    private bool _paused;

    private bool _aimingEnabled;
    private bool _locomotionEnabled;
    private bool _weaponEnabled;
    private bool _reloadEnabled;

    private void Awake()
    {
        _pausePanel.SetActive(false);
        _paused = false;
        if (_locomotion != null)
            _inputReader = _locomotion.GetComponent<PlayerInputReader>();
        if (_inputReader == null)
            Debug.LogError("PauseMenuController: PlayerInputReader not found on player.", this);
    }

    private void Update()
    {
        if (_inputReader == null || !_inputReader.PausePressed)
            return;

        if (_paused)
        {
            Resume();
            return;
        }

        // Do not open pause on victory or defeat.
        if (Time.timeScale == 0f)
            return;

        if (_playerHealth != null && _playerHealth.IsDead())
            return;

        Pause();
    }

    public void Pause()
    {
        if (_paused || Time.timeScale == 0f)
            return;

        _paused = true;

        if (_activeWeapon != null)
        {
            RaycastWeapon weapon = _activeWeapon.GetActiveWeapon();

            if (weapon != null)
                weapon.StopFiring();
        }

        _aimingEnabled = _aiming != null && _aiming.enabled;
        _locomotionEnabled = _locomotion != null && _locomotion.enabled;
        _weaponEnabled = _activeWeapon != null && _activeWeapon.enabled;
        _reloadEnabled = _reloadWeapon != null && _reloadWeapon.enabled;

        if (_aiming != null) _aiming.enabled = false;
        if (_locomotion != null) _locomotion.enabled = false;
        if (_activeWeapon != null) _activeWeapon.enabled = false;
        if (_reloadWeapon != null) _reloadWeapon.enabled = false;

        _inputReader.SetGameplayEnabled(false);
        Time.timeScale = 0f;

        _pausePanel.SetActive(true);

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Resume()
    {
        if (!_paused)
            return;

        _paused = false;
        _pausePanel.SetActive(false);

        Time.timeScale = 1f;
        _inputReader.SetGameplayEnabled(true);

        if (_aiming != null) _aiming.enabled = _aimingEnabled;
        if (_locomotion != null) _locomotion.enabled = _locomotionEnabled;
        if (_activeWeapon != null) _activeWeapon.enabled = _weaponEnabled;
        if (_reloadWeapon != null) _reloadWeapon.enabled = _reloadEnabled;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;

        SceneManager.LoadScene(
            SceneManager.GetActiveScene().name
        );
    }

    public void GoToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }
}

