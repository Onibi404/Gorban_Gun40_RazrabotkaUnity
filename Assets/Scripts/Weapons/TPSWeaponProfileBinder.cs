using UnityEngine;

// Optional bridge for existing weapons. Does not replace RaycastWeapon,
// ActiveWeapon, ReloadWeapon, or existing animation controllers.
[DisallowMultipleComponent]
public sealed class TPSWeaponProfileBinder : MonoBehaviour
{
    [SerializeField] private TPSWeaponProfile _profile;
    [SerializeField] private bool _applyAtAwake = true;

    public TPSWeaponProfile Profile => _profile;

    private void Awake()
    {
        if (_applyAtAwake)
            ApplyProfile();
    }

    [ContextMenu("Apply Weapon Profile Now")]
    public void ApplyProfile()
    {
        if (_profile == null)
        {
            Debug.LogWarning("TPSWeaponProfileBinder: Profile is not assigned.", this);
            return;
        }

        if (_profile.WeaponType == TPSWeaponType.Melee)
        {
            var melee = GetComponent<MeleeWeapon>();
            if (melee == null)
            {
                Debug.LogWarning("TPSWeaponProfileBinder: MeleeWeapon not found on this object.", this);
                return;
            }

            melee.damage = _profile.Damage;
            melee.range = _profile.Range;
            return;
        }

        var weapon = GetComponent<RaycastWeapon>();
        if (weapon == null)
        {
            Debug.LogWarning("TPSWeaponProfileBinder: RaycastWeapon not found on this object.", this);
            return;
        }

        // Only standard parameters are applied here. Special mechanics
        // (pellets, grenade, penetration, etc.) require dedicated classes.
        weapon.damage = _profile.Damage;
        weapon.fireRate = Mathf.Max(1, _profile.FireRate);
        weapon.clipSize = _profile.MagazineSize;
        weapon.clipCount = _profile.ReserveMagazines;
        weapon.ammoCount = _profile.MagazineSize;
        weapon.bulletSpeed = _profile.ProjectileSpeed;
        weapon.bulletDrop = _profile.ProjectileDrop;
    }
}
