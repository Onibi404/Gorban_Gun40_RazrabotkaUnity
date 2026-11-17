using System;
using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;
using Zenject;

public class ActiveWeapon : MonoBehaviour
{
    public enum WeaponSlot {
        Primary = 0,
        Secondary = 1,
        Shotgun = 2,
        GrenadeLauncher = 3,
        Railgun = 4
    }

    public Animator rigController;
    public Transform[] weaponSlots;
    public bool isChangingWeapon;

    RaycastWeapon[] equipped_weapons = new RaycastWeapon[5];
    TPSWeaponSpecialFire[] specialWeapons = new TPSWeaponSpecialFire[5];
    CharacterAiming characterAiming;
    AmmoWidget ammoWidget;
    Transform crossHairTarget;
    ReloadWeapon reload;
    PlayerInputReader inputReader;

    [Inject]
    private void Construct(AmmoWidget ui, CrossHairTarget target)
    {
        ammoWidget = ui;
        crossHairTarget = target.transform;
    }

    int activeWeaponIndex = -1;
    bool isHolstered = false;

    private void Awake() {
        characterAiming = GetComponent<CharacterAiming>();
        reload = GetComponent<ReloadWeapon>();
        inputReader = GetComponent<PlayerInputReader>();
    }

    // Start is called before the first frame update
    void Start()
    {
        RaycastWeapon existingWeapon = GetComponentInChildren<RaycastWeapon>();
        if (existingWeapon) {
            Equip(existingWeapon);
        }
    }

    public bool IsFiring() {
        RaycastWeapon currentWeapon = GetActiveWeapon();
        if (!currentWeapon) {
            return false;
        }
        return currentWeapon.isFiring;
    }

    public RaycastWeapon GetActiveWeapon() {
        return GetWeapon(activeWeaponIndex);
    }

    RaycastWeapon GetWeapon(int index) {
        if (index < 0 || index >= equipped_weapons.Length) {
            return null;
        }
        return equipped_weapons[index];
    }

    // Update is called once per frame
    void Update()
    {
        var weapon = GetWeapon(activeWeaponIndex);
        bool notSprinting = rigController.GetCurrentAnimatorStateInfo(2).shortNameHash == Animator.StringToHash("not_sprinting");
        bool canFire = !isHolstered && notSprinting && !reload.isReloading;
        if (weapon) {
            TPSWeaponSpecialFire special = specialWeapons[activeWeaponIndex];
            if (special && special.IsSpecial) {
                weapon.StopFiring();
                if (special.Tick(inputReader.FireHeld && canFire, crossHairTarget.position)
                    && ammoWidget) {
                    ammoWidget.Refresh(weapon.ammoCount, weapon.clipCount);
                }
            } else {
                if (inputReader.FireHeld && canFire && !weapon.isFiring)
                    weapon.StartFiring();
                if (!inputReader.FireHeld || !canFire)
                    weapon.StopFiring();
            }
            weapon.UpdateWeapon(Time.deltaTime, crossHairTarget.position);
        }

        if (inputReader.HolsterPressed) {
            ToggleActiveWeapon();
        }

        if (inputReader.Weapon1Pressed) {
            SetActiveWeapon(WeaponSlot.Primary);
        }
        if (inputReader.Weapon2Pressed) {
            SetActiveWeapon(WeaponSlot.Secondary);
        }
        // New Input System keyboard keys, without changing already-working
        // Weapon1/Weapon2 actions or their temporary fallback.
        if (Time.timeScale > 0f && Cursor.lockState == CursorLockMode.Locked
            && Keyboard.current != null) {
            if (Keyboard.current.digit3Key.wasPressedThisFrame) SetActiveWeapon(WeaponSlot.Shotgun);
            if (Keyboard.current.digit4Key.wasPressedThisFrame) SetActiveWeapon(WeaponSlot.GrenadeLauncher);
            if (Keyboard.current.digit5Key.wasPressedThisFrame) SetActiveWeapon(WeaponSlot.Railgun);
        }

    }

    private void OnDisable() {
        var weapon = GetActiveWeapon();
        if (weapon) weapon.StopFiring();
    }

