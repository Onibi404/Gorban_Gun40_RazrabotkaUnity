using System.Collections;
using UnityEngine;
using UnityEngine.AI;
using Zenject;

// Spawns two existing enemy types through the project's own DI-backed factory.
// A coroutine is stopped automatically when this component is disabled or destroyed.
[DisallowMultipleComponent]
public sealed class TPSWaveSpawner : MonoBehaviour
{
    [Header("Enemy prefabs (Assets/Prefabs/Characters)")]
    [SerializeField] private AiAgent _rangedEnemyPrefab;
    [SerializeField] private AiAgent _meleeEnemyPrefab;

    [Header("At least two spawn points on the baked NavMesh")]
    [SerializeField] private Transform[] _spawnPoints;

    [Header("Wave settings")]
    [SerializeField, Min(1)] private int _waveCount = 2;
    [SerializeField, Min(0)] private int _rangedPerWave = 2;
    [SerializeField, Min(0)] private int _meleePerWave = 1;
    [SerializeField, Min(0f)] private float _firstWaveDelay = 3f;
    [SerializeField, Min(0f)] private float _secondsBetweenWaves = 12f;
    [SerializeField, Min(0f)] private float _secondsBetweenEnemies = 0.3f;
    [SerializeField, Min(0.1f)] private float _corpseLifetime = 20f;
    [SerializeField] private bool _startOnPlay = true;

    [Header("Runtime (read only)")]
    [SerializeField] private int _spawnedCount;
    [SerializeField] private int _wavesStarted;

    private IEnemyFactory _factory;
    private IGameLevelState _levelState;
    private PlayerHealth _player;
    private Coroutine _spawnRoutine;
    private int _nextSpawnPoint;

    [Inject]
    private void Construct(IEnemyFactory factory, IGameLevelState levelState, PlayerHealth player)
    {
        _factory = factory;
        _levelState = levelState;
        _player = player;
    }

    private void Start()
    {
        if (_startOnPlay)
            StartSpawning();
    }

    public void StartSpawning()
    {
        if (_spawnRoutine != null || !isActiveAndEnabled)
            return;

        if (_factory == null || _player == null)
        {
            Debug.LogError("TPSWaveSpawner: Zenject did not inject dependencies.", this);
            return;
        }

        if (_spawnPoints == null || _spawnPoints.Length < 2)
        {
            Debug.LogError("TPSWaveSpawner: assign at least two spawn points.", this);
            return;
        }

        if (_rangedEnemyPrefab == null || _meleeEnemyPrefab == null)
        {
            Debug.LogError("TPSWaveSpawner: assign both enemy prefabs.", this);
            return;
        }

        _spawnRoutine = StartCoroutine(SpawnWaves());
    }

    private IEnumerator SpawnWaves()
    {
        if (_firstWaveDelay > 0f)
            yield return new WaitForSeconds(_firstWaveDelay);

        for (int wave = 0; wave < _waveCount; wave++)
        {
            if (LevelIsFinished())
                break;

            _wavesStarted++;

            for (int i = 0; i < _rangedPerWave; i++)
            {
                if (LevelIsFinished()) break;
                SpawnOne(_rangedEnemyPrefab);
                if (_secondsBetweenEnemies > 0f)
                    yield return new WaitForSeconds(_secondsBetweenEnemies);
            }

            for (int i = 0; i < _meleePerWave; i++)
            {
                if (LevelIsFinished()) break;
                SpawnOne(_meleeEnemyPrefab);
                if (_secondsBetweenEnemies > 0f)
                    yield return new WaitForSeconds(_secondsBetweenEnemies);
            }

            if (wave < _waveCount - 1 && !LevelIsFinished())
                yield return new WaitForSeconds(_secondsBetweenWaves);
        }

        _spawnRoutine = null;
    }

    private bool LevelIsFinished()
    {
        return _levelState != null && _levelState.IsFinished;
    }

    private void SpawnOne(AiAgent prefab)
    {
        Transform point = _spawnPoints[_nextSpawnPoint % _spawnPoints.Length];
        _nextSpawnPoint++;
        if (point == null)
        {
            Debug.LogWarning("TPSWaveSpawner: one spawn point is not assigned.", this);
            return;
        }

        NavMeshHit hit;
        if (!NavMesh.SamplePosition(point.position, out hit, 2.5f, NavMesh.AllAreas))
        {
            Debug.LogWarning("TPSWaveSpawner: no baked NavMesh near " + point.name, point);
            return;
        }

        AiAgent enemy = _factory.Create(prefab, hit.position, point.rotation);
        enemy.playerTransform = _player.transform;
        var cleanup = enemy.gameObject.AddComponent<TPSSpawnedEnemyCleanup>();
        cleanup.Initialize(_corpseLifetime);
        _spawnedCount++;
    }

    private void OnDisable()
    {
        if (_spawnRoutine != null)
        {
            StopCoroutine(_spawnRoutine);
            _spawnRoutine = null;
        }
    }

    private void OnDrawGizmosSelected()
    {
        if (_spawnPoints == null) return;
        Gizmos.color = Color.cyan;
        foreach (Transform point in _spawnPoints)
        {
            if (point == null) continue;
            Gizmos.DrawWireSphere(point.position, 0.7f);
            Gizmos.DrawRay(point.position, point.forward * 1.3f);
        }
    }
}
