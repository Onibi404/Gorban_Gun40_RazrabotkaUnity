using System;
using UnityEngine;
using Zenject;

public sealed class GameAudioService : IGameAudioService, IInitializable, IDisposable
{
    private const string SaveKey = "TPS_MasterVolume_V2";
    private float _masterVolume;

    public float MasterVolume => _masterVolume;

    [Inject]
    public GameAudioService(TPSGameSettings settings)
    {
        _masterVolume = Mathf.Clamp01(
            PlayerPrefs.GetFloat(SaveKey, settings.DefaultMasterVolume));
    }

    public void Initialize()
    {
        AudioListener.volume = _masterVolume;
    }

    public void SetMasterVolume(float value)
    {
        _masterVolume = Mathf.Clamp01(value);
        AudioListener.volume = _masterVolume;
        PlayerPrefs.SetFloat(SaveKey, _masterVolume);
        PlayerPrefs.Save();
    }

    public void Dispose()
    {
        // Nothing to release; Zenject manages this project's service lifetime.
    }
}
