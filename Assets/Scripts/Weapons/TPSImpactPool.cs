using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Object Pool pattern: reuses hit/explosion VFX instead of Instantiate/Destroy on every hit.
/// Created lazily in the active scene. Does not require a change to Zenject installers.
/// </summary>
public sealed class TPSImpactPool : MonoBehaviour
{
    private static TPSImpactPool _instance;
    private readonly Dictionary<GameObject, Stack<GameObject>> _available =
        new Dictionary<GameObject, Stack<GameObject>>();

    private const int MaxCachedPerPrefab = 32;

    public static GameObject Spawn(GameObject prefab, Vector3 position,
                                   Quaternion rotation, float releaseAfter = 3f)
    {
        if (!prefab) return null;
        if (!_instance)
        {
            GameObject holder = new GameObject("TPS_ImpactPool");
            _instance = holder.AddComponent<TPSImpactPool>();
        }
        return _instance.Rent(prefab, position, rotation, releaseAfter);
    }

    private GameObject Rent(GameObject prefab, Vector3 position,
                            Quaternion rotation, float releaseAfter)
    {
        Stack<GameObject> stack;
        if (!_available.TryGetValue(prefab, out stack))
        {
            stack = new Stack<GameObject>();
            _available.Add(prefab, stack);
        }

        GameObject effect = null;
        while (stack.Count > 0 && !effect)
            effect = stack.Pop();

        if (!effect) effect = Instantiate(prefab, transform);
        effect.transform.SetParent(transform, false);
        effect.transform.SetPositionAndRotation(position, rotation);
        effect.SetActive(true);

        // A reused ParticleSystem must be restarted, not just enabled.
        ParticleSystem[] particles = effect.GetComponentsInChildren<ParticleSystem>(true);
        foreach (ParticleSystem particle in particles)
        {
            particle.Clear(true);
            particle.Play(true);
        }
        StartCoroutine(ReturnAfter(prefab, effect, Mathf.Max(0.1f, releaseAfter)));
        return effect;
    }

    private IEnumerator ReturnAfter(GameObject prefab, GameObject effect, float delay)
    {
        yield return new WaitForSeconds(delay);
        if (!effect) yield break;
        effect.SetActive(false);
        effect.transform.SetParent(transform, false);
        Stack<GameObject> stack;
        if (!_available.TryGetValue(prefab, out stack))
        {
            Destroy(effect);
            yield break;
        }
        if (stack.Count < MaxCachedPerPrefab) stack.Push(effect);
        else Destroy(effect);
    }

    [ContextMenu("Show VFX Pool Status")]
    private void ShowStatus()
    {
        foreach (KeyValuePair<GameObject, Stack<GameObject>> entry in _available)
            Debug.Log("TPSImpactPool: " + entry.Key.name + ": " +
                      entry.Value.Count + " available", this);
    }
}
