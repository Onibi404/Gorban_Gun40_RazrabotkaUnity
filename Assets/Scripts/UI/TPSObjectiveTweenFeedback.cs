using DG.Tweening;
using TMPro;
using UnityEngine;

/// <summary>
/// Pulse only when the mission objective text changes, not every frame.
/// No change to existing LevelFlowController is required.
/// </summary>
[RequireComponent(typeof(TMP_Text))]
[DisallowMultipleComponent]
public sealed class TPSObjectiveTweenFeedback : MonoBehaviour
{
    private TMP_Text _label;
    private string _previousText;
    private Vector3 _normalScale;
    private Tween _pulse;

    private void Awake()
    {
        _label = GetComponent<TMP_Text>();
        _normalScale = transform.localScale;
    }

    private void OnEnable()
    {
        _previousText = _label != null ? _label.text : "";
    }

    private void LateUpdate()
    {
        if (_label == null || _label.text == _previousText) return;
        _previousText = _label.text;
        if (_pulse != null && _pulse.IsActive()) _pulse.Kill();
        transform.localScale = _normalScale;
        _pulse = transform.DOScale(_normalScale * 1.12f, 0.18f)
            .SetLoops(2, LoopType.Yoyo)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true)
            .SetLink(gameObject);
    }

    private void OnDisable()
    {
        if (_pulse != null && _pulse.IsActive()) _pulse.Kill();
        transform.localScale = _normalScale;
    }
}
