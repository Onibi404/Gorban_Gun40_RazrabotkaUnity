using System.Collections.Generic;
using Netologia;
using UnityEngine;

namespace Netologia.TowerDefence
{


    [RequireComponent(typeof(Tower))]
    public class BarrackSpawner : MonoBehaviour
    {
        [Header("Archer production")]
        [SerializeField] private Unit _archerPrefab;
        [SerializeField] private Transform _spawnPoint;
        [SerializeField, Min(1)] private int _maxAliveArchers = 5;
        [SerializeField, Min(0.1f)] private float _firstSpawnDelay = 3f;
        [SerializeField, Min(1f)] private float _archerHealth = 20f;
        [SerializeField, Min(0f), Tooltip("0 = archers live until killed or the barrack is sold")]
        private float _archerLifetime = 0f;

        private readonly List<Unit> _alive = new List<Unit>();
        private readonly Dictionary<Unit, float> _spawnTimes = new Dictionary<Unit, float>();

        private Tower _tower;
        private float _spawnTimer;

        private void Awake()
        {
            _tower = GetComponent<Tower>();
        }

        private void OnEnable()
        {
            _spawnTimer = _firstSpawnDelay;
        }

        private void Update()
        {
            if (!TimeManager.IsGame || _archerPrefab == null)
                return;

            RemoveLostArchers();
            _spawnTimer -= TimeManager.DeltaTime;

            if (_spawnTimer > 0f)
                return;

            float interval = _tower != null && _tower.Level >= 0
                ? Mathf.Max(0.1f, _tower.AttackDelay)
                : 10f;
            _spawnTimer = interval;

            if (_alive.Count >= _maxAliveArchers)
                return;

            if (SpawnArcher() && _tower != null && _tower.Level >= 0)
                _tower.Attack();
        }

        private bool SpawnArcher()
        {
            Vector3 position = _spawnPoint != null ? _spawnPoint.position : transform.position;
            Quaternion rotation = _spawnPoint != null ? _spawnPoint.rotation : Quaternion.identity;

            Unit archer = Instantiate(_archerPrefab, position, rotation);
            if (archer == null)
                return false;


            archer.CurrentHealth = _archerHealth;

            ArcherAI ai = archer.GetComponent<ArcherAI>();
            if (ai == null || !ai.HasWalkableArea)
            {
                Debug.LogWarning("[BarrackSpawner] Archer could not claim a walkable ArcherArea.", this);
                Destroy(archer.gameObject);
                return false;
            }

            _alive.Add(archer);
            _spawnTimes[archer] = TimeManager.Time;
            return true;
        }

        private void RemoveLostArchers()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                Unit archer = _alive[i];
                bool inactive = archer == null || !archer.gameObject.activeInHierarchy;
                bool dead = !inactive && archer.IsDead;
                bool expired = false;

                if (!inactive && !dead && _archerLifetime > 0f &&
                    _spawnTimes.TryGetValue(archer, out float born))
                {
                    expired = TimeManager.Time - born >= _archerLifetime;
                }

                if (!inactive && !dead && !expired)
                    continue;

                _alive.RemoveAt(i);
                if (archer == null)
                    continue;

                _spawnTimes.Remove(archer);
                if (dead || expired)
                    Destroy(archer.gameObject);
            }
        }

        private void OnDisable()
        {
            
            for (int i = 0; i < _alive.Count; i++)
            {
                Unit archer = _alive[i];
                if (archer != null)
                    Destroy(archer.gameObject);
            }

            _alive.Clear();
            _spawnTimes.Clear();
        }
    }
}
