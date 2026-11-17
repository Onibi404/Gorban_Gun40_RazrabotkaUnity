using UnityEngine;

// Added only to enemies spawned by TPSWaveSpawner.
// Removes a defeated enemy's ragdoll after a configurable delay.
[DisallowMultipleComponent]
public sealed class TPSSpawnedEnemyCleanup : MonoBehaviour
{
    private AiHealth _health;
    private float _checkAt;
    private float _corpseLifetime = 20f;

    public void Initialize(float corpseLifetime)
    {
        _corpseLifetime = Mathf.Max(0.1f, corpseLifetime);
    }

    private void Awake()
    {
        _health = GetComponent<AiHealth>();
    }

    private void Update()
    {
        if (Time.time < _checkAt)
            return;

        _checkAt = Time.time + 0.5f;

        if (_health != null && _health.IsDead())
        {
            Destroy(gameObject, _corpseLifetime);
            enabled = false;
        }
    }
}
