using System;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Installs three original 3D weapon models into existing working prefabs.
/// Leaves gameplay components, muzzle positions, slots, animations and pickup logic unchanged.
/// Safe to repeat after model or material changes (old TPS visuals are replaced).
/// </summary>
public static class TPSArsenalVisualInstaller
{
    private const string AssetsRoot = "Assets/_Project/Models/TPSArsenal";
    private const string MaterialFolder = AssetsRoot + "/Materials";
    private const string WeaponFolder = "Assets/Prefabs/Weapons";
    private const string PickupFolder = "Assets/Prefabs/Pickups";
    private const string ProfileFolder = "Assets/_Project/Configs/Weapons";

    private struct Entry
    {
        public readonly string Weapon, Pickup, Model, Profile, Effect;
        public readonly Color EffectColor;
        public Entry(string weapon, string pickup, string model, string profile,
                     string effect, Color effectColor)
        {
            Weapon = weapon; Pickup = pickup; Model = model; Profile = profile;
            Effect = effect; EffectColor = effectColor;
        }
    }

    private static readonly Entry[] Weapons =
    {
        new Entry("Weapon_TPS_Shotgun", "Pickup_TPS_Shotgun",
            "TPS_Shotgun_Original", "03_Shotgun", "TPS_Shotgun_Sparks", new Color(1f, .65f, .15f)),
        new Entry("Weapon_TPS_Grenade", "Pickup_TPS_Grenade",
            "TPS_GrenadeLauncher_Original", "04_GrenadeLauncher", "TPS_Grenade_Explosion", new Color(1f, .28f, .06f)),
        new Entry("Weapon_TPS_Railgun", "Pickup_TPS_Railgun",
            "TPS_Railgun_Original", "05_Railgun", "TPS_Railgun_Sparks", new Color(.12f, .9f, 1f))
    };

    [MenuItem("Tools/TPS Shooter/Apply 3 Weapon Models + Pool (81-90)")]
    public static void Install()
    {
        foreach (Entry entry in Weapons)
        {
            if (!AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot + "/" + entry.Model + ".obj") ||
                !AssetDatabase.LoadAssetAtPath<GameObject>(WeaponFolder + "/" + entry.Weapon + ".prefab") ||
                !AssetDatabase.LoadAssetAtPath<GameObject>(PickupFolder + "/" + entry.Pickup + ".prefab") ||
                !AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(ProfileFolder + "/" + entry.Profile + ".asset"))
            {
                EditorUtility.DisplayDialog("TPS Shooter - missing assets",
                    "Missing model, weapon, pickup or profile for: " + entry.Weapon +
                    "\n\nFirst install weapons via Setup 6 Weapons (43-50), then copy all new files from this archive.", "OK");
                return;
            }
        }

        if (!AssetDatabase.IsValidFolder(MaterialFolder))
            AssetDatabase.CreateFolder(AssetsRoot, "Materials");
        CreateMaterials();

        int changed = 0;
        foreach (Entry entry in Weapons)
        {
            GameObject model = AssetDatabase.LoadAssetAtPath<GameObject>(AssetsRoot + "/" + entry.Model + ".obj");
            if (ReplaceVisual(WeaponFolder + "/" + entry.Weapon + ".prefab", model, entry.Model)) changed++;
            if (ReplaceVisual(PickupFolder + "/" + entry.Pickup + ".prefab", model, entry.Model)) changed++;

            GameObject vfx = CreateVfxPrefab(entry.Effect, entry.EffectColor);
            TPSWeaponProfile profile = AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(
                ProfileFolder + "/" + entry.Profile + ".asset");
            SerializedObject so = new SerializedObject(profile);
            SerializedProperty effectField = so.FindProperty("_impactEffectPrefab");
            if (effectField != null)
            {
                effectField.objectReferenceValue = vfx;
                so.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(profile);
            }
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog("TPS Shooter - installed",
            "Updated " + changed + " prefabs: three weapons and three pickups.\n" +
            "Added impact VFX to three weapon profiles. Runtime scripts now use an Object Pool and NonAlloc physics.\n\n" +
            "Check the new models in Prefab Mode, then run scene_sandbox and test keys 3/4/5.", "OK");
    }


