using DG.Tweening;
using UnityEngine;

/// <summary>Animate victory/defeat panels in unscaled time, even when game is paused.</summary>
[RequireComponent(typeof(CanvasGroup))]
[DisallowMultipleComponent]
public sealed class TPSResultPanelTween : MonoBehaviour
{
    private CanvasGroup _group;
    private Vector3 _normalScale;
    private Sequence _animation;

    private void Awake()
    {
        _group = GetComponent<CanvasGroup>();
        _normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        if (_group == null) _group = GetComponent<CanvasGroup>();
        if (_normalScale == Vector3.zero) _normalScale = transform.localScale;
        if (_animation != null && _animation.IsActive()) _animation.Kill();

        _group.alpha = 0f;
        transform.localScale = _normalScale * 0.90f;
        _animation = DOTween.Sequence();
        _animation.Join(DOTween.To(() => _group.alpha,
            value => _group.alpha = value, 1f, 0.35f));
        _animation.Join(transform.DOScale(_normalScale, 0.35f).SetEase(Ease.OutBack));
        _animation.SetUpdate(true).SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (_animation != null && _animation.IsActive()) _animation.Kill();
        _animation = null;
        if (_group != null) _group.alpha = 1f;
        transform.localScale = _normalScale;
    }
}
