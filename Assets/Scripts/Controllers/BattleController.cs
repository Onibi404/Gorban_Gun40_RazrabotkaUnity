using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class BattleController : MonoBehaviour
{
    [Header("Игровые объекты")]
    [SerializeField] private Battlefield battlefield;
    [SerializeField] private PlayerController playerController;

    [Header("Интерфейс")]
    [SerializeField] private BattleUI battleUI;

    [Header("Перезапуск")]
    [SerializeField] private float restartHoldDuration = 1.5f;

    private CheckersCommand checkersCommand;
    private GameInputActions inputActions;

    private bool isRestartHolding;
    private float restartHoldTime;

    public PlayerTeam CurrentTeam =>
        checkersCommand?.CurrentTeam
        ?? PlayerTeam.White;

    private void Awake()
    {
        inputActions =
            new GameInputActions();
    }

    private void OnEnable()
    {
        inputActions.Gameplay.Confirm.performed +=
            OnConfirmPerformed;

        inputActions.Gameplay.Cancel.performed +=
            OnCancelPerformed;

        inputActions.Gameplay.Restart.started +=
            OnRestartStarted;

        inputActions.Gameplay.Restart.canceled +=
            OnRestartCanceled;

        inputActions.Gameplay.Enable();
    }

    private void Start()
    {
        checkersCommand =
            new CheckersCommand(
                battlefield,
                playerController);

        checkersCommand.TurnChanged +=
            OnTurnChanged;

        battlefield.Initialize(this);

        battleUI?.SetCurrentTeam(
            checkersCommand.CurrentTeam);
    }

    private void Update()
    {
        UpdateRestartProgress();
    }

    private void OnDisable()
    {
        inputActions.Gameplay.Confirm.performed -=
            OnConfirmPerformed;

        inputActions.Gameplay.Cancel.performed -=
            OnCancelPerformed;

        inputActions.Gameplay.Restart.started -=
            OnRestartStarted;

        inputActions.Gameplay.Restart.canceled -=
            OnRestartCanceled;

        inputActions.Gameplay.Disable();
    }

    private void OnDestroy()
    {
        if (checkersCommand != null)
        {
            checkersCommand.TurnChanged -=
                OnTurnChanged;
        }

        inputActions?.Dispose();
    }

    private void OnConfirmPerformed(
        InputAction.CallbackContext context)
    {
        checkersCommand?.Confirm();
    }

    private void OnCancelPerformed(
        InputAction.CallbackContext context)
    {
        checkersCommand?.Cancel();
    }

    private void OnRestartStarted(
        InputAction.CallbackContext context)
    {
        isRestartHolding = true;
        restartHoldTime = 0f;

        battleUI?.SetRestartProgress(0f);
        battleUI?.ShowRestartProgress(true);
    }

    private void OnRestartCanceled(
        InputAction.CallbackContext context)
    {
        CancelRestart();
    }

    private void UpdateRestartProgress()
    {
        if (!isRestartHolding)
            return;

        restartHoldTime +=
            Time.unscaledDeltaTime;

        float duration =
            Mathf.Max(
                0.1f,
                restartHoldDuration);

        float progress =
            restartHoldTime / duration;

        battleUI?.SetRestartProgress(
            progress);

        if (progress >= 1f)
        {
            RestartScene();
        }
    }

    private void CancelRestart()
    {
        isRestartHolding = false;
        restartHoldTime = 0f;

        battleUI?.SetRestartProgress(0f);
        battleUI?.ShowRestartProgress(false);
    }

    private void RestartScene()
    {
        isRestartHolding = false;

        Scene currentScene =
            SceneManager.GetActiveScene();

        SceneManager.LoadScene(
            currentScene.buildIndex);
    }

    public void HandleCellSelected(Cell cell)
    {
        if (playerController.IsBusy)
            return;

        checkersCommand?.Interact(cell);
    }

    private void OnTurnChanged(
        PlayerTeam team)
    {
        battleUI?.SetCurrentTeam(team);
    }
}