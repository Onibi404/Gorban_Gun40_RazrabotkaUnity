using UnityEngine;

[CreateAssetMenu(fileName = "TPSGameSettings", menuName = "TPS/DI/Game Settings")]
public sealed class TPSGameSettings : ScriptableObject
{
    [SerializeField, Range(0f, 1f)] private float _defaultMasterVolume = 1f;
    public float DefaultMasterVolume => _defaultMasterVolume;
}
