using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Optional demonstration of a hinged interactive door animated in FixedUpdate.</summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class TPSTweenHingedDoor : MonoBehaviour
{
    [SerializeField] private Transform _player;
    [SerializeField, Min(1f)] private float _interactDistance = 2.8f;
    [SerializeField] private float _openAngle = 90f;

    private Rigidbody _body;
    private Quaternion _closedRotation;
    private float _currentAngle;
    private bool _open;
    private Tween _motion;

    public void Configure(Transform player) { _player = player; }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;
        _body.useGravity = false;
        _closedRotation = _body.rotation;
    }

    private void Update()
    {
        if (_player == null || Time.timeScale <= 0f ||
            Keyboard.current == null || !Keyboard.current.eKey.wasPressedThisFrame ||
            (_motion != null && _motion.IsActive() && _motion.IsPlaying()))
            return;

        Vector3 delta = _player.position - transform.position;
        delta.y = 0f;
        if (delta.sqrMagnitude > _interactDistance * _interactDistance) return;

        _open = !_open;
        float goal = _open ? _openAngle : 0f;
        if (_motion != null && _motion.IsActive()) _motion.Kill();
        _motion = DOTween.To(() => _currentAngle,
                angle =>
                {
                    _currentAngle = angle;
                    _body.MoveRotation(_closedRotation * Quaternion.Euler(0f, angle, 0f));
                }, goal, 0.8f)
            .SetEase(Ease.InOutSine)
            .SetUpdate(UpdateType.Fixed)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (_motion != null && _motion.IsActive()) _motion.Kill();
        _motion = null;
    }
}
