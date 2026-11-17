using System.Collections.Generic;
using UnityEngine;

// Physical projectile with area-of-effect damage and bounded lifetime.
[RequireComponent(typeof(Rigidbody))]
[RequireComponent(typeof(Collider))]
public sealed class TPSGrenadeProjectile : MonoBehaviour
{
    private Rigidbody _body;
    private float _damage;
    private float _radius;
    private Transform _owner;
    private GameObject _effect;
    private bool _exploded;
    private float _deadline;
    private readonly Collider[] _overlapResults = new Collider[128];
    private readonly HashSet<Health> _affected = new HashSet<Health>();

    private void Awake()
    {
        _body = GetComponent<Rigidbody>();
    }

    public void Launch(Vector3 velocity, float damage, float radius,
                       Transform owner, GameObject effect)
    {
        _damage = damage;
        _radius = Mathf.Max(0.5f, radius);
        _owner = owner;
        _effect = effect;
        _deadline = Time.time + 3.5f;
        _body.useGravity = true;
        _body.collisionDetectionMode = CollisionDetectionMode.Continuous;
        _body.velocity = velocity;

        Collider own = GetComponent<Collider>();
        if (owner && own)
            foreach (var c in owner.GetComponentsInChildren<Collider>())
                if (c) Physics.IgnoreCollision(own, c, true);
    }

    private void Update()
    {
        if (!_exploded && _deadline > 0f && Time.time >= _deadline) Explode();
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (_deadline > 0f) Explode();
    }

    private void Explode()
    {
        if (_exploded) return;
        _exploded = true;
        _affected.Clear();
        int count = Physics.OverlapSphereNonAlloc(transform.position, _radius,
            _overlapResults, ~0, QueryTriggerInteraction.Ignore);
        for (int i = 0; i < count; i++)
        {
            Collider c = _overlapResults[i];
            if (!c) continue;
            var health = c.GetComponentInParent<Health>();
            if (!health || (_owner && health.transform.root == _owner) || !_affected.Add(health)) continue;
            float dist = Vector3.Distance(transform.position, c.ClosestPoint(transform.position));
            float falloff = 1f - 0.6f * Mathf.Clamp01(dist / _radius);
            Vector3 direction = (c.transform.position - transform.position).normalized;
            health.MarkLastHitByPlayer(_owner && _owner.CompareTag("Player"));
            health.TakeDamage(_damage * falloff, direction);
        }
        if (_effect) TPSImpactPool.Spawn(_effect, transform.position, Quaternion.identity, 4f);
        Destroy(gameObject);
    }
}
