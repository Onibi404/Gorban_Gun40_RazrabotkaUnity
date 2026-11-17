using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using Zenject;

public class LevelFlowController : MonoBehaviour
{
    [Header("Existing Systems")]
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private KillCounter _killCounter;
    [SerializeField] private MouseEject _mouseEject;

    [Header("UI Panels")]
    [SerializeField] private GameObject _victoryPanel;
    [SerializeField] private GameObject _defeatPanel;
    [SerializeField] private TMP_Text _objectiveText;

    [Header("Mission Settings")]
    [SerializeField, Min(1)] private int _requiredKills = 3;

    private IGameLevelState _levelState;
    private bool _gameFinished;
    private bool _defeatPending;
    private bool _externalVictoryMode;

    [Inject]
    private void Construct(IGameLevelState levelState)
    {
        _levelState = levelState;
    }

    private void Awake()
    {
        Time.timeScale = 1f;
        if (_victoryPanel != null) _victoryPanel.SetActive(false);
        if (_defeatPanel != null) _defeatPanel.SetActive(false);
    }

    private void OnEnable()
    {
        if (_killCounter != null)
            _killCounter.OnChanged += OnKillsChanged;
    }

    private void Start()
    {
        if (_killCounter != null)
            OnKillsChanged(_killCounter.Kills);
    }

    private void Update()
    {
        if (_gameFinished || _defeatPending)
            return;

        if (_playerHealth != null && _playerHealth.IsDead())
        {
            _defeatPending = true;
            StartCoroutine(ShowDefeatDelayed());
        }
    }

    private IEnumerator ShowDefeatDelayed()
    {
        yield return new WaitForSecondsRealtime(1.5f);
        if (!_gameFinished)
            ShowResult(_defeatPanel);
    }

    private void OnKillsChanged(int kills)
    {
        if (_gameFinished || _defeatPending || _externalVictoryMode)
            return;

        if (_objectiveText != null)
            _objectiveText.text = $"Уничтожено врагов: {kills}/{_requiredKills}";

        if (kills >= _requiredKills)
            ShowResult(_victoryPanel);
    }

    // Invoked by TPSMissionDirector from its Awake.
    public void EnableExternalVictoryMode()
    {
        _externalVictoryMode = true;
    }

    public void SetExternalObjectiveText(string objective)
    {
        if (_objectiveText != null && _externalVictoryMode && !_gameFinished)
            _objectiveText.text = objective;
    }

    public void CompleteMissionVictory()
    {
        if (!_externalVictoryMode || _gameFinished || _defeatPending)
            return;
        ShowResult(_victoryPanel);
    }

    private void ShowResult(GameObject panel)
    {
        if (panel == null || _gameFinished || _levelState == null ||
            !_levelState.TryComplete(panel == _victoryPanel))
            return;

        _gameFinished = true;
        panel.SetActive(true);

        if (_mouseEject != null)
            _mouseEject.enabled = false;

        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    private void OnDisable()
    {
        if (_killCounter != null)
            _killCounter.OnChanged -= OnKillsChanged;
    }
}
