using Zenject;

public sealed class WeaponFactory : IWeaponFactory
{
    private readonly DiContainer _container;

    public WeaponFactory(DiContainer container)
    {
        _container = container;
    }

    public RaycastWeapon Create(RaycastWeapon prefab)
    {
        return _container.InstantiatePrefabForComponent<RaycastWeapon>(prefab.gameObject);
    }
}
