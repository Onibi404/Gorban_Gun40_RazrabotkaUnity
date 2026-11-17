using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// Mission gate with helpful E-key feedback and physically moved DOTween panels.
/// Keeps the API, scene references and mission stages used by steps 51-60.
/// </summary>
[DisallowMultipleComponent]
public sealed class TPSMissionGate : MonoBehaviour
{
    [SerializeField] private TPSMissionDirector _mission;
    [SerializeField] private Transform _player;
    [SerializeField] private Rigidbody _leftPanel;
    [SerializeField] private Rigidbody _rightPanel;
    [SerializeField, Min(1f)] private float _interactDistance = 3.5f;
    [SerializeField, Min(0.1f)] private float _openDuration = 1.2f;
    [SerializeField, Min(0.5f)] private float _slideDistance = 1.8f;

    private Sequence _opening;
    private bool _opened;

    public void Configure(TPSMissionDirector mission, Transform player,
        Rigidbody leftPanel, Rigidbody rightPanel)
    {
        _mission = mission;
        _player = player;
        _leftPanel = leftPanel;
        _rightPanel = rightPanel;
    }

    private void Update()
    {
        if (_opened || (_opening != null && _opening.IsActive()) ||
            _mission == null || _player == null || Time.timeScale <= 0f ||
            Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame)
            return;

        Vector3 difference = _player.position - transform.position;
        difference.y = 0f;
        if (difference.sqrMagnitude > _interactDistance * _interactDistance)
        {
            if (difference.sqrMagnitude <= 100f)
                _mission.ShowGateHint("Подойдите ближе к центру двери и нажмите E");
            return;
        }

        if (!_mission.CanOpenGate())
        {
            _mission.ShowGateHint(_mission.ExplainGateRequirement());
            return;
        }

        if (_leftPanel == null || _rightPanel == null)
        {
            _mission.ShowGateHint("У двери не назначены обе створки");
            return;
        }

        // Mission stage is advanced only after all preconditions have passed.
        _mission.GateOpened();
        OpenWithTween();
    }

    private void OpenWithTween()
    {
        if (_opening != null && _opening.IsActive()) _opening.Kill();

        Vector3 leftGoal = _leftPanel.position - transform.right * _slideDistance;
        Vector3 rightGoal = _rightPanel.position + transform.right * _slideDistance;

        // DOTween.To + Rigidbody.MovePosition runs on the physics step, so it
        // does not teleport kinematic collision panels every Update frame.
        Tween left = DOTween.To(() => _leftPanel.position,
                value => _leftPanel.MovePosition(value), leftGoal, _openDuration)
            .SetEase(Ease.InOutCubic);
        Tween right = DOTween.To(() => _rightPanel.position,
                value => _rightPanel.MovePosition(value), rightGoal, _openDuration)
            .SetEase(Ease.InOutCubic);

        _opening = DOTween.Sequence();
        _opening.Join(left).Join(right)
            .SetUpdate(UpdateType.Fixed)
            .SetLink(gameObject)
            .OnComplete(() =>
            {
                _opened = true;
                _opening = null;
            });
    }

    private void OnDisable()
    {
        if (_opening != null && _opening.IsActive()) _opening.Kill();
        _opening = null;
    }
}
