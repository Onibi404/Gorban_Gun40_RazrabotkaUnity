using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

// One-click editor setup. Run only after step 38-42 profiles were created.
public static class TPSQuickArsenalSetup
{
    private const string WeaponsFolder = "Assets/Prefabs/Weapons";
    private const string PickupsFolder = "Assets/Prefabs/Pickups";
    private const string ProfilesFolder = "Assets/_Project/Configs/Weapons";

    [MenuItem("Tools/TPS Shooter/Setup 6 Weapons (43-50)")]
    public static void Setup()
    {
        if (!AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfilesFolder + "/03_Shotgun.asset") ||
            !AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfilesFolder + "/04_GrenadeLauncher.asset") ||
            !AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfilesFolder + "/05_Railgun.asset"))
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "First run Tools > TPS Shooter > Create 6 Weapon Profiles (step 40).", "OK");
            return;
        }

        CreateWeapon("Weapon_Assault", "Weapon_TPS_Shotgun", "03_Shotgun", ActiveWeapon.WeaponSlot.Shotgun);
        CreateWeapon("Weapon_BFG", "Weapon_TPS_Grenade", "04_GrenadeLauncher", ActiveWeapon.WeaponSlot.GrenadeLauncher);
        CreateWeapon("Weapon_BFG", "Weapon_TPS_Railgun", "05_Railgun", ActiveWeapon.WeaponSlot.Railgun);

        CreateGrenadePrefab();
        CreatePickup("Pickup_Assault", "Pickup_TPS_Shotgun", "Weapon_TPS_Shotgun");
        CreatePickup("Pickup_BFG", "Pickup_TPS_Grenade", "Weapon_TPS_Grenade");
        CreatePickup("Pickup_BFG", "Pickup_TPS_Railgun", "Weapon_TPS_Railgun");

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        SetupScene();
        EditorUtility.DisplayDialog("TPS Shooter",
            "Done! Three weapons and pickups are prepared. Check the TPS_NewWeaponPickups object in scene_sandbox. Press Cmd+S to save the scene.", "OK");
    }

    private static void CreateWeapon(string srcName, string dstName, string profileName,
                                     ActiveWeapon.WeaponSlot slot)
    {
        string from = WeaponsFolder + "/" + srcName + ".prefab";
        string to = WeaponsFolder + "/" + dstName + ".prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(to))
        {
            if (!AssetDatabase.CopyAsset(from, to))
            {
                Debug.LogError("Could not copy prefab " + from);
                return;
            }
        }
        GameObject root = PrefabUtility.LoadPrefabContents(to);
        try
        {
            RaycastWeapon weapon = root.GetComponentInChildren<RaycastWeapon>(true);
            if (!weapon) { Debug.LogError("RaycastWeapon not found in " + dstName); return; }
            weapon.weaponSlot = slot;
            // weapon.weaponName intentionally stays from the original animation set.
            TPSWeaponProfileBinder binder = weapon.GetComponent<TPSWeaponProfileBinder>();
            if (!binder) binder = weapon.gameObject.AddComponent<TPSWeaponProfileBinder>();
            SerializedObject serializedBinder = new SerializedObject(binder);
            serializedBinder.FindProperty("_profile").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfilesFolder + "/" + profileName + ".asset");
            serializedBinder.ApplyModifiedPropertiesWithoutUndo();
            if (!weapon.GetComponent<TPSWeaponSpecialFire>())
                weapon.gameObject.AddComponent<TPSWeaponSpecialFire>();
            EditorUtility.SetDirty(weapon);
            PrefabUtility.SaveAsPrefabAsset(root, to);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        // Reuse sound already assigned in the original rifle/BFG prefab.
        var profile = AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfilesFolder + "/" + profileName + ".asset");
        var sourceRoot = AssetDatabase.LoadAssetAtPath<GameObject>(from);
        if (!profile || !sourceRoot) return;
        var sourceWeapon = sourceRoot.GetComponentInChildren<RaycastWeapon>(true);
        if (!sourceWeapon) return;
        SerializedObject source = new SerializedObject(sourceWeapon);
        SerializedProperty clips = source.FindProperty("shotClips");
        SerializedObject dest = new SerializedObject(profile);
        SerializedProperty shot = dest.FindProperty("_shotClip");
        if (clips != null && clips.isArray && clips.arraySize > 0 && shot != null && !shot.objectReferenceValue)
        {
            shot.objectReferenceValue = clips.GetArrayElementAtIndex(0).objectReferenceValue;
            dest.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(profile);
        }
    }

    private static void CreateGrenadePrefab()
    {
        string path = WeaponsFolder + "/TPS_GrenadeProjectile.prefab";
        GameObject grenadePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (!grenadePrefab)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = "TPS_GrenadeProjectile";
            go.transform.localScale = Vector3.one * 0.25f;
            go.AddComponent<Rigidbody>();
            go.AddComponent<TPSGrenadeProjectile>();
            grenadePrefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
        }
        TPSWeaponProfile profile = AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfilesFolder + "/04_GrenadeLauncher.asset");
        if (profile && grenadePrefab)
        {
            SerializedObject so = new SerializedObject(profile);
            var projectile = so.FindProperty("_projectilePrefab");
            if (projectile != null && !projectile.objectReferenceValue)
            {
                projectile.objectReferenceValue = grenadePrefab;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
            }
        }
    }

    private static void CreatePickup(string originalName, string pickupName, string weaponName)
    {
        string original = PickupsFolder + "/" + originalName + ".prefab";
        string to = PickupsFolder + "/" + pickupName + ".prefab";
        if (!AssetDatabase.LoadAssetAtPath<GameObject>(to))
        {
            if (!AssetDatabase.CopyAsset(original, to))
            {
                Debug.LogError("Could not copy pickup " + original);
                return;
            }
        }
        GameObject root = PrefabUtility.LoadPrefabContents(to);
        try
        {
            var pickup = root.GetComponentInChildren<WeaponPickup>(true);
            var weaponRoot = AssetDatabase.LoadAssetAtPath<GameObject>(WeaponsFolder + "/" + weaponName + ".prefab");
            var weapon = weaponRoot ? weaponRoot.GetComponentInChildren<RaycastWeapon>(true) : null;
            if (!pickup || !weapon)
            {
                Debug.LogError("Pickup or weapon missing: " + pickupName + ", " + weaponName);
                return;
            }
            pickup.weaponFab = weapon;
            EditorUtility.SetDirty(pickup);
            PrefabUtility.SaveAsPrefabAsset(root, to);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void SetupScene()
    {
        // Safe: only touches the currently open gameplay scene, never MainMenu.
        if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name != "scene_sandbox")
        {
            Debug.LogWarning("Open scene_sandbox and run Setup 6 Weapons again to place pickups in the scene.");
            return;
        }
        ActiveWeapon player = Object.FindObjectOfType<ActiveWeapon>();
        if (!player)
        {
            Debug.LogWarning("ActiveWeapon player not found. Prefabs created; drag the three TPS pickups into the scene manually.");
            return;
        }
        if (!player.GetComponent<TPSPlayerMelee>())
            Undo.AddComponent<TPSPlayerMelee>(player.gameObject);

        Transform group = GameObject.Find("TPS_NewWeaponPickups")?.transform;
        if (!group)
        {
            var holder = new GameObject("TPS_NewWeaponPickups");
            Undo.RegisterCreatedObjectUndo(holder, "TPS weapons pickup group");
            group = holder.transform;
        }
        string[] names = {"Pickup_TPS_Shotgun", "Pickup_TPS_Grenade", "Pickup_TPS_Railgun"};
        for (int i = 0; i < names.Length; i++)
        {
            if (group.Find(names[i])) continue;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PickupsFolder + "/" + names[i] + ".prefab");
            if (!prefab) continue;
            GameObject instance = PrefabUtility.InstantiatePrefab(prefab) as GameObject;
            if (!instance) continue;
            instance.name = names[i];
            Undo.RegisterCreatedObjectUndo(instance, "TPS new weapon pickup");
            instance.transform.SetParent(group, true);
            Vector3 pos = player.transform.position + player.transform.forward * (3.5f + i * 2.5f)
                          + player.transform.right * (i - 1) * 1.5f;
            RaycastHit hit;
            if (Physics.Raycast(pos + Vector3.up * 15f, Vector3.down, out hit, 30f))
                pos.y = hit.point.y + 0.6f;
            instance.transform.position = pos;
        }
        EditorSceneManager.MarkSceneDirty(UnityEngine.SceneManagement.SceneManager.GetActiveScene());
    }
}
