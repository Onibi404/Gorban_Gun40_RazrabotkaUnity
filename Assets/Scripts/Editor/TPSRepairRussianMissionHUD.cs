#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;

/// <summary>
/// Repair the TextMeshPro font on the existing mission guide without rebuilding the HUD.
/// Uses the existing LiberationSans.ttf already included with TextMeshPro Essentials.
/// </summary>
public static class TPSRepairRussianMissionHUD
{
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    private const string GeneratedFontPath = "Assets/_Project/Fonts/TPS_Cyrillic_Dynamic.asset";
    private const string HudObjectName = "TPS_MissionGuideCanvas";

    [MenuItem("Tools/TPS Shooter/Fix Russian Mission Text (74)")]
    public static void Repair()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("TPS Shooter", "Останови Play Mode перед исправлением шрифта.", "OK");
            return;
        }

        GameObject hud = GameObject.Find(HudObjectName);
        if (hud == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Не найден TPS_MissionGuideCanvas. Открой scene_sandbox, где ты добавила инструкцию 71–73.", "OK");
            return;
        }

        Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
        if (sourceFont == null)
        {
            string[] ids = AssetDatabase.FindAssets("LiberationSans t:Font");
            if (ids.Length > 0)
                sourceFont = AssetDatabase.LoadAssetAtPath<Font>(AssetDatabase.GUIDToAssetPath(ids[0]));
        }
        if (sourceFont == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Не найден исходный LiberationSans.ttf. Проверь Assets/TextMesh Pro/Fonts/ и импорт TMP Essentials.", "OK");
            return;
        }

        TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedFontPath);
        if (fontAsset == null)
        {
            EnsureAssetFolder("Assets/_Project/Fonts");
            fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont, 56, 5,
                GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic, true);
            if (fontAsset == null)
            {
                EditorUtility.DisplayDialog("TPS Shooter", "Не удалось создать TMP Font Asset из LiberationSans.ttf.", "OK");
                return;
            }
            fontAsset.name = "TPS_Cyrillic_Dynamic";
            AssetDatabase.CreateAsset(fontAsset, GeneratedFontPath);

            // Preserve its material and initial atlas as sub-assets for a clean Git checkout.
            Material material = fontAsset.material;
            if (material != null && !AssetDatabase.Contains(material))
                AssetDatabase.AddObjectToAsset(material, fontAsset);

            Texture2D[] atlases = fontAsset.atlasTextures;
            if (atlases != null)
            {
                foreach (Texture2D atlas in atlases)
                {
                    if (atlas != null && !AssetDatabase.Contains(atlas))
                        AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
            }
        }

        fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;

        const string russian =
            "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯ" +
            "абвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
            "0123456789 .,:;!?()-/<>—";
        string missing;
        if (!fontAsset.TryAddCharacters(russian, out missing))
            Debug.LogWarning("TPS: не удалось добавить некоторые символы в TMP-шрифт: " + missing);

        TMP_Text[] labels = hud.GetComponentsInChildren<TMP_Text>(true);
        int updated = 0;
        foreach (TMP_Text label in labels)
        {
            if (label == null) continue;
            Undo.RecordObject(label, "Fix Russian TMP mission text");
            label.font = fontAsset;
            label.fontSharedMaterial = fontAsset.material;
            label.SetAllDirty();
            EditorUtility.SetDirty(label);
            updated++;
        }

        EditorUtility.SetDirty(fontAsset);
        AssetDatabase.SaveAssets();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = hud;
        EditorGUIUtility.PingObject(hud);
        EditorUtility.DisplayDialog("TPS Shooter",
            "Исправлено TMP-полей: " + updated + ".\n" +
            "Шрифт создан на основе уже имеющегося LiberationSans.ttf.\n\n" +
            "Нажми Cmd+S и Play. Пересоздавать HUD и миссию НЕ нужно.", "OK");
    }

    private static void EnsureAssetFolder(string path)
    {
        string[] parts = path.Split('/');
        string parent = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = parent + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(parent, parts[i]);
            parent = next;
        }
    }
}
#endif
