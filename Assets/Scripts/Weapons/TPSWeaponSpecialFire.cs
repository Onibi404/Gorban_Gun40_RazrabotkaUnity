using System;
using System.Collections.Generic;
using UnityEngine;
using Zenject;

// Additional firing mechanics. The original RaycastWeapon is not modified.
// Installed on three new weapon prefabs generated via the editor menu.
[DisallowMultipleComponent]
[RequireComponent(typeof(RaycastWeapon))]
public sealed class TPSWeaponSpecialFire : MonoBehaviour
{
    private RaycastWeapon _weapon;
    private TPSWeaponProfileBinder _binder;
    private AudioSource _audio;
    private IProjectileFactory _projectileFactory;
    private float _nextShot;
    private float _chargeStarted = -1f;
    // Allocated once per weapon instance; reused every shot.
    private readonly RaycastHit[] _shotgunHits = new RaycastHit[16];
    private readonly RaycastHit[] _railHits = new RaycastHit[96];
    private readonly HashSet<Health> _railAlreadyHit = new HashSet<Health>();

    private sealed class HitDistanceComparer : IComparer<RaycastHit>
    {
        public static readonly HitDistanceComparer Instance = new HitDistanceComparer();
        public int Compare(RaycastHit a, RaycastHit b) { return a.distance.CompareTo(b.distance); }
    }

    [Inject]
    private void Construct(IProjectileFactory projectileFactory)
    {
        _projectileFactory = projectileFactory;
    }

    public bool IsSpecial
    {
        get
        {
            var p = _binder ? _binder.Profile : null;
            return p && (p.WeaponType == TPSWeaponType.Shotgun ||
                         p.WeaponType == TPSWeaponType.GrenadeLauncher ||
                         p.WeaponType == TPSWeaponType.Railgun);
        }
    }

    private void Awake()
    {
        _weapon = GetComponent<RaycastWeapon>();
        _binder = GetComponent<TPSWeaponProfileBinder>();
        _audio = GetComponent<AudioSource>();
    }

    // Return true only when a shot was actually fired (for updating ammo UI).
    public bool Tick(bool fireHeld, Vector3 target)
    {
        if (!IsSpecial) return false;
        var profile = _binder.Profile;

        if (!fireHeld)
        {
            _chargeStarted = -1f;
            return false;
        }
        if (Time.time < _nextShot) return false;

        if (profile.WeaponType == TPSWeaponType.Railgun && profile.ChargeDuration > 0f)
        {
            if (_chargeStarted < 0f) _chargeStarted = Time.time;
            if (Time.time - _chargeStarted < profile.ChargeDuration) return false;
        }
        _chargeStarted = -1f;
        _nextShot = Time.time + 1f / Mathf.Max(1, profile.FireRate);

        if (_weapon.ammoCount <= 0)
        {
            PlayClip(profile.EmptyClip);
            return false;
        }
        _weapon.ammoCount--;
        if (_weapon.muzzleFlash != null)
            foreach (var flash in _weapon.muzzleFlash) if (flash) flash.Emit(1);

        PlayClip(profile.ShotClip);
        if (_weapon.recoil && _weapon.recoil.recoilPattern != null &&
            _weapon.recoil.recoilPattern.Length > 0)
        {
            _weapon.recoil.GenerateRecoil(_weapon.weaponName);
        }

        Transform origin = _weapon.raycastOrigin ? _weapon.raycastOrigin : transform;
        Vector3 forward = (target - origin.position).normalized;
        if (forward.sqrMagnitude < 0.01f) forward = origin.forward;

        switch (profile.WeaponType)
        {
            case TPSWeaponType.Shotgun:
                FireShotgun(origin.position, forward, profile);
                break;
            case TPSWeaponType.GrenadeLauncher:
                LaunchGrenade(origin.position, forward, profile);
                break;
            case TPSWeaponType.Railgun:
                FireRailgun(origin.position, forward, profile);
                break;
        }
        return true;
    }

    private void PlayClip(AudioClip clip)
    {
        if (_audio && clip) _audio.PlayOneShot(clip);
    }

