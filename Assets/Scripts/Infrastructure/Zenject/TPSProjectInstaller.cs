using Zenject;

public sealed class TPSProjectInstaller : MonoInstaller
{
    public override void InstallBindings()
    {
        Container.BindInterfacesAndSelfTo<GameAudioService>().AsSingle();
    }
}
