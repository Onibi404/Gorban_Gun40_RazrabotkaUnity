using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

// Additional close-combat weapon: V (new Input System).
// Does not alter the player's existing weapon rigs or animations.
[DisallowMultipleComponent]
public sealed class TPSPlayerMelee : MonoBehaviour
{
    [SerializeField] private float damage = 25f;
    [SerializeField] private float range = 2.0f;
    [SerializeField] private float halfAngle = 65f;
    [SerializeField] private float cooldown = 0.65f;
    [SerializeField] private LayerMask hitMask = ~0;
    private float _nextAttack;
    private readonly Collider[] _hits = new Collider[64];
    private readonly HashSet<Health> _affected = new HashSet<Health>();

    private void Update()
    {
        if (Time.timeScale <= 0f || Cursor.lockState != CursorLockMode.Locked ||
            Keyboard.current == null || !Keyboard.current.vKey.wasPressedThisFrame ||
            Time.time < _nextAttack) return;

        _nextAttack = Time.time + cooldown;
        _affected.Clear();
        Vector3 from = transform.position + Vector3.up * 1.1f;
        Vector3 forward = transform.forward;
        int count = Physics.OverlapSphereNonAlloc(from, range, _hits, hitMask,
            QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = _hits[i];
            if (!c || c.transform.root == transform.root) continue;
            var health = c.GetComponentInParent<Health>();
            if (!health || !_affected.Add(health)) continue;
            Vector3 delta = c.ClosestPoint(from) - from;
            delta.y = 0f;
            if (delta.sqrMagnitude > 0.01f &&
                Vector3.Angle(forward, delta.normalized) > halfAngle) continue;
            health.MarkLastHitByPlayer(true);
            health.TakeDamage(damage, forward);
        }
        Debug.DrawRay(from, forward * range, Color.red, 0.4f);
    }
}
