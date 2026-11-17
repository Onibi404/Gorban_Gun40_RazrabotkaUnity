#if UNITY_EDITOR
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Adds the full-screen Tab map, reusing the existing Minimap camera/RenderTexture.
/// Does not move mission or minimap objects.
/// </summary>
public static class TPSInstallFullscreenMap
{
    [MenuItem("Tools/TPS Shooter/Add Fullscreen Map on Tab (75-78)")]
    public static void Install()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog("TPS Shooter", "Останови Play Mode перед установкой карты.", "OK");
            return;
        }

        GameObject alreadyInstalled = GameObject.Find("TPS_FullscreenMapCanvas");
        if (alreadyInstalled != null)
        {
            Selection.activeGameObject = alreadyInstalled;
            EditorUtility.DisplayDialog("TPS Shooter", "Полноэкранная карта уже установлена. Повторное создание не требуется.", "OK");
            return;
        }

        Minimap minimap = Object.FindObjectOfType<Minimap>(true);
        Camera mapCamera = minimap != null ? minimap.miniCam : null;
        if (mapCamera == null || mapCamera.targetTexture == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Не найдена миникарта с Camera.targetTexture.\n\n" +
                "Проверь объект Mini_Map и поле Mini Cam у компонента Minimap.\n" +
                "Если у камеры не назначен RenderTexture — карту пока не устанавливай.", "OK");
            return;
        }

        GameObject canvasGO = new GameObject("TPS_FullscreenMapCanvas",
            typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
        Undo.RegisterCreatedObjectUndo(canvasGO, "Install full screen map");
        Canvas canvas = canvasGO.GetComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 25;

        CanvasScaler scaler = canvasGO.GetComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920, 1080);
        scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
        scaler.matchWidthOrHeight = 0.5f;

        GameObject panel = new GameObject("FullscreenMap_Panel", typeof(RectTransform), typeof(Image));
        panel.transform.SetParent(canvasGO.transform, false);
        RectTransform panelRect = panel.GetComponent<RectTransform>();
        panelRect.anchorMin = Vector2.zero;
        panelRect.anchorMax = Vector2.one;
        panelRect.offsetMin = panelRect.offsetMax = Vector2.zero;
        Image background = panel.GetComponent<Image>();
        background.color = new Color(0.025f, 0.04f, 0.065f, 0.95f);
        background.raycastTarget = false;

        GameObject view = new GameObject("Map_RenderTexture", typeof(RectTransform), typeof(RawImage));
        view.transform.SetParent(panel.transform, false);
        RectTransform viewRect = view.GetComponent<RectTransform>();
        viewRect.anchorMin = viewRect.anchorMax = new Vector2(0.5f, 0.5f);
        viewRect.pivot = new Vector2(0.5f, 0.5f);
        viewRect.sizeDelta = new Vector2(840f, 840f);
        viewRect.anchoredPosition = new Vector2(0f, 0f);
        RawImage mapImage = view.GetComponent<RawImage>();
        mapImage.texture = mapCamera.targetTexture;
        mapImage.raycastTarget = false;

        MakeLabel(panel.transform, "Title", "КАРТА УРОВНЯ", new Vector2(0, 475), 34f);
        MakeLabel(panel.transform, "CloseHint", "TAB — закрыть карту", new Vector2(0, -475), 23f);

        TPSFullscreenMap controller = canvasGO.AddComponent<TPSFullscreenMap>();
        controller.Configure(panel, mapCamera);
        panel.SetActive(false);

        EditorUtility.SetDirty(canvasGO);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = canvasGO;
        EditorGUIUtility.PingObject(canvasGO);
        EditorUtility.DisplayDialog("TPS Shooter",
            "Карта установлена.\nTAB — открыть и закрыть.\nВо время паузы карта скрывается.\n\n" +
            "Если на заголовке квадраты, запусти Fix Russian Mission Text (74).\n" +
            "Нажми Cmd+S и Play.", "OK");
    }

    private static void MakeLabel(Transform parent, string name, string text,
        Vector2 position, float size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rect = go.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(900, 60);
        rect.anchoredPosition = position;
        TextMeshProUGUI label = go.GetComponent<TextMeshProUGUI>();
        label.text = text;
        label.fontSize = size;
        label.alignment = TextAlignmentOptions.Center;
        label.raycastTarget = false;
        label.color = Color.white;

        TMP_FontAsset russianFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/_Project/Fonts/TPS_Cyrillic_Dynamic.asset");
        if (russianFont != null)
            label.font = russianFont;
    }
}
#endif
