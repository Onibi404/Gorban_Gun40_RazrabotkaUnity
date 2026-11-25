#if UNITY_EDITOR
using System;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;


public static class SceneTextEditor
{
    private const string Goal =
        "<b>ЗАЩИТИТЕ КОРОЛЕВСТВО!</b>\n" +
        "Не дайте врагам добраться до замка.\n" +
        "Стройте башни и бараки, чтобы остановить наступление.";

    [MenuItem("Tools/Scene/Update Intro")]
    private static void Apply()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("Tower Defense", "Выйдите из Play Mode перед изменением сцены.", "OK");
            return;
        }

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "LevelScene")
        {
            EditorUtility.DisplayDialog("Tower Defense", "Откройте сцену LevelScene и повторите команду.", "OK");
            return;
        }

        var texts = FindTexts(scene);
        TextMeshProUGUI goalText = null;
        foreach (var text in texts)
        {
            if (text == null) continue;
            if (text.text.Contains("Мой король") ||
                (text.name == "Text (TMP)" && text.transform.parent != null &&
                 text.transform.parent.parent != null &&
                 text.transform.parent.parent.GetComponent<Canvas>() != null))
            {
                goalText = text;
                break;
            }
        }

        if (goalText == null)
        {
            EditorUtility.DisplayDialog("Tower Defense",
                "Стартовое окно не найдено. Сцена не изменена. Пришлите скрин Hierarchy объекта с текстом.", "OK");
            return;
        }

        Undo.SetCurrentGroupName("Update intro");
        int undoGroup = Undo.GetCurrentGroup();

        Undo.RecordObject(goalText, "Update tutorial text");
        goalText.text = Goal;
        goalText.alignment = TextAlignmentOptions.Center;
        goalText.fontSize = 27f;
        goalText.enableAutoSizing = true;
        goalText.fontSizeMin = 20f;
        goalText.fontSizeMax = 27f;
        goalText.color = new Color(1f, 0.97f, 0.85f, 1f);
        EditorUtility.SetDirty(goalText);

        var textRect = goalText.rectTransform;
        Undo.RecordObject(textRect, "Center goal text");
        textRect.anchoredPosition = Vector2.zero;
        EditorUtility.SetDirty(textRect);

        var panel = goalText.transform.parent;
        var panelImage = panel != null ? panel.GetComponent<Image>() : null;
        if (panelImage != null)
        {
            Undo.RecordObject(panelImage, "Polish goal panel");
            panelImage.color = new Color(0.11f, 0.16f, 0.24f, 0.94f);
            EditorUtility.SetDirty(panelImage);
        }

        int knightImagesDisabled = 0;
        if (panel != null)
        {
            foreach (Transform child in panel)
            {
                if (child == goalText.transform)
                    continue;

                var image = child.GetComponent<Image>();
                
                if (image != null && image.sprite != null && child.gameObject.activeSelf)
                {
                    Undo.RecordObject(child.gameObject, "Hide intro knight");
                    child.gameObject.SetActive(false);
                    knightImagesDisabled++;
                    EditorUtility.SetDirty(child.gameObject);
                }
            }
        }

        int labelsChanged = 0;
        foreach (var label in texts)
        {
            if (label == null || label == goalText) continue;
            string replacement = null;
            switch (label.text.Trim())
            {
                case "Game Win!": replacement = "ПОБЕДА!"; break;
                case "Game Lose!": replacement = "ПОРАЖЕНИЕ"; break;
                case "Menu Pause": replacement = "ПАУЗА"; break;
            }
            if (replacement == null) continue;
            Undo.RecordObject(label, "Localize TD UI");
            label.text = replacement;
            EditorUtility.SetDirty(label);
            labelsChanged++;
        }

        Undo.CollapseUndoOperations(undoGroup);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Scene] Цель обновлена; изображений рыцаря скрыто: " +
                  knightImagesDisabled + "; интерфейсных надписей обновлено: " + labelsChanged +
                  ". Сохраните сцену Cmd+S.");
        EditorUtility.DisplayDialog("Tower Defense",
            "Цель игры обновлена, рыцарь скрыт, UI переведён.\n" +
            "Скрытых изображений: " + knightImagesDisabled +
            ".\nНажмите Cmd+S и проверьте сцену в Play Mode.", "Готово");
    }

    [MenuItem("Tools/Scene/Update Intro", true)]
    private static bool Validate() => !EditorApplication.isPlayingOrWillChangePlaymode;

    [MenuItem("Tools/Scene/Update Menu Title")]
    private static void PolishMainMenu()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
            return;

        var scene = SceneManager.GetActiveScene();
        if (!scene.IsValid() || scene.name != "MainMenuScene")
        {
            EditorUtility.DisplayDialog("Tower Defense", "Откройте MainMenuScene и повторите команду.", "OK");
            return;
        }

        foreach (var label in FindTexts(scene))
        {
            if (label == null || !label.text.Contains("Tower Defense Prototype"))
                continue;

            Undo.RecordObject(label, "Set final game title");
            label.text = "ЗАЩИТА КОРОЛЕВСТВА";
            label.color = new Color(1f, 0.88f, 0.42f, 1f);
            label.enableAutoSizing = true;
            label.fontSizeMin = 26f;
            label.fontSizeMax = 58f;
            EditorUtility.SetDirty(label);
            EditorSceneManager.MarkSceneDirty(scene);
            Debug.Log("[Scene] Заголовок главного меню обновлён. Сохраните сцену Cmd+S.");
            EditorUtility.DisplayDialog("Tower Defense", "Название игры обновлено. Нажмите Cmd+S.", "Готово");
            return;
        }
        EditorUtility.DisplayDialog("Tower Defense", "Заголовок Tower Defense Prototype не найден. Сцена не изменена.", "OK");
    }

    [MenuItem("Tools/Scene/Update Menu Title", true)]
    private static bool ValidateMainMenu() => !EditorApplication.isPlayingOrWillChangePlaymode;

    private static TextMeshProUGUI[] FindTexts(Scene scene)
    {
        var all = new System.Collections.Generic.List<TextMeshProUGUI>();
        foreach (var root in scene.GetRootGameObjects())
            all.AddRange(root.GetComponentsInChildren<TextMeshProUGUI>(true));
        return all.ToArray();
    }
}
#endif
