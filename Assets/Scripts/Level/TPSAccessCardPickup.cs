using DG.Tweening;
using UnityEngine;

/// <summary>Access card, with looping bob and a fly-to-player pickup tween.</summary>
[RequireComponent(typeof(BoxCollider))]
[DisallowMultipleComponent]
public sealed class TPSAccessCardPickup : MonoBehaviour
{
    [SerializeField] private TPSMissionDirector _mission;
    [SerializeField] private float _spinDegreesPerSecond = 100f;

    private Tween _bob;
    private Sequence _collectAnimation;
    private bool _collected;

    public void Configure(TPSMissionDirector mission) { _mission = mission; }

    private void OnEnable()
    {
        _collected = false;
        _bob = transform.DOLocalMoveY(transform.localPosition.y + 0.18f, 0.85f)
            .SetEase(Ease.InOutSine).SetLoops(-1, LoopType.Yoyo)
            .SetLink(gameObject);
    }

    private void Update()
    {
        if (!_collected)
            transform.Rotate(Vector3.up, _spinDegreesPerSecond * Time.deltaTime, Space.World);
    }

    private void OnTriggerEnter(Collider other) { TryPickup(other); }
    private void OnTriggerStay(Collider other) { TryPickup(other); }

    private void TryPickup(Collider other)
    {
        if (_collected || _mission == null || !_mission.IsPlayer(other) ||
            !_mission.TryCollectAccessCard())
            return;

        _collected = true;
        GetComponent<BoxCollider>().enabled = false;
        if (_bob != null && _bob.IsActive()) _bob.Kill();

        Vector3 target = other.transform.position + Vector3.up * 1.4f;
        _collectAnimation = DOTween.Sequence();
        _collectAnimation.Join(transform.DOMove(target, 0.35f).SetEase(Ease.OutCubic));
        _collectAnimation.Join(transform.DOScale(Vector3.zero, 0.35f).SetEase(Ease.InBack));
        _collectAnimation.SetLink(gameObject).OnComplete(() => Destroy(gameObject));
    }

    private void OnDisable()
    {
        if (_bob != null && _bob.IsActive()) _bob.Kill();
        if (_collectAnimation != null && _collectAnimation.IsActive()) _collectAnimation.Kill();
    }
}
