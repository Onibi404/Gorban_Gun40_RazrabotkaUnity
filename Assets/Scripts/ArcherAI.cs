using System.Collections.Generic;
using Netologia;
using Netologia.Systems;
using Netologia.TowerDefence;
using Netologia.TowerDefence.Behaviors;
using UnityEngine;
using Zenject;


[RequireComponent(typeof(Unit))]
public class ArcherAI : MonoBehaviour
{
    [Header("Movement")]
    [SerializeField, Min(0.1f)] private float _speed = 2.3f;
    [SerializeField, Min(0.01f)] private float _stopDistance = 0.10f;
    [SerializeField] private Rigidbody2D _rb2d;
    [SerializeField, Range(0.05f, 0.5f)] private float _navigationGridSpacing = 0.14f;
    [SerializeField, Range(0f, 0.2f)] private float _agentClearance = 0.06f;
    [SerializeField, Min(0.1f)] private float _pathRefreshInterval = 0.55f;
    [SerializeField] private LayerMask _obstacleMask; 

    [Header("Nearby archers")]
    [SerializeField, Range(0.1f, 0.8f)] private float _separationRadius = 0.34f;
    [SerializeField, Range(0f, 1f)] private float _separationStrength = 0.40f;

    [Header("Enemy detection")]
    [SerializeField, Min(0.1f)] private float _visionRadius = 4.5f;
    [SerializeField] private LayerMask _enemyMask;
    [SerializeField, Min(0.05f)] private float _targetRefreshInterval = 0.20f;

    [Header("Attack")]
    [SerializeField, Min(0.1f)] private float _attackDistance = 1.25f;
    [SerializeField] private Projectile _arrowRef;
    [SerializeField] private Transform _shootPoint;
    [SerializeField, Min(0.05f)] private float _attackCooldown = 1.1f;
    [SerializeField, Min(0.1f)] private float _attackDamage = 3f;
    [SerializeField] private ElementalType _element = ElementalType.Physic;

    private readonly Collider2D[] _enemyColliders = new Collider2D[64];
    private readonly Collider2D[] _nearbyColliders = new Collider2D[48];
    private readonly List<Vector2> _path = new List<Vector2>(32);

    private Unit _self;
    private Rigidbody2D _body;
    private ProjectileSystem _projectiles;
    private ArcherArea _area;
    private Unit _target;
    private int _waypointIndex;
    private float _attackTimer;
    private float _targetRefreshTimer;
    private float _pathTimer;
    private Vector2 _lastPlannedEnemyPosition;

    public bool HasWalkableArea => _area != null;

    [Inject]
    private void Construct(ProjectileSystem projectiles)
    {
        _projectiles = projectiles;
    }

