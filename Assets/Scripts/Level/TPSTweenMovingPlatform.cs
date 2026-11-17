using DG.Tweening;
using UnityEngine;

/// <summary>
/// Kinematic DOTween lift. A rider trigger transfers platform motion to the
/// player's CharacterController while the player is standing in the ride area.
/// </summary>
[RequireComponent(typeof(Rigidbody))]
[DisallowMultipleComponent]
public sealed class TPSTweenMovingPlatform : MonoBehaviour
{
    [SerializeField] private PlayerHealth _player;
    [SerializeField, Min(0.5f)] private float _liftHeight = 2f;
    [SerializeField, Min(0.3f)] private float _riseSeconds = 2.5f;

    private Rigidbody _body;
    private CharacterController _playerController;
    private Sequence _sequence;
    private Vector3 _lastPosition;
    private int _riderContacts;

    public void Configure(PlayerHealth player)
    {
        _player = player;
        _playerController = player != null ? player.GetComponent<CharacterController>() : null;
    }

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
        _body.isKinematic = true;
        _body.useGravity = false;
        _lastPosition = _body.position;
        if (_player != null && _playerController == null)
            _playerController = _player.GetComponent<CharacterController>();
    }

    private void OnEnable()
    {
        if (_body == null) _body = GetComponent<Rigidbody>();
        _lastPosition = _body.position;
        Vector3 end = _lastPosition + Vector3.up * _liftHeight;
        _sequence = DOTween.Sequence();
        _sequence.Append(DOTween.To(() => _body.position,
                value => _body.MovePosition(value), end, _riseSeconds)
            .SetEase(Ease.InOutSine));
        _sequence.AppendInterval(0.5f);
        _sequence.SetLoops(-1, LoopType.Yoyo);
        _sequence.SetUpdate(UpdateType.Fixed);
        _sequence.SetLink(gameObject);
    }

    private void FixedUpdate()
    {
        Vector3 current = _body.position;
        Vector3 displacement = current - _lastPosition;
        _lastPosition = current;
        if (_riderContacts > 0 && _playerController != null &&
            _playerController.enabled && _player != null && !_player.IsDead())
        {
            Vector3 relative = _player.transform.position - current;
            // Only carry players standing above the platform surface.
            if (Mathf.Abs(relative.x) < 1.25f && Mathf.Abs(relative.z) < 1.25f &&
                relative.y > -0.25f && relative.y < 2.8f)
                _playerController.Move(displacement);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other)) _riderContacts++;
    }

    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other)) _riderContacts = Mathf.Max(0, _riderContacts - 1);
    }

    private bool IsPlayer(Collider other)
    {
        return _player != null &&
               (other.transform == _player.transform ||
                other.transform.IsChildOf(_player.transform));
    }

    private void OnDisable()
    {
        _riderContacts = 0;
        if (_sequence != null && _sequence.IsActive()) _sequence.Kill();
        _sequence = null;
    }
}
