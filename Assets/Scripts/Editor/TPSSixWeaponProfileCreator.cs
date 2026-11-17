using UnityEditor;
using UnityEngine;

// Generate 6 ScriptableObject templates without manually creating each file.
public static class TPSSixWeaponProfileCreator
{
    private const string Folder = "Assets/_Project/Configs/Weapons";

    [MenuItem("Tools/TPS Shooter/Create 6 Weapon Profiles")]
    public static void CreateProfiles()
    {
        EnsureFolder("Assets", "_Project");
        EnsureFolder("Assets/_Project", "Configs");
        EnsureFolder("Assets/_Project/Configs", "Weapons");

        Create("01_Pistol", TPSWeaponType.Pistol, 12f, 5, 12, 999, 100f, 1000f, 0f, 0f, 8, 4f, 3, 60f, 0f);
        Create("02_AssaultRifle", TPSWeaponType.AssaultRifle, 9f, 12, 30, 4, 125f, 1000f, 0f, 2.5f, 8, 4f, 3, 60f, 0f);
        Create("03_Shotgun", TPSWeaponType.Shotgun, 6f, 1, 6, 4, 25f, 1000f, 0f, 12f, 10, 4f, 3, 60f, 0f);
        Create("04_GrenadeLauncher", TPSWeaponType.GrenadeLauncher, 45f, 1, 1, 8, 60f, 22f, 9.81f, 0f, 8, 5f, 3, 60f, 0f);
        Create("05_Railgun", TPSWeaponType.Railgun, 30f, 1, 5, 3, 150f, 1000f, 0f, 0f, 8, 4f, 3, 60f, 1.1f);
        Create("06_Melee", TPSWeaponType.Melee, 25f, 2, 0, 0, 1.8f, 0f, 0f, 0f, 8, 0f, 1, 60f, 0f);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("TPS Shooter", "Weapon profiles are ready in " + Folder, "OK");
    }

    private static void EnsureFolder(string parent, string child)
    {
        string path = parent + "/" + child;
        if (!AssetDatabase.IsValidFolder(path))
            AssetDatabase.CreateFolder(parent, child);
    }

    private static void Create(string name, TPSWeaponType type, float dmg, int rate,
        int clip, int reserve, float range, float speed, float drop, float spread,
        int pellets, float blast, int penetrations, float meleeAngle, float charge)
    {
        string path = Folder + "/" + name + ".asset";
        if (AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(path) != null)
            return; // Preserve user edits if the menu is called twice.

        TPSWeaponProfile profile = ScriptableObject.CreateInstance<TPSWeaponProfile>();
        SerializedObject so = new SerializedObject(profile);
        so.FindProperty("_weaponType").enumValueIndex = (int)type;
        so.FindProperty("_damage").floatValue = dmg;
        so.FindProperty("_fireRate").intValue = rate;
        so.FindProperty("_magazineSize").intValue = clip;
        so.FindProperty("_reserveMagazines").intValue = reserve;
        so.FindProperty("_range").floatValue = range;
        so.FindProperty("_projectileSpeed").floatValue = speed;
        so.FindProperty("_projectileDrop").floatValue = drop;
        so.FindProperty("_spreadDegrees").floatValue = spread;
        so.FindProperty("_pelletCount").intValue = pellets;
        so.FindProperty("_explosionRadius").floatValue = blast;
        so.FindProperty("_railPenetrations").intValue = penetrations;
        so.FindProperty("_meleeHalfAngle").floatValue = meleeAngle;
        so.FindProperty("_chargeDuration").floatValue = charge;
        so.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.CreateAsset(profile, path);
    }
}
