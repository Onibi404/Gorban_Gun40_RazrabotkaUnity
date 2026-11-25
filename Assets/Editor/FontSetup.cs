#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;


public static class FontSetup
{
    private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
    private const string GeneratedFolder = "Assets/TextMesh Pro/Resources/Fonts & Materials";
    private const string GeneratedAssetPath = GeneratedFolder + "/RussianText.asset";
    private const string SettingsPath = "Assets/TextMesh Pro/Resources/TMP Settings.asset";

    [MenuItem("Tools/Text/Setup Russian Font")]
    public static void FixRussianFonts()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Tower Defense", "Сначала выйдите из Play Mode.", "OK");
            return;
        }

        try
        {
            TMP_FontAsset fontAsset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(GeneratedAssetPath);
            if (fontAsset == null)
            {
                Font source = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
                if (source == null)
                {
                    EditorUtility.DisplayDialog("Tower Defense", "Не найден исходный шрифт: " + SourceFontPath, "OK");
                    return;
                }

                fontAsset = TMP_FontAsset.CreateFontAsset(
                    source, 90, 9, GlyphRenderMode.SDFAA,
                    1024, 1024, AtlasPopulationMode.Dynamic, true);

                if (fontAsset == null)
                    throw new InvalidOperationException("Не удалось создать динамический TMP Font Asset.");

                fontAsset.name = "RussianText";
                fontAsset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                fontAsset.isMultiAtlasTexturesEnabled = true;

                
                string characters = BuildCharacters();
                fontAsset.TryAddCharacters(characters, out string missing);
                if (!string.IsNullOrEmpty(missing))
                    Debug.LogWarning("[Fonts] В исходном шрифте отсутствуют некоторые дополнительные знаки: " + missing);

                Texture2D atlas = fontAsset.atlasTexture;
                Material material = fontAsset.material;
                AssetDatabase.CreateAsset(fontAsset, GeneratedAssetPath);

                if (atlas != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(atlas)))
                {
                    atlas.name = "RussianTextAtlas";
                    AssetDatabase.AddObjectToAsset(atlas, fontAsset);
                }
                if (material != null && string.IsNullOrEmpty(AssetDatabase.GetAssetPath(material)))
                {
                    material.name = "RussianTextMaterial";
                    AssetDatabase.AddObjectToAsset(material, fontAsset);
                }
                EditorUtility.SetDirty(fontAsset);
            }

            
            TMP_Settings settings = AssetDatabase.LoadAssetAtPath<TMP_Settings>(SettingsPath);
            if (settings == null)
            {
                EditorUtility.DisplayDialog("Tower Defense", "Не найден TMP Settings: " + SettingsPath, "OK");
                return;
            }
            SerializedObject serializedSettings = new SerializedObject(settings);
            SerializedProperty fallbacks = serializedSettings.FindProperty("m_fallbackFontAssets");
            if (fallbacks == null)
                throw new InvalidOperationException("Не удалось найти список Fallback Font Assets в TMP Settings.");

            bool alreadyListed = false;
            for (int i = 0; i < fallbacks.arraySize; i++)
            {
                if (fallbacks.GetArrayElementAtIndex(i).objectReferenceValue == fontAsset)
                {
                    alreadyListed = true;
                    break;
                }
            }
            if (!alreadyListed)
            {
                int index = fallbacks.arraySize;
                fallbacks.InsertArrayElementAtIndex(index);
                fallbacks.GetArrayElementAtIndex(index).objectReferenceValue = fontAsset;
            }
            serializedSettings.ApplyModifiedProperties();
            EditorUtility.SetDirty(settings);

            int changedCount = 0;
            for (int s = 0; s < SceneManager.sceneCount; s++)
            {
                Scene scene = SceneManager.GetSceneAt(s);
                if (!scene.IsValid() || !scene.isLoaded)
                    continue;

                bool changedScene = false;
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (TMP_Text label in root.GetComponentsInChildren<TMP_Text>(true))
                    {
                        if (label == null || !HasCyrillic(label.text) || label.font == fontAsset)
                            continue;

                        Undo.RecordObject(label, "Use Cyrillic TMP font");
                        label.font = fontAsset;
                        EditorUtility.SetDirty(label);
                        changedCount++;
                        changedScene = true;
                    }
                }
                if (changedScene)
                    EditorSceneManager.MarkSceneDirty(scene);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("[Fonts] Кириллица подключена. Надписей обновлено: " + changedCount +
                ". Резервный шрифт добавлен в TMP Settings. Сохраните сцену Cmd+S.");
            EditorUtility.DisplayDialog("Tower Defense",
                "Готово! Кириллический TMP-шрифт создан из LiberationSans.ttf.\n" +
                "Подключён глобальный fallback; надписей в открытых сценах обновлено: " + changedCount +
                ".\nНажмите Cmd+S, запустите игру и проверьте окно победы/поражения.", "OK");
        }
        catch (Exception error)
        {
            Debug.LogException(error);
            EditorUtility.DisplayDialog("Tower Defense", "Не удалось исправить шрифт. Скопируйте первую красную ошибку из Console: " + error.Message, "OK");
        }
    }

    [MenuItem("Tools/Text/Setup Russian Font", true)]
    private static bool Validate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static bool HasCyrillic(string value)
    {
        if (string.IsNullOrEmpty(value)) return false;
        foreach (char ch in value)
            if (ch >= '\u0400' && ch <= '\u052F') return true;
        return false;
    }

    private static string BuildCharacters()
    {
        var builder = new StringBuilder(256);
        for (char ch = ' '; ch <= '~'; ch++) builder.Append(ch);
        for (char ch = '\u0410'; ch <= '\u044F'; ch++) builder.Append(ch);
        builder.Append("Ёё№«»—–…₽");
        return builder.ToString();
    }
}
#endif