    private void FireShotgun(Vector3 from, Vector3 forward, TPSWeaponProfile p)
    {
        Quaternion aim = Quaternion.LookRotation(forward);
        int pellets = Mathf.Max(1, p.PelletCount);
        for (int i = 0; i < pellets; i++)
        {
            Vector2 jitter = UnityEngine.Random.insideUnitCircle * p.SpreadDegrees;
            Vector3 direction = (aim * Quaternion.Euler(jitter.y, jitter.x, 0f)) * Vector3.forward;
            int count = Physics.RaycastNonAlloc(from, direction, _shotgunHits, p.Range,
                _weapon.layerMask, QueryTriggerInteraction.Ignore);
            int closestIndex = -1;
            float closestDistance = float.PositiveInfinity;
            for (int j = 0; j < count; j++)
            {
                if (_shotgunHits[j].distance < closestDistance)
                {
                    closestDistance = _shotgunHits[j].distance;
                    closestIndex = j;
                }
            }
            if (closestIndex >= 0)
            {
                RaycastHit hit = _shotgunHits[closestIndex];
                ApplyDamage(hit.collider, direction, p.Damage);
                SpawnImpact(hit.point, hit.normal, p);
            }
            Debug.DrawRay(from, direction * p.Range, Color.yellow, 0.2f);
        }
    }

    private void FireRailgun(Vector3 from, Vector3 forward, TPSWeaponProfile p)
    {
        int count = Physics.RaycastNonAlloc(from, forward, _railHits, p.Range,
            _weapon.layerMask, QueryTriggerInteraction.Ignore);
        Array.Sort(_railHits, 0, count, HitDistanceComparer.Instance);
        _railAlreadyHit.Clear();
        int pierced = 0;
        float visualDistance = p.Range;
        for (int i = 0; i < count; i++)
        {
            RaycastHit hit = _railHits[i];
            if (!hit.collider) continue;
            Health health = hit.collider.GetComponentInParent<Health>();
            if (!health)
            {
                visualDistance = hit.distance;
                break; // A wall blocks the rail beam.
            }
            if (health.transform.root == transform.root || !_railAlreadyHit.Add(health)) continue;
            health.MarkLastHitByPlayer(transform.root.CompareTag("Player"));
            health.TakeDamage(p.Damage, forward);
            SpawnImpact(hit.point, hit.normal, p);
            pierced++;
            visualDistance = hit.distance;
            if (pierced >= Mathf.Max(1, p.RailPenetrations)) break;
        }
        Debug.DrawRay(from, forward * visualDistance, Color.cyan, 0.5f);
    }

    private void LaunchGrenade(Vector3 from, Vector3 forward, TPSWeaponProfile p)
    {
        GameObject instance = null;
        var rotation = Quaternion.LookRotation(forward);
        Vector3 position = from + forward * 0.65f;
        if (p.ProjectilePrefab)
        {
            instance = _projectileFactory != null
                ? _projectileFactory.Create(p.ProjectilePrefab, position, rotation)
                : Instantiate(p.ProjectilePrefab, position, rotation);
        }
        else
        {
            // Safe fallback if the generated grenade prefab was not assigned.
            instance = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            instance.name = "TPS_Grenade_Fallback";
            instance.transform.position = position;
            instance.transform.localScale = Vector3.one * 0.25f;
            instance.AddComponent<Rigidbody>();
            instance.AddComponent<TPSGrenadeProjectile>();
        }

        TPSGrenadeProjectile projectile = instance.GetComponent<TPSGrenadeProjectile>();
        if (!projectile) projectile = instance.AddComponent<TPSGrenadeProjectile>();
        projectile.Launch(forward * Mathf.Max(2f, p.ProjectileSpeed) + Vector3.up * 2f,
            p.Damage, p.ExplosionRadius, transform.root, p.ImpactEffectPrefab);
    }

    private void ApplyDamage(Collider target, Vector3 direction, float damage)
    {
        Health health = target.GetComponentInParent<Health>();
        if (!health || health.transform.root == transform.root) return;
        health.MarkLastHitByPlayer(transform.root.CompareTag("Player"));
        health.TakeDamage(damage, direction);
    }

    private void SpawnImpact(Vector3 point, Vector3 normal, TPSWeaponProfile p)
    {
        if (p.ImpactEffectPrefab)
            TPSImpactPool.Spawn(p.ImpactEffectPrefab, point, Quaternion.LookRotation(normal), 3f);
    }
}
