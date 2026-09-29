using System;
using System.Collections;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    [SerializeField, Min(0.05f)] private float moveDuration = 0.25f;

    public bool IsBusy { get; private set; }

    public void VisualizeMove(CheckersMove move, Action onComplete)
    {
        if (!IsBusy)
            StartCoroutine(MoveRoutine(move, onComplete));
    }

    private IEnumerator MoveRoutine(CheckersMove move, Action onComplete)
    {
        IsBusy = true;

        Unit unit = move.Unit;
        Vector3 start = unit.transform.position;
        Vector3 end = move.To.transform.position + Vector3.up * 0.35f;
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / moveDuration);
            unit.transform.position = Vector3.Lerp(start, end, t);
            yield return null;
        }

        unit.transform.position = end;
        IsBusy = false;
        onComplete?.Invoke();
    }
}
