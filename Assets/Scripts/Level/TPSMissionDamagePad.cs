using System.Collections;
using UnityEngine;

/// <summary>Optional acid/fire zone with cancellable periodic damage.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class TPSMissionDamagePad : MonoBehaviour
{
    [SerializeField] private PlayerHealth _player;
    [SerializeField, Min(0f)] private float _damagePerTick = 3f;
    [SerializeField, Min(0.1f)] private float _secondsPerTick = 1f;

    private Coroutine _damageRoutine;
    private int _playerContacts;

    public void Configure(PlayerHealth player)
    {
        _player = player;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerContacts++;
        if (_damageRoutine == null)
            _damageRoutine = StartCoroutine(DamageWhileInside());
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsPlayer(other)) return;
        _playerContacts = Mathf.Max(0, _playerContacts - 1);
        if (_playerContacts == 0)
            StopDamage();
    }

    private bool IsPlayer(Collider other)
    {
        return _player != null &&
               (other.transform == _player.transform ||
                other.transform.IsChildOf(_player.transform));
    }

    private IEnumerator DamageWhileInside()
    {
        while (_playerContacts > 0 && _player != null && !_player.IsDead())
        {
            yield return new WaitForSeconds(_secondsPerTick);
            if (_playerContacts > 0 && _player != null && !_player.IsDead())
                _player.TakeDamage(_damagePerTick, Vector3.zero);
        }
        _damageRoutine = null;
    }

    private void StopDamage()
    {
        if (_damageRoutine == null) return;
        StopCoroutine(_damageRoutine);
        _damageRoutine = null;
    }

    private void OnDisable()
    {
        _playerContacts = 0;
        StopDamage();
    }
}
