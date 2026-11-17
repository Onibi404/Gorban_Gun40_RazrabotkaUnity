using UnityEngine;

public enum TPSWeaponType
{
    Pistol,
    AssaultRifle,
    Shotgun,
    GrenadeLauncher,
    Railgun,
    Melee
}

// Stores balance and effect references outside gameplay code.
// Different firing mechanics will be connected in the next weapon step.
[CreateAssetMenu(fileName = "WeaponProfile", menuName = "TPS/Weapons/Weapon Profile")]
public sealed class TPSWeaponProfile : ScriptableObject
{
    [Header("Identity")]
    [SerializeField] private TPSWeaponType _weaponType = TPSWeaponType.Pistol;

    [Header("General balance")]
    [SerializeField, Min(0f)] private float _damage = 12f;
    [SerializeField, Min(1)] private int _fireRate = 5;
    [SerializeField, Min(0)] private int _magazineSize = 12;
    [SerializeField, Min(0)] private int _reserveMagazines = 3;
    [SerializeField, Min(0.1f)] private float _range = 100f;

    [Header("Ballistics and spread")]
    [SerializeField, Min(0f)] private float _projectileSpeed = 1000f;
    [SerializeField, Min(0f)] private float _projectileDrop = 0f;
    [SerializeField, Min(0f)] private float _spreadDegrees = 0f;

    [Header("Distinct weapon mechanics (used in later steps)")]
    [SerializeField, Range(1, 20)] private int _pelletCount = 8;
    [SerializeField, Min(0f)] private float _explosionRadius = 4f;
    [SerializeField, Range(1, 3)] private int _railPenetrations = 3;
    [SerializeField, Range(1f, 180f)] private float _meleeHalfAngle = 60f;
    [SerializeField, Min(0f)] private float _chargeDuration = 0.5f;

    [Header("Optional VFX and SFX")]
    [SerializeField] private AudioClip _shotClip;
    [SerializeField] private AudioClip _emptyClip;
    [SerializeField] private AudioClip _reloadClip;
    [SerializeField] private GameObject _projectilePrefab;
    [SerializeField] private GameObject _impactEffectPrefab;

    public TPSWeaponType WeaponType => _weaponType;
    public float Damage => _damage;
    public int FireRate => _fireRate;
    public int MagazineSize => _magazineSize;
    public int ReserveMagazines => _reserveMagazines;
    public float Range => _range;
    public float ProjectileSpeed => _projectileSpeed;
    public float ProjectileDrop => _projectileDrop;
    public float SpreadDegrees => _spreadDegrees;
    public int PelletCount => _pelletCount;
    public float ExplosionRadius => _explosionRadius;
    public int RailPenetrations => _railPenetrations;
    public float MeleeHalfAngle => _meleeHalfAngle;
    public float ChargeDuration => _chargeDuration;
    public AudioClip ShotClip => _shotClip;
    public AudioClip EmptyClip => _emptyClip;
    public AudioClip ReloadClip => _reloadClip;
    public GameObject ProjectilePrefab => _projectilePrefab;
    public GameObject ImpactEffectPrefab => _impactEffectPrefab;
}
