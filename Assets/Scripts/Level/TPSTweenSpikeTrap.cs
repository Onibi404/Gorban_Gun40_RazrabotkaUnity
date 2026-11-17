using DG.Tweening;
using UnityEngine;

/// <summary>
/// Visual spikes for the existing damage trigger. The original damage-over-time
/// behaviour is left untouched; visuals are animated once in OnEnable.
/// </summary>
[DisallowMultipleComponent]
public sealed class TPSTweenSpikeTrap : MonoBehaviour
{
    [SerializeField] private Transform[] _spikes;
    [SerializeField, Min(0.05f)] private float _height = 0.75f;
    [SerializeField, Min(0.1f)] private float _halfCycle = 0.85f;

    private Tween[] _tweens;

    public void Configure(Transform[] spikes) { _spikes = spikes; }

    private void OnEnable()
    {
        if (_spikes == null) return;
        _tweens = new Tween[_spikes.Length];
        for (int i = 0; i < _spikes.Length; i++)
        {
            Transform spike = _spikes[i];
            if (spike == null) continue;
            _tweens[i] = spike.DOLocalMoveY(spike.localPosition.y + _height,
                    _halfCycle + i * 0.025f)
                .SetEase(Ease.InOutSine)
                .SetLoops(-1, LoopType.Yoyo)
                .SetLink(gameObject);
        }
    }

    private void OnDisable()
    {
        if (_tweens == null) return;
        foreach (Tween tween in _tweens)
            if (tween != null && tween.IsActive()) tween.Kill();
        _tweens = null;
    }
}
