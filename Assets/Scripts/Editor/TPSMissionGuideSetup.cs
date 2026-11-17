#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Idempotent one-click HUD setup for the existing 3-zone mission.
/// Does not recreate or move gameplay objects; uses the existing Cyrillic TMP font.
/// </summary>
public static class TPSMissionGuideSetup
{
    private const string CanvasName = "TPS_MissionGuideCanvas";

    [MenuItem("Tools/TPS Shooter/Add Mission Guide HUD (71-73)")]
    public static void Install()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "scene_sandbox")
        {
            EditorUtility.DisplayDialog("TPS Shooter", "Сначала открой сцену scene_sandbox.", "OK");
            return;
        }

        var existing = GameObject.Find(CanvasName);
        if (existing != null)
        {
            Selection.activeGameObject = existing;
            EditorGUIUtility.PingObject(existing);
            EditorUtility.DisplayDialog("TPS Shooter", "Инструкция уже существует. Повторно она не создана.", "OK");
            return;
        }

        GameObject missionRoot = GameObject.Find("TPS_ThreeZoneMission");
        TPSMissionDirector director = missionRoot != null
            ? missionRoot.GetComponent<TPSMissionDirector>() : null;
        if (director == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Не найден TPSMissionDirector на TPS_ThreeZoneMission. Открой рабочую сцену с миссией 51–60.", "OK");
            return;
        }

        TMP_FontAsset font = GetExistingHudFont();
        if (font == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Не найден TMP шрифт существующего игрового HUD. Проверь компонент Objective Text у LevelFlowController и повтори установку.", "OK");
            return;
        }

        GameObject canvasObject = new GameObject(CanvasName,
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
        Undo.RegisterCreatedObjectUndo(canvasObject, "Add TPS mission guide HUD");
        Canvas canvas = canvasObject.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 10;
        CanvasScaler scaler = canvasObject.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        var panel = new GameObject("MissionGuide_Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.sizeDelta = new Vector2(485f, 398f);
        panelRect.anchoredPosition = new Vector2(24f, -115f);
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.055f, 0.09f, 0.15f, 0.91f);
        background.raycastTarget = false;

        MakeBar(panel.transform, "TopAccent", new Vector2(0, 0),
            new Vector2(485, 4), new Color(0.26f, 0.82f, 0.87f, 1f));
        MakeBar(panel.transform, "Divider", new Vector2(20, -126),
            new Vector2(445, 1), new Color(0.40f, 0.53f, 0.66f, 0.6f));

        MakeText(panel.transform, "Title", font, new Vector2(20f, -16f),
            new Vector2(445f, 34f), 25f, "ПРОХОЖДЕНИЕ МИССИИ",
            new Color(0.85f, 0.95f, 1f, 1f));
        TextMeshProUGUI current = MakeText(panel.transform, "CurrentObjective", font,
            new Vector2(20f, -60f), new Vector2(445f, 60f), 20f,
            "СЕЙЧАС: Войти в зелёную зону", Color.white);
        current.enableWordWrapping = true;

        TextMeshProUGUI steps = MakeText(panel.transform, "AllObjectives_0_to_6", font,
            new Vector2(20f, -143f), new Vector2(445f, 204f), 18f,
            "0. Войти в зелёную зону", Color.white);
        steps.lineSpacing = 8;
        steps.enableWordWrapping = false;

        MakeText(panel.transform, "FooterHint", font, new Vector2(20f, -366f),
            new Vector2(445f, 22f), 15f,
            "Красная ловушка — необязательная", new Color(0.65f, 0.74f, 0.85f));

        TPSMissionGuideHUD guide = canvasObject.AddComponent<TPSMissionGuideHUD>();
        guide.Configure(director, current, steps);

        EditorUtility.SetDirty(canvasObject);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = canvasObject;
        EditorGUIUtility.PingObject(canvasObject);
        EditorUtility.DisplayDialog("TPS Shooter",
            "Инструкция добавлена на экран. Зоны, дверь и оружие не изменены.\n\n" +
            "Нажми Cmd+S и Play. При необходимости передвинь MissionGuide_Panel через его RectTransform.",
            "OK");
    }

    private static TMP_FontAsset GetExistingHudFont()
    {
        var flow = Object.FindObjectOfType<LevelFlowController>(true);
        if (flow != null)
        {
            var serialized = new SerializedObject(flow);
            var objective = serialized.FindProperty("_objectiveText");
            var label = objective != null ? objective.objectReferenceValue as TMP_Text : null;
            if (label != null && label.font != null)
                return label.font;
        }

        // Fallback to another in-scene TMP text, preserving Russian glyph support
        // whenever existing HUD texts use a Cyrillic-capable font asset.
        TMP_Text[] labels = Object.FindObjectsOfType<TMP_Text>(true);
        foreach (TMP_Text label in labels)
        {
            if (label != null && label.font != null && label.text != null &&
                (label.text.Contains("врагов") || label.text.Contains("Миссия") ||
                 label.text.Contains("Уничтожено")))
                return label.font;
        }
        return TMP_Settings.defaultFontAsset;
    }

    private static void MakeBar(Transform parent, string name, Vector2 pos,
        Vector2 size, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(Image));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;
        var graphic = go.GetComponent<Image>();
        graphic.color = color;
        graphic.raycastTarget = false;
    }

    private static TextMeshProUGUI MakeText(Transform parent, string name, TMP_FontAsset font,
        Vector2 pos, Vector2 size, float fontSize, string content, Color color)
    {
        var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI text = go.GetComponent<TextMeshProUGUI>();
        text.font = font;
        text.fontSize = fontSize;
        text.color = color;
        text.text = content;
        text.alignment = TextAlignmentOptions.TopLeft;
        text.enableAutoSizing = false;
        text.richText = true;
        text.raycastTarget = false;
        return text;
    }
}
#endif
