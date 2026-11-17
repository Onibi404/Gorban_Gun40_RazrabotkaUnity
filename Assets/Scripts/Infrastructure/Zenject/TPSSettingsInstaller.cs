using System;
using UnityEngine;
using Zenject;

[CreateAssetMenu(fileName = "TPSSettingsInstaller", menuName = "TPS/DI/Settings Installer")]
public sealed class TPSSettingsInstaller : ScriptableObjectInstaller<TPSSettingsInstaller>
{
    [SerializeField] private TPSGameSettings _settings;

    public override void InstallBindings()
    {
        if (_settings == null)
            throw new InvalidOperationException("TPSSettingsInstaller: assign TPSGameSettings asset.");

        Container.Bind<TPSGameSettings>().FromInstance(_settings).AsSingle();
    }
}
