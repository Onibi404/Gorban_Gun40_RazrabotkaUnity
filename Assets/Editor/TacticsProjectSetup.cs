#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

public static class TacticsProjectSetup
{
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs";
    private const string ScenesPath = "Assets/Scenes";
    private const string ScenePath = ScenesPath + "/TacticsScene.unity";

    [MenuItem("Шашки/Создать готовую сцену")]
    public static void SetupProject()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Шашки",
                "Сначала остановите режим Play, затем снова запустите создание сцены.",
                "ОК");
            return;
        }

        EnsureFolder(MaterialsPath);
        EnsureFolder(PrefabsPath);
        EnsureFolder(ScenesPath);

        Material light = CreateMaterial("CellLight", new Color(0.82f, 0.72f, 0.56f));
        Material dark = CreateMaterial("CellDark", new Color(0.28f, 0.16f, 0.10f));
        Material hover = CreateMaterial("CellHover", new Color(0.95f, 0.82f, 0.18f));
        Material move = CreateMaterial("CellMove", new Color(0.25f, 0.70f, 0.32f));
        Material attack = CreateMaterial("CellAttack", new Color(0.85f, 0.22f, 0.18f));
        Material selected = CreateMaterial("CellSelected", new Color(0.18f, 0.52f, 0.90f));
        Material pending = CreateMaterial("CellPending", new Color(0.65f, 0.30f, 0.85f));

        Material whiteUnit = CreateMaterial("WhiteUnit", new Color(0.93f, 0.93f, 0.88f));
        Material blackUnit = CreateMaterial("BlackUnit", new Color(0.08f, 0.08f, 0.08f));
        Material whiteKing = CreateMaterial("WhiteKing", new Color(0.98f, 0.78f, 0.18f));
        Material blackKing = CreateMaterial("BlackKing", new Color(0.58f, 0.17f, 0.72f));

        Cell cellPrefab = CreateCellPrefab();
        Unit unitPrefab = CreateUnitPrefab();

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        CreateCamera();
        CreateLight();
        CreateEventSystem();

        GameObject battlefieldObject = new GameObject("Battlefield");
        Battlefield battlefield = battlefieldObject.AddComponent<Battlefield>();
        battlefieldObject.transform.position = Vector3.zero;

        GameObject controllersObject = new GameObject("Controllers");
        PlayerController playerController = controllersObject.AddComponent<PlayerController>();
        BattleController battleController = controllersObject.AddComponent<BattleController>();

        AssignBattlefieldReferences(
            battlefield,
            cellPrefab,
            unitPrefab,
            light,
            dark,
            hover,
            move,
            attack,
            selected,
            pending,
            whiteUnit,
            blackUnit,
            whiteKing,
            blackKing);

        SerializedObject battleSerialized = new SerializedObject(battleController);
        battleSerialized.FindProperty("battlefield").objectReferenceValue = battlefield;
        battleSerialized.FindProperty("playerController").objectReferenceValue = playerController;
        battleSerialized.ApplyModifiedPropertiesWithoutUndo();

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, ScenePath);

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Selection.activeObject = battlefieldObject;

        EditorUtility.DisplayDialog(
            "Настройка шашек завершена",
            "Сцена создана: Assets/Scenes/TacticsScene.unity\n\n" +
            "Теперь нажмите Play.\n\n" +
            "ЛКМ — выбрать шашку/клетку\n" +
            "Пробел — подтвердить ход\n" +
            "Esc — отменить",
            "ОК");
    }

    [MenuItem("Шашки/Проверить сцену")]
    public static void ValidateScene()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.LogWarning("Проверка сцены: сначала остановите режим Play.");
            return;
        }

        Battlefield battlefield = Object.FindObjectOfType<Battlefield>();
        BattleController battleController = Object.FindObjectOfType<BattleController>();
        PlayerController playerController = Object.FindObjectOfType<PlayerController>();
        Camera mainCamera = Camera.main;

        bool ok = true;

        if (battlefield == null)
        {
            Debug.LogError("ПРОВЕРКА: объект Battlefield отсутствует.");
            ok = false;
        }

        if (battleController == null)
        {
            Debug.LogError("ПРОВЕРКА: объект BattleController отсутствует.");
            ok = false;
        }

        if (playerController == null)
        {
            Debug.LogError("ПРОВЕРКА: объект PlayerController отсутствует.");
            ok = false;
        }

        if (mainCamera == null)
        {
            Debug.LogError("ПРОВЕРКА: главная камера с тегом MainCamera отсутствует.");
            ok = false;
        }

        if (ok)
            Debug.Log("ПРОВЕРКА УСПЕШНА: в сцене есть Battlefield, BattleController, PlayerController и Main Camera.");
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Main Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        cameraObject.tag = "MainCamera";
        cameraObject.transform.position = new Vector3(3.5f, 10f, 3.5f);
        cameraObject.transform.rotation = Quaternion.Euler(90f, 0f, 0f);
        camera.orthographic = true;
        camera.orthographicSize = 4.8f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.12f, 0.12f, 0.14f);
    }

    private static void CreateLight()
    {
        GameObject lightObject = new GameObject("Directional Light");
        Light directionalLight = lightObject.AddComponent<Light>();
        directionalLight.type = LightType.Directional;
        directionalLight.intensity = 1.1f;
        lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
    }

    private static void CreateEventSystem()
    {
        GameObject eventSystemObject = new GameObject("EventSystem");
        eventSystemObject.AddComponent<EventSystem>();
        eventSystemObject.AddComponent<StandaloneInputModule>();
    }

    private static void AssignBattlefieldReferences(
        Battlefield battlefield,
        Cell cellPrefab,
        Unit unitPrefab,
        Material light,
        Material dark,
        Material hover,
        Material move,
        Material attack,
        Material selected,
        Material pending,
        Material whiteUnit,
        Material blackUnit,
        Material whiteKing,
        Material blackKing)
    {
        SerializedObject serialized = new SerializedObject(battlefield);
        serialized.FindProperty("cellPrefab").objectReferenceValue = cellPrefab;
        serialized.FindProperty("unitPrefab").objectReferenceValue = unitPrefab;
        serialized.FindProperty("lightMaterial").objectReferenceValue = light;
        serialized.FindProperty("darkMaterial").objectReferenceValue = dark;
        serialized.FindProperty("hoverMaterial").objectReferenceValue = hover;
        serialized.FindProperty("moveMaterial").objectReferenceValue = move;
        serialized.FindProperty("attackMaterial").objectReferenceValue = attack;
        serialized.FindProperty("selectedMaterial").objectReferenceValue = selected;
        serialized.FindProperty("pendingMaterial").objectReferenceValue = pending;
        serialized.FindProperty("whiteUnitMaterial").objectReferenceValue = whiteUnit;
        serialized.FindProperty("blackUnitMaterial").objectReferenceValue = blackUnit;
        serialized.FindProperty("whiteKingMaterial").objectReferenceValue = whiteKing;
        serialized.FindProperty("blackKingMaterial").objectReferenceValue = blackKing;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Cell CreateCellPrefab()
    {
        string path = PrefabsPath + "/Cell.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing.GetComponent<Cell>();

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cube);
        temp.name = "Cell";
        temp.transform.localScale = new Vector3(1f, 0.1f, 1f);

        Cell cell = temp.GetComponent<Cell>();
        if (cell == null)
            cell = temp.AddComponent<Cell>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab.GetComponent<Cell>();
    }

    private static Unit CreateUnitPrefab()
    {
        string path = PrefabsPath + "/Unit.prefab";
        GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null)
            return existing.GetComponent<Unit>();

        GameObject temp = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        temp.name = "Unit";
        temp.transform.localScale = new Vector3(0.72f, 0.18f, 0.72f);

        Unit unit = temp.GetComponent<Unit>();
        if (unit == null)
            unit = temp.AddComponent<Unit>();

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(temp, path);
        Object.DestroyImmediate(temp);
        return prefab.GetComponent<Unit>();
    }

    private static Material CreateMaterial(string name, Color color)
    {
        string path = MaterialsPath + "/" + name + ".mat";
        Material existing = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (existing != null)
        {
            existing.color = color;
            EditorUtility.SetDirty(existing);
            return existing;
        }

        Shader shader = Shader.Find("Standard");
        if (shader == null)
            shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
            shader = Shader.Find("Unlit/Color");

        if (shader == null)
            throw new System.InvalidOperationException("Не найден совместимый шейдер.");

        Material material = new Material(shader)
        {
            name = name,
            color = color
        };

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path))
            return;

        string[] parts = path.Split('/');
        string current = parts[0];

        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif
