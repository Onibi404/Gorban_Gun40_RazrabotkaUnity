using System.Collections.Generic;
using Netologia.TowerDefence;
using UnityEngine;


public class ArcherArea : MonoBehaviour
{
    [SerializeField, Min(1)] private int _capacity = 5;
    [SerializeField] private Collider2D _areaCollider;
    [SerializeField, Min(0.03f)] private float _segmentCheckStep = 0.08f;
    [SerializeField, Min(0.01f)] private float _occupiedCellRadius = 0.18f;

    private readonly HashSet<ArcherAI> _occupants = new HashSet<ArcherAI>();
    private readonly Collider2D[] _obstacleHits = new Collider2D[16];
    private SelectCell[] _buildCells;

    public Bounds AreaBounds => _areaCollider != null ? _areaCollider.bounds : new Bounds(transform.position, Vector3.zero);
    public bool HasFreeSlot => Count < _capacity;
    public int Capacity => _capacity;

    public int Count
    {
        get
        {
            RemoveInvalidOccupants();
            return _occupants.Count;
        }
    }

    private void Reset()
    {
        _areaCollider = GetComponent<Collider2D>();
        if (_areaCollider != null)
            _areaCollider.isTrigger = true;
    }

    private void Awake()
    {
        if (_areaCollider == null)
            _areaCollider = GetComponent<Collider2D>();

        _buildCells = FindObjectsOfType<SelectCell>();

        if (_areaCollider == null)
            Debug.LogError("[ArcherArea] Missing Collider2D on " + name, this);
        else if (!_areaCollider.isTrigger)
            Debug.LogWarning("[ArcherArea] Collider2D should be a trigger on " + name, this);
    }

    public bool Contains(Vector2 point)
    {
        return _areaCollider != null && _areaCollider.enabled &&
               _areaCollider.gameObject.activeInHierarchy && _areaCollider.OverlapPoint(point);
    }

    public Vector2 NearestPointInside(Vector2 point)
    {
        if (_areaCollider == null)
            return point;

        if (Contains(point))
            return point;

        Vector2 nearest = _areaCollider.ClosestPoint(point);
        Vector2 inward = (Vector2)_areaCollider.bounds.center - nearest;
        if (inward.sqrMagnitude > 0.000001f)
        {
            Vector2 candidate = nearest + inward.normalized * 0.05f;
            if (Contains(candidate))
                return candidate;
        }

        return Contains(nearest) ? nearest : GetRandomPointInside();
    }


    public bool CanStand(Vector2 point, float radius, LayerMask obstacleMask)
    {
        if (!Contains(point))
            return false;

        if (radius > 0f)
        {
            if (!Contains(point + Vector2.up * radius) ||
                !Contains(point - Vector2.up * radius) ||
                !Contains(point + Vector2.right * radius) ||
                !Contains(point - Vector2.right * radius))
                return false;
        }

        if (_buildCells == null)
            _buildCells = FindObjectsOfType<SelectCell>();

        float clearance = _occupiedCellRadius + radius;
        float clearanceSquared = clearance * clearance;
        for (int i = 0; i < _buildCells.Length; i++)
        {
            SelectCell cell = _buildCells[i];
            if (cell == null || !cell.HasTower)
                continue;

            if (((Vector2)cell.transform.position - point).sqrMagnitude < clearanceSquared)
                return false;
        }

        if (obstacleMask.value == 0)
            return true;

        
        int count = Physics2D.OverlapCircleNonAlloc(point, radius, _obstacleHits, obstacleMask);
        for (int i = 0; i < count; i++)
        {
            Collider2D hit = _obstacleHits[i];
            if (hit != null && !hit.isTrigger)
                return false;
        }

        return true;
    }

    public bool CanTravel(Vector2 from, Vector2 to)
    {
        return CanTravel(from, to, 0f, default(LayerMask));
    }

    public bool CanTravel(Vector2 from, Vector2 to, float radius, LayerMask obstacleMask)
    {
        float distance = Vector2.Distance(from, to);
        int segments = Mathf.Max(1, Mathf.CeilToInt(distance / _segmentCheckStep));

        for (int i = 0; i <= segments; i++)
        {
            Vector2 sample = Vector2.Lerp(from, to, i / (float)segments);
            if (!CanStand(sample, radius, obstacleMask))
                return false;
        }

        return true;
    }

    public bool TryClaim(ArcherAI archer)
    {
        if (archer == null)
            return false;

        RemoveInvalidOccupants();
        if (_occupants.Contains(archer))
            return true;
        if (_occupants.Count >= _capacity)
            return false;

        return _occupants.Add(archer);
    }

    public void Release(ArcherAI archer)
    {
        if (archer != null)
            _occupants.Remove(archer);
        else
            RemoveInvalidOccupants();
    }

    public Vector2 GetRandomPointInside(int attempts = 16)
    {
        if (_areaCollider == null)
            return transform.position;

        Bounds b = _areaCollider.bounds;
        for (int i = 0; i < attempts; i++)
        {
            Vector2 point = new Vector2(Random.Range(b.min.x, b.max.x),
                                         Random.Range(b.min.y, b.max.y));
            if (Contains(point))
                return point;
        }

        return b.center;
    }

    private void RemoveInvalidOccupants()
    {
        _occupants.RemoveWhere(archer => archer == null || !archer.isActiveAndEnabled);
    }

    private void OnDrawGizmosSelected()
    {
        Collider2D col = _areaCollider != null ? _areaCollider : GetComponent<Collider2D>();
        if (col == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
    }
}
