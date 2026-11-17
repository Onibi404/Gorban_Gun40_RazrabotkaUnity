using System;
using UnityEngine;
using Zenject;

public sealed class EnemyFactory : IEnemyFactory
{
    private readonly DiContainer _container;

    public EnemyFactory(DiContainer container)
    {
        _container = container;
    }

    public AiAgent Create(AiAgent prefab, Vector3 position, Quaternion rotation)
    {
        if (prefab == null)
            throw new ArgumentNullException(nameof(prefab));

        // Instantiate at the final position so NavMeshAgent starts on the baked NavMesh.
        return _container.InstantiatePrefabForComponent<AiAgent>(
            prefab.gameObject, position, rotation, null);
    }
}