    private void Awake()
    {
        _self = GetComponent<Unit>();
        _body = _rb2d != null ? _rb2d : GetComponent<Rigidbody2D>();
        if (_body != null)
        {
            
            
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            _body.interpolation = RigidbodyInterpolation2D.Interpolate;
            _body.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        Collider2D ownCollider = GetComponent<Collider2D>();
        if (ownCollider != null)
            ownCollider.isTrigger = true;

        ResolveProjectileSystem();
    }

    private void OnEnable()
    {
        _target = null;
        _attackTimer = 0f;
        _targetRefreshTimer = 0f;
        _pathTimer = 0f;
        _waypointIndex = 0;
        _path.Clear();
        ClaimNearestArea();
    }

    private void Update()
    {
        if (!TimeManager.IsGame)
            return;

        if (_self == null || _self.IsDead)
        {
            Destroy(gameObject);
            return;
        }

        ResolveProjectileSystem();
        _attackTimer -= TimeManager.DeltaTime;
        _targetRefreshTimer -= TimeManager.DeltaTime;

        if (_targetRefreshTimer <= 0f || !IsValidTarget(_target))
        {
            Unit fresh = FindNearestEnemy();
            if (fresh != _target)
            {
                _target = fresh;
                _pathTimer = 0f;
            }
            _targetRefreshTimer = _targetRefreshInterval;
        }

        if (_target == null)
        {
            
            _path.Clear();
            _waypointIndex = 0;
            return;
        }

        Vector2 delta = (Vector2)_target.transform.position - Position;
        if (delta.sqrMagnitude <= _attackDistance * _attackDistance)
        {
            
            
            _self.FlipByDirection(delta.x);
            TryShoot(_target);
        }
    }

    private void FixedUpdate()
    {
        if (!TimeManager.IsGame || _area == null || !IsValidTarget(_target))
            return;

        Vector2 position = Position;
        Vector2 enemyPosition = _target.transform.position;
        Vector2 delta = enemyPosition - position;

        
        if (delta.sqrMagnitude <= _attackDistance * _attackDistance)
            return;

        
        _pathTimer -= TimeManager.FixedDeltaTime;
        if (_pathTimer <= 0f ||
            (enemyPosition - _lastPlannedEnemyPosition).sqrMagnitude > 0.42f * 0.42f)
        {
            BuildPath(position, enemyPosition);
            _lastPlannedEnemyPosition = enemyPosition;
            _pathTimer = _pathRefreshInterval;
        }

        if (_path.Count == 0 || _waypointIndex >= _path.Count)
            return; 

        
        while (_waypointIndex + 1 < _path.Count &&
               _area.CanTravel(position, _path[_waypointIndex + 1], _agentClearance, _obstacleMask))
        {
            _waypointIndex++;
        }

        Vector2 waypoint = _path[_waypointIndex];
        Vector2 toward = waypoint - position;
        float remaining = toward.magnitude;
        if (remaining <= _stopDistance * 0.5f)
        {
            _waypointIndex++;
            return;
        }

        float step = Mathf.Min(_speed * TimeManager.FixedDeltaTime, remaining);
        Vector2 direction = toward / remaining;
        Vector2 separation = ComputeSeparation(position);
        Vector2 steered = direction + separation * _separationStrength;
        if (steered.sqrMagnitude > 0.0001f)
            steered.Normalize();
        else
            steered = direction;

        Vector2 next = position + steered * step;
        if (!_area.CanTravel(position, next, _agentClearance, _obstacleMask))
        {
            
            next = position + direction * step;
            if (!_area.CanTravel(position, next, _agentClearance, _obstacleMask))
            {
                _pathTimer = 0f;
                return;
            }
        }

        if (_body != null)
            _body.MovePosition(next);
        else
            transform.position = new Vector3(next.x, next.y, transform.position.z);

        _self.FlipByDirection((next - position).x);
    }

    private void BuildPath(Vector2 position, Vector2 enemyPosition)
    {
        _path.Clear();
        _waypointIndex = 0;
        ArcherPathfinder.TryBuildPath(_area, position, enemyPosition,
            Mathf.Max(0.05f, _attackDistance - 0.08f),
            _navigationGridSpacing, _agentClearance, _obstacleMask, _path);
    }

    private Vector2 ComputeSeparation(Vector2 position)
    {
        int count = Physics2D.OverlapCircleNonAlloc(
            position, _separationRadius, _nearbyColliders, 1 << gameObject.layer);

        Vector2 push = Vector2.zero;
        for (int i = 0; i < count; i++)
        {
            Collider2D col = _nearbyColliders[i];
            if (col == null)
                continue;
            ArcherAI other = col.GetComponentInParent<ArcherAI>();
            if (other == null || other == this || !other.isActiveAndEnabled)
                continue;

            Vector2 difference = position - (Vector2)other.transform.position;
            float sqr = difference.sqrMagnitude;
            if (sqr < 0.000001f)
            {
                float sign = GetInstanceID() < other.GetInstanceID() ? -1f : 1f;
                difference = new Vector2(0.1f * sign, 0.07f * sign);
                sqr = difference.sqrMagnitude;
            }
            float length = Mathf.Sqrt(sqr);
            push += difference / length * Mathf.Clamp01((_separationRadius - length) / _separationRadius);
        }

        return Vector2.ClampMagnitude(push, 1f);
    }

    private Unit FindNearestEnemy()
    {
        int count = Physics2D.OverlapCircleNonAlloc(Position, _visionRadius,
            _enemyColliders, _enemyMask);
        Unit best = null;
        float bestSqr = float.PositiveInfinity;

        for (int i = 0; i < count; i++)
        {
            Collider2D col = _enemyColliders[i];
            if (col == null)
                continue;
            Unit candidate = col.GetComponentInParent<Unit>();
            if (!IsValidTarget(candidate) || candidate == _self)
                continue;

            float d = ((Vector2)candidate.transform.position - Position).sqrMagnitude;
            if (d >= bestSqr)
                continue;
            bestSqr = d;
            best = candidate;
        }


        if (IsValidTarget(_target))
        {
            float currentSqr = ((Vector2)_target.transform.position - Position).sqrMagnitude;
            if (currentSqr <= _visionRadius * _visionRadius &&
                (best == null || currentSqr <= bestSqr * 1.35f))
                return _target;
        }
        return best;
    }

    private static bool IsValidTarget(Unit unit)
    {
        return unit != null && unit.gameObject.activeInHierarchy && !unit.IsDead;
    }

    private Vector2 Position => _body != null ? _body.position : (Vector2)transform.position;

    private void TryShoot(Unit enemy)
    {
        if (!IsValidTarget(enemy) || _attackTimer > 0f ||
            _projectiles == null || _arrowRef == null)
            return;

        Vector3 origin = _shootPoint != null ? _shootPoint.position : transform.position;
        Projectile arrow = _projectiles[_arrowRef].Get;
        arrow.PrepareData(origin, enemy, _attackDamage, _element);
        arrow.Ref = _arrowRef;
        _attackTimer = _attackCooldown;
    }

    private void ResolveProjectileSystem()
    {
        if (_projectiles == null && Director.Instance != null)
            _projectiles = Director.Instance.Projectiles;
    }

    private void ClaimNearestArea()
    {
        if (_area != null)
            _area.Release(this);
        _area = null;

        ArcherArea[] areas = FindObjectsOfType<ArcherArea>();
        ArcherArea best = null;
        float shortest = float.PositiveInfinity;
        Vector2 spawn = Position;

        foreach (ArcherArea area in areas)
        {
            if (area == null || !area.HasFreeSlot)
                continue;
            Vector2 candidate = area.NearestPointInside(spawn);
            float d = (candidate - spawn).sqrMagnitude;
            if (d < shortest)
            {
                shortest = d;
                best = area;
            }
        }

        if (best == null || !best.TryClaim(this))
        {
            Debug.LogWarning("[ArcherAI] No free ArcherArea. Extend allowed areas in the level.", this);
            return;
        }

        _area = best;
        if (_area.CanStand(spawn, _agentClearance, _obstacleMask))
            return;


        Bounds bounds = _area.AreaBounds;
        float closest = float.PositiveInfinity;
        Vector2 safe = _area.NearestPointInside(spawn);
        bool found = false;
        const float step = 0.10f;
        for (float y = bounds.min.y + step * 0.5f; y < bounds.max.y; y += step)
        {
            for (float x = bounds.min.x + step * 0.5f; x < bounds.max.x; x += step)
            {
                Vector2 p = new Vector2(x, y);
                if (!_area.CanStand(p, _agentClearance, _obstacleMask))
                    continue;
                float d = (spawn - p).sqrMagnitude;
                if (d >= closest)
                    continue;
                closest = d;
                safe = p;
                found = true;
            }
        }

        if (!found)
        {
            _area.Release(this);
            _area = null;
            Debug.LogWarning("[ArcherAI] Claimed zone has no walkable room for the archer.", this);
            return;
        }

        if (_body != null)
            _body.position = safe;
        else
            transform.position = new Vector3(safe.x, safe.y, transform.position.z);
    }

    private void OnDisable()
    {
        if (_area != null)
            _area.Release(this);
        _area = null;
        _target = null;
        _path.Clear();
    }

    private void OnValidate()
    {
        if (_visionRadius <= _attackDistance)
            _visionRadius = _attackDistance + 0.5f;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _visionRadius);
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, _attackDistance);
        Gizmos.color = Color.cyan;
        for (int i = _waypointIndex; i < _path.Count; i++)
        {
            Vector3 from = i == _waypointIndex ? transform.position : (Vector3)_path[i - 1];
            Gizmos.DrawLine(from, _path[i]);
        }
    }
}
