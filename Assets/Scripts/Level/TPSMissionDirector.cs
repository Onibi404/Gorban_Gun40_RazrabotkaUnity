using System.Collections;
using UnityEngine;

/// <summary>
/// Scene-level mission state machine. References are assigned by the editor setup tool;
/// no runtime FindObjectOfType, singletons, or hardcoded scene paths.
/// </summary>
[DisallowMultipleComponent]
public sealed class TPSMissionDirector : MonoBehaviour
{
    private enum Stage
    {
        ReachZoneOne,
        CollectCard,
        EliminateEnemies,
        OpenGate,
        ReachZoneTwo,
        ReachExit,
        Complete
    }

    [Header("Scene references")]
    [SerializeField] private LevelFlowController _levelFlow;
    [SerializeField] private PlayerHealth _player;
    [SerializeField] private KillCounter _killCounter;
    [SerializeField, Min(1)] private int _requiredKills = 6;

    [Header("Runtime (read only)")]
    [SerializeField] private bool _hasAccessCard;
    [SerializeField] private bool _gateOpen;
    [SerializeField] private int _missionStage;

    private Stage _stage = Stage.ReachZoneOne;
    private Coroutine _gateHintCoroutine;

    public bool HasAccessCard => _hasAccessCard;
    public bool GateIsOpen => _gateOpen;

    // Read-only state for the always-visible mission guide (steps 71-73).
    public int CurrentStage => _missionStage;
    public int CurrentKills => _killCounter != null ? _killCounter.Kills : 0;
    public int RequiredKills => _requiredKills;

    public void Configure(LevelFlowController levelFlow, PlayerHealth player,
        KillCounter killCounter, int requiredKills)
    {
        _levelFlow = levelFlow;
        _player = player;
        _killCounter = killCounter;
        _requiredKills = Mathf.Max(1, requiredKills);
    }

    private void Awake()
    {
        // Disable kill-only victory before the first kills are registered.
        if (_levelFlow != null)
            _levelFlow.EnableExternalVictoryMode();
    }

    private void OnEnable()
    {
        if (_killCounter != null)
            _killCounter.OnChanged += OnKillsChanged;
    }

    private void Start()
    {
        UpdateObjective();
    }

    private void OnDisable()
    {
        if (_killCounter != null)
            _killCounter.OnChanged -= OnKillsChanged;
    }

    public bool IsPlayer(Collider other)
    {
        if (_player == null || other == null)
            return false;

        Transform candidate = other.transform;
        return candidate == _player.transform || candidate.IsChildOf(_player.transform);
    }

    public void EnterZone(TPSMissionZone.ZoneId zone)
    {
        if (MissionInactive()) return;

        if (_stage == Stage.ReachZoneOne && zone == TPSMissionZone.ZoneId.First)
            ChangeStage(Stage.CollectCard);
        else if (_stage == Stage.ReachZoneTwo && zone == TPSMissionZone.ZoneId.Second)
            ChangeStage(Stage.ReachExit);
        else if (_stage == Stage.ReachExit && zone == TPSMissionZone.ZoneId.Exit)
        {
            ChangeStage(Stage.Complete);
            if (_levelFlow != null)
                _levelFlow.CompleteMissionVictory();
        }
    }

    public bool TryCollectAccessCard()
    {
        // Collecting the card itself counts as reaching Zone 1. This avoids
        // a silent softlock when colliders enter in an unexpected order.
        if (MissionInactive() || _hasAccessCard ||
            (_stage != Stage.CollectCard && _stage != Stage.ReachZoneOne))
            return false;

        _hasAccessCard = true;
        ChangeStage(Stage.EliminateEnemies);
        CheckKills();
        return true;
    }

    public bool CanOpenGate()
    {
        return !MissionInactive() && _hasAccessCard &&
               _stage == Stage.OpenGate && !_gateOpen;
    }

    public void GateOpened()
    {
        if (!CanOpenGate()) return;
        _gateOpen = true;
        ChangeStage(Stage.ReachZoneTwo);
    }

    // Give the player actionable feedback on the existing objective HUD.
    public string ExplainGateRequirement()
    {
        if (_gateOpen || _stage >= Stage.ReachZoneTwo)
            return "Дверь уже открыта. Идите в голубую зону";
        if (!_hasAccessCard)
            return "Сначала подберите ЖЁЛТУЮ ключ-карту в зелёной зоне";
        if (_killCounter != null && _killCounter.Kills < _requiredKills)
            return "Уничтожьте врагов: " + _killCounter.Kills + "/" + _requiredKills;
        if (_stage == Stage.OpenGate)
            return "Нажмите E возле двери";
        if (_stage == Stage.ReachZoneOne || _stage == Stage.CollectCard)
            return "Сначала войдите в зелёную зону";
        return "Текущее задание: " + _stage;
    }

    public void ShowGateHint(string message)
    {
        if (MissionInactive() || string.IsNullOrEmpty(message)) return;
        Debug.Log("TPS Mission Gate: " + message, this);
        if (_gateHintCoroutine != null)
            StopCoroutine(_gateHintCoroutine);
        _gateHintCoroutine = StartCoroutine(ShowHintBriefly(message));
    }

    private IEnumerator ShowHintBriefly(string message)
    {
        if (_levelFlow != null)
            _levelFlow.SetExternalObjectiveText(message);
        yield return new WaitForSeconds(2.5f);
        _gateHintCoroutine = null;
        UpdateObjective();
    }

    private void OnKillsChanged(int kills)
    {
        CheckKills();
    }

    private void CheckKills()
    {
        if (MissionInactive()) return;

        if (_stage == Stage.EliminateEnemies && _killCounter != null &&
            _killCounter.Kills >= _requiredKills)
            ChangeStage(Stage.OpenGate);
        else if (_stage == Stage.EliminateEnemies)
            UpdateObjective();
    }

    private bool MissionInactive()
    {
        return _stage == Stage.Complete ||
               (_player != null && _player.IsDead()) ||
               Time.timeScale <= 0f;
    }

    private void ChangeStage(Stage next)
    {
        _stage = next;
        _missionStage = (int)next;
        UpdateObjective();
    }

    private void UpdateObjective()
    {
        if (_levelFlow == null) return;

        string objective;
        switch (_stage)
        {
            case Stage.ReachZoneOne:
                objective = "Зона 1: дойдите до ЗЕЛЁНОГО маркера";
                break;
            case Stage.CollectCard:
                objective = "Зона 1: подберите ЖЁЛТУЮ ключ-карту";
                break;
            case Stage.EliminateEnemies:
                int kills = _killCounter != null ? _killCounter.Kills : 0;
                objective = "Устраните врагов: " + kills + "/" + _requiredKills;
                break;
            case Stage.OpenGate:
                objective = "Откройте проход: подойдите к двери и нажмите E";
                break;
            case Stage.ReachZoneTwo:
                objective = "Зона 2: войдите в СИНИЙ маркер за дверью";
                break;
            case Stage.ReachExit:
                objective = "Зона 3: доберитесь до ФИОЛЕТОВОЙ эвакуации";
                break;
            default:
                objective = "Миссия выполнена!";
                break;
        }

        _levelFlow.SetExternalObjectiveText(objective);
    }
}
