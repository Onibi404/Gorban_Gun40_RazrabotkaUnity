using System.Collections;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Timed healing remains a cancellable coroutine; pickup feedback uses DOTween.
/// </summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class TPSTimedHealingPickup : MonoBehaviour
{
    [SerializeField] private PlayerHealth _player;
    [SerializeField, Min(1f)] private float _totalHeal = 40f;
    [SerializeField, Min(1)] private int _tickCount = 5;
    [SerializeField, Min(0.05f)] private float _tickInterval = 0.35f;

    private Coroutine _healing;
    private Tween _bob;
    private Tween _spin;
    private Sequence _pickupVisual;

    public void Configure(PlayerHealth player) { _player = player; }

    private void OnEnable()
    {
        _bob = transform.DOLocalMoveY(transform.localPosition.y + 0.20f, 1.0f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
        _spin = transform.DOLocalRotate(new Vector3(0f, 360f, 0f), 3.0f,
                RotateMode.FastBeyond360)
            .SetEase(Ease.Linear).SetLoops(-1, LoopType.Restart)
            .SetLink(gameObject);
    }

    private void OnTriggerEnter(Collider other) { TryHeal(other); }
    private void OnTriggerStay(Collider other) { TryHeal(other); }

    private void TryHeal(Collider other)
    {
        if (_healing != null || _player == null || _player.IsDead() ||
            _player.currentHealth >= _player.maxHealth)
            return;
        if (other.transform != _player.transform &&
            !other.transform.IsChildOf(_player.transform))
            return;

        GetComponent<BoxCollider>().enabled = false;
        _healing = StartCoroutine(HealOverTime());

        if (_bob != null && _bob.IsActive()) _bob.Kill();
        if (_spin != null && _spin.IsActive()) _spin.Kill();
        _pickupVisual = DOTween.Sequence();
        _pickupVisual.Join(transform.DOMove(_player.transform.position + Vector3.up * 1.4f,
            0.35f).SetEase(Ease.OutCubic));
        _pickupVisual.Join(transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack));
        _pickupVisual.SetLink(gameObject);
    }

    private IEnumerator HealOverTime()
    {
        float amountPerTick = _totalHeal / _tickCount;
        for (int index = 0; index < _tickCount; index++)
        {
            if (_player == null || _player.IsDead()) yield break;
            _player.Heal(amountPerTick);
            yield return new WaitForSeconds(_tickInterval);
        }
        _healing = null;
        Destroy(gameObject);
    }

    private void OnDisable()
    {
        if (_healing != null)
        {
            StopCoroutine(_healing);
            _healing = null;
        }
        if (_bob != null && _bob.IsActive()) _bob.Kill();
        if (_spin != null && _spin.IsActive()) _spin.Kill();
        if (_pickupVisual != null && _pickupVisual.IsActive()) _pickupVisual.Kill();
    }
}