    public void Equip(RaycastWeapon newWeapon) {
        if (!newWeapon) return;
        int index = (int)newWeapon.weaponSlot;
        if (index < 0 || index >= equipped_weapons.Length) {
            Debug.LogWarning("Unknown weapon slot: " + index, newWeapon);
            return;
        }
        var oldWeapon = GetWeapon(index);
        if (oldWeapon && oldWeapon != newWeapon)
            Destroy(oldWeapon.gameObject);

        // The original rig has two anchors. All additional ranged weapons
        // reuse the Primary anchor, and inactive instances are disabled.
        int anchorIndex = index == (int)WeaponSlot.Secondary ? 1 : 0;
        if (weaponSlots == null || weaponSlots.Length <= anchorIndex || !weaponSlots[anchorIndex]) {
            Debug.LogError("ActiveWeapon: existing weaponSlots are not configured.", this);
            return;
        }
        if (newWeapon.recoil) {
            newWeapon.recoil.characterAiming = characterAiming;
            newWeapon.recoil.animator = rigController;
        }
        newWeapon.transform.SetParent(weaponSlots[anchorIndex], false);
        equipped_weapons[index] = newWeapon;
        specialWeapons[index] = newWeapon.GetComponent<TPSWeaponSpecialFire>();
        newWeapon.gameObject.SetActive(index == activeWeaponIndex || activeWeaponIndex == -1);

        if (index == activeWeaponIndex) {
            newWeapon.gameObject.SetActive(true);
        } else {
            SetActiveWeapon(newWeapon.weaponSlot);
        }
        if (ammoWidget) ammoWidget.Refresh(newWeapon.ammoCount, newWeapon.clipCount);
    }

    void ToggleActiveWeapon() {
        bool isHolstered = rigController.GetBool("holster_weapon");
        if (isHolstered) {
            StartCoroutine(ActivateWeapon(activeWeaponIndex));
        } else {
            StartCoroutine(HolsterWeapon(activeWeaponIndex));
        }
    }

    void SetActiveWeapon(WeaponSlot weaponSlot) {
        int previous = activeWeaponIndex;
        int next = (int)weaponSlot;
        if (previous == next || isChangingWeapon || !GetWeapon(next)) return;
        StartCoroutine(SwitchWeapon(previous, next));
    }

    IEnumerator SwitchWeapon(int holsterIndex, int activateIndex) {
        if (rigController) rigController.SetInteger("weapon_index", activateIndex <= 1 ? activateIndex : 0);
        var oldWeapon = GetWeapon(holsterIndex);
        if (oldWeapon) oldWeapon.StopFiring();
        yield return StartCoroutine(HolsterWeapon(holsterIndex));
        if (oldWeapon) oldWeapon.gameObject.SetActive(false);
        var newWeapon = GetWeapon(activateIndex);
        if (!newWeapon) yield break;
        newWeapon.gameObject.SetActive(true);
        yield return StartCoroutine(ActivateWeapon(activateIndex));
        activeWeaponIndex = activateIndex;
        if (ammoWidget) ammoWidget.Refresh(newWeapon.ammoCount, newWeapon.clipCount);
    }

    IEnumerator HolsterWeapon(int index) {
        isChangingWeapon = true;
        isHolstered = true;
        var weapon = GetWeapon(index);
        if (weapon) {
            rigController.SetBool("holster_weapon", true);
            do {
                yield return new WaitForSeconds(0.05f);
            } while (rigController.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1.0f);
        }
        isChangingWeapon = false;
    }

    IEnumerator ActivateWeapon(int index) {
        isChangingWeapon = true;
        var weapon = GetWeapon(index);
        if (weapon) {
            rigController.SetBool("holster_weapon", false);
            rigController.Play("weapon_" + weapon.weaponName + "_equip");
            do {
                yield return new WaitForSeconds(0.05f);
            } while (rigController.GetCurrentAnimatorStateInfo(0).normalizedTime <= 1.0f);
            isHolstered = false;
        }
        isChangingWeapon = false;
    }

    public void DropWeapon() {
        var currentWeapon = GetActiveWeapon();
        if (currentWeapon) {
            currentWeapon.transform.SetParent(null);
            currentWeapon.gameObject.GetComponent<BoxCollider>().enabled = true;
            currentWeapon.gameObject.AddComponent<Rigidbody>();
            equipped_weapons[activeWeaponIndex] = null;
            specialWeapons[activeWeaponIndex] = null;
        }
    }

    public void RefillAmmo(int clipCount) {
        var weapon = GetActiveWeapon();
        if (weapon) {
            weapon.clipCount += clipCount;
            ammoWidget.Refresh(weapon.ammoCount, weapon.clipCount);
        }
    }
}
