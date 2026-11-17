using UnityEngine;
using Zenject;

public sealed class ProjectileFactory : IProjectileFactory
{
    private readonly DiContainer _container;

    public ProjectileFactory(DiContainer container)
    {
        _container = container;
    }

    public GameObject Create(GameObject prefab, Vector3 position, Quaternion rotation)
    {
        GameObject instance = _container.InstantiatePrefab(prefab);
        instance.transform.SetPositionAndRotation(position, rotation);
        return instance;
    }
}