    [MenuItem("Tools/TPS Shooter/Verify 3 Models + Pool (81-90)")]
    public static void Verify()
    {
        int ok = 0;
        foreach (Entry entry in Weapons)
        {
            string[] paths =
            {
                WeaponFolder + "/" + entry.Weapon + ".prefab",
                PickupFolder + "/" + entry.Pickup + ".prefab"
            };
            foreach (string path in paths)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                bool correct = root && root.transform.Find("TPS_VisualOverride") &&
                    root.transform.Find("TPS_VisualOverride")
                        .GetComponentInChildren<MeshRenderer>(true);
                if (correct) ok++;
                else Debug.LogWarning("TPS missing model: " + path);
            }
            TPSWeaponProfile profile = AssetDatabase.LoadAssetAtPath<TPSWeaponProfile>(
                ProfileFolder + "/" + entry.Profile + ".asset");
            if (profile && profile.ImpactEffectPrefab) ok++;
            else Debug.LogWarning("TPS missing pooled impact VFX: " + entry.Profile);
        }
        EditorUtility.DisplayDialog("TPS Shooter - model verification",
            "Checked " + ok + "/9: 6 prefabs with visual models + 3 profiles with VFX.\n" +
            (ok == 9 ? "All model/pool asset references are present. Test in Play Mode." :
                       "Some assets are missing; inspect warnings in Console."), "OK");
    }

    private static bool ReplaceVisual(string prefabPath, GameObject modelAsset, string modelName)
    {
        GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
        try
        {
            Transform previous = root.transform.Find("TPS_VisualOverride");
            if (previous) UnityEngine.Object.DestroyImmediate(previous.gameObject);

            // Disable only 3D geometry. Do not touch ParticleSystemRenderers, scripts,
            // colliders, audio, rig bones, muzzle or raycast origins.
            foreach (Renderer renderer in root.GetComponentsInChildren<Renderer>(true))
            {
                if (renderer is MeshRenderer || renderer is SkinnedMeshRenderer)
                    renderer.enabled = false;
            }

            GameObject newModel = PrefabUtility.InstantiatePrefab(modelAsset, root.scene) as GameObject;
            if (!newModel)
            {
                Debug.LogError("Cannot instantiate model asset for " + prefabPath);
                return false;
            }
            newModel.name = "TPS_VisualOverride";
            newModel.transform.SetParent(root.transform, false);
            newModel.transform.localPosition = Vector3.zero;
            newModel.transform.localRotation = Quaternion.identity;
            newModel.transform.localScale = Vector3.one;
            ApplyMaterials(newModel);

            PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            Debug.Log("TPS model installed: " + prefabPath + " <- " + modelName);
            return true;
        }
        finally
        {
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static readonly string[] MaterialNames =
    { "BLACK", "DARK", "STEEL", "SILVER", "RUBBER", "OLIVE", "GREEN",
      "ORANGE", "BRASS", "CYAN", "TEAL", "RED", "WHITE" };

    private static readonly Color[] MaterialColors =
    {
        new Color(.06f,.08f,.105f), new Color(.14f,.18f,.22f),
        new Color(.36f,.43f,.50f), new Color(.66f,.71f,.76f),
        new Color(.035f,.04f,.05f), new Color(.20f,.27f,.18f),
        new Color(.32f,.39f,.21f), new Color(.95f,.35f,.08f),
        new Color(.66f,.43f,.16f), new Color(.03f,.8f,.92f),
        new Color(.06f,.33f,.37f), new Color(.92f,.13f,.10f),
        new Color(.85f,.86f,.75f)
    };

    private static void CreateMaterials()
    {
        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (!shader) shader = Shader.Find("Universal Render Pipeline/Simple Lit");
        if (!shader) shader = Shader.Find("Standard");

        for (int i = 0; i < MaterialNames.Length; i++)
        {
            string path = MaterialFolder + "/MAT_" + MaterialNames[i] + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (!material)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, path);
            }
            if (material.HasProperty("_BaseColor"))
                material.SetColor("_BaseColor", MaterialColors[i]);
            else if (material.HasProperty("_Color"))
                material.SetColor("_Color", MaterialColors[i]);
            if (MaterialNames[i] == "CYAN")
            {
                if (material.HasProperty("_EmissionColor"))
                {
                    material.EnableKeyword("_EMISSION");
                    material.SetColor("_EmissionColor", MaterialColors[i] * 2f);
                }
            }
            EditorUtility.SetDirty(material);
        }
    }

    private static void ApplyMaterials(GameObject model)
    {
        foreach (MeshRenderer renderer in model.GetComponentsInChildren<MeshRenderer>(true))
        {
            string name = renderer.gameObject.name.ToUpperInvariant();
            int index = Array.FindIndex(MaterialNames,
                label => name.Contains("MAT_" + label));
            if (index < 0 && renderer.sharedMaterial != null)
            {
                string oldName = renderer.sharedMaterial.name.ToUpperInvariant();
                index = Array.FindIndex(MaterialNames,
                    label => oldName.Contains("MAT_" + label));
            }
            if (index < 0) index = 2;
            Material material = AssetDatabase.LoadAssetAtPath<Material>(
                MaterialFolder + "/MAT_" + MaterialNames[index] + ".mat");
            Material[] assigned = renderer.sharedMaterials;
            if (assigned == null || assigned.Length == 0) assigned = new Material[1];
            for (int i = 0; i < assigned.Length; i++) assigned[i] = material;
            renderer.sharedMaterials = assigned;
            renderer.enabled = true;
        }
    }

    private static GameObject CreateVfxPrefab(string name, Color color)
    {
        string path = AssetsRoot + "/" + name + ".prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing) return existing;

        GameObject root = new GameObject(name);
        try
        {
            ParticleSystem particles = root.AddComponent<ParticleSystem>();
            var main = particles.main;
            main.duration = .4f;
            main.loop = false;
            main.playOnAwake = false;
            main.startLifetime = new ParticleSystem.MinMaxCurve(.15f,.34f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.4f,4.2f);
            main.startSize = new ParticleSystem.MinMaxCurve(.04f,.12f);
            main.startColor = color;
            var emission = particles.emission;
            emission.rateOverTime = 0f;
            emission.SetBursts(new [] { new ParticleSystem.Burst(0f, (short)18) });
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .04f;
            var particleRenderer = root.GetComponent<ParticleSystemRenderer>();
            Shader shader = Shader.Find("Universal Render Pipeline/Particles/Unlit");
            if (!shader) shader = Shader.Find("Particles/Standard Unlit");
            if (!shader) shader = Shader.Find("Sprites/Default");
            string materialPath = AssetsRoot + "/" + name + "_Particles.mat";
            Material particleMaterial = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (!particleMaterial)
            {
                particleMaterial = new Material(shader);
                AssetDatabase.CreateAsset(particleMaterial, materialPath);
            }
            particleMaterial.name = name + "_ParticleMaterial";
            if (particleMaterial.HasProperty("_BaseColor"))
                particleMaterial.SetColor("_BaseColor", color);
            if (particleMaterial.HasProperty("_Color"))
                particleMaterial.SetColor("_Color", color);
            EditorUtility.SetDirty(particleMaterial);
            particleRenderer.sharedMaterial = particleMaterial;
            return PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { UnityEngine.Object.DestroyImmediate(root); }
    }
}
