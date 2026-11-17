using UnityEngine.Rendering;
using Zenject;

public sealed class TPSSceneInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        // Existing scene components, injected into other scene objects.
        Container.Bind<PlayerInputReader>().FromComponentInHierarchy().AsSingle();
        Container.Bind<PlayerHealth>().FromComponentInHierarchy().AsSingle();
        Container.Bind<CameraManager>().FromComponentInHierarchy().AsSingle();
        Container.Bind<Volume>().FromComponentInHierarchy().AsSingle();
        Container.Bind<AmmoWidget>().FromComponentInHierarchy().AsSingle();
        Container.Bind<CrossHairTarget>().FromComponentInHierarchy().AsSingle();

        // Services have independent lifetimes for each level instance.
        Container.Bind<IGameInputService>().To<GameInputService>().AsSingle();
        Container.Bind<IGameLevelState>().To<GameLevelState>().AsSingle();

        // Our own factories (not Zenject PlaceholderFactory).
        Container.Bind<IWeaponFactory>().To<WeaponFactory>().AsSingle();
        Container.Bind<IEnemyFactory>().To<EnemyFactory>().AsSingle();
        Container.Bind<IProjectileFactory>().To<ProjectileFactory>().AsSingle();
        Container.Bind<IPickupFactory>().To<PickupFactory>().AsSingle();
    }
}
