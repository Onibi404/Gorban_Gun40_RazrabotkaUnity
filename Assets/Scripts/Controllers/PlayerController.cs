using System;
using DG.Tweening;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [Header("Анимация хода")]
    [SerializeField, Min(0.05f)]
    private float moveDuration = 0.25f;

    [SerializeField]
    private Ease moveEase = Ease.InOutSine;

    private Tween moveTween;

    public bool IsBusy { get; private set; }

    public void VisualizeMove(
        CheckersMove move,
        Action onComplete)
    {
        if (IsBusy)
            return;

        IsBusy = true;

        Unit unit = move.Unit;

        Vector3 targetPosition =
            move.To.transform.position +
            Vector3.up * 0.35f;

        moveTween?.Kill();

        moveTween =
            unit.transform
                .DOMove(
                    targetPosition,
                    moveDuration)
                .SetEase(moveEase)
                .OnComplete(() =>
                {
                    IsBusy = false;
                    moveTween = null;

                    onComplete?.Invoke();
                });
    }

    private void OnDisable()
    {
        moveTween?.Kill();
        moveTween = null;

        IsBusy = false;
    }
}