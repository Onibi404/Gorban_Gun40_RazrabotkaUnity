using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class CheckersSceneBaker
{
    private const string CellPrefabPath =
        "Assets/Prefabs/Cell.prefab";

    private const string UnitPrefabPath =
        "Assets/Prefabs/Unit.prefab";

    private const string MaterialsPath =
        "Assets/Materials/";

    [MenuItem("Шашки/Подготовить стартовое поле в текущей сцене")]
    public static void BakeInitialBoard()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            EditorUtility.DisplayDialog(
                "Шашки",
                "Сначала остановите Play Mode.",
                "ОК");

            return;
        }

        Scene scene = SceneManager.GetActiveScene();

        if (!scene.IsValid() || string.IsNullOrEmpty(scene.path))
        {
            EditorUtility.DisplayDialog(
                "Шашки",
                "Сначала откройте и сохраните игровую сцену TacticsScene.",
                "ОК");

            return;
        }

        Battlefield battlefield =
            Object.FindObjectOfType<Battlefield>();

        if (battlefield == null)
        {
            EditorUtility.DisplayDialog(
                "Шашки",
                "В текущей сцене не найден объект Battlefield.",
                "ОК");

            return;
        }

        GameObject cellPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                CellPrefabPath);

        GameObject unitPrefab =
            AssetDatabase.LoadAssetAtPath<GameObject>(
                UnitPrefabPath);

        Material lightMaterial =
            LoadMaterial("CellLight.mat");

        Material darkMaterial =
            LoadMaterial("CellDark.mat");

        Material hoverMaterial =
            LoadMaterial("CellHover.mat");

        Material moveMaterial =
            LoadMaterial("CellMove.mat");

        Material attackMaterial =
            LoadMaterial("CellAttack.mat");

        Material selectedMaterial =
            LoadMaterial("CellSelected.mat");

        Material pendingMaterial =
            LoadMaterial("CellPending.mat");

        Material whiteUnitMaterial =
            LoadMaterial("WhiteUnit.mat");

        Material blackUnitMaterial =
            LoadMaterial("BlackUnit.mat");

        Material whiteKingMaterial =
            LoadMaterial("WhiteKing.mat");

        Material blackKingMaterial =
            LoadMaterial("BlackKing.mat");

        if (
            cellPrefab == null ||
            unitPrefab == null ||
            lightMaterial == null ||
            darkMaterial == null ||
            hoverMaterial == null ||
            moveMaterial == null ||
            attackMaterial == null ||
            selectedMaterial == null ||
            pendingMaterial == null ||
            whiteUnitMaterial == null ||
            blackUnitMaterial == null ||
            whiteKingMaterial == null ||
            blackKingMaterial == null)
        {
            EditorUtility.DisplayDialog(
                "Шашки",
                "Не найдены необходимые prefab или материалы.",
                "ОК");

            return;
        }

        AssignBattlefieldMaterials(
            battlefield,
            lightMaterial,
            darkMaterial,
            hoverMaterial,
            moveMaterial,
            attackMaterial,
            selectedMaterial,
            pendingMaterial,
            whiteUnitMaterial,
            blackUnitMaterial,
            whiteKingMaterial,
            blackKingMaterial);

        RemoveOldBoard(battlefield.transform);

        GameObject cellsRoot =
            new GameObject("Cells");

        Undo.RegisterCreatedObjectUndo(
            cellsRoot,
            "Create Cells root");

        cellsRoot.transform.SetParent(
            battlefield.transform,
            false);

        GameObject unitsRoot =
            new GameObject("Units");

        Undo.RegisterCreatedObjectUndo(
            unitsRoot,
            "Create Units root");

        unitsRoot.transform.SetParent(
            battlefield.transform,
            false);

        Cell[,] cells =
            new Cell[
                Battlefield.BoardSize,
                Battlefield.BoardSize];

        for (int x = 0;
             x < Battlefield.BoardSize;
             x++)
        {
            for (int y = 0;
                 y < Battlefield.BoardSize;
                 y++)
            {
                Material material =
                    (x + y) % 2 == 1
                        ? darkMaterial
                        : lightMaterial;

                cells[x, y] =
                    CreateCell(
                        cellPrefab,
                        cellsRoot.transform,
                        x,
                        y,
                        material);
            }
        }

        for (int y = 0; y < 3; y++)
        {
            for (int x = 0;
                 x < Battlefield.BoardSize;
                 x++)
            {
                if ((x + y) % 2 != 1)
                    continue;

                CreateUnit(
                    unitPrefab,
                    unitsRoot.transform,
                    cells[x, y],
                    x,
                    y,
                    PlayerTeam.White,
                    whiteUnitMaterial);
            }
        }

        for (
            int y = Battlefield.BoardSize - 3;
            y < Battlefield.BoardSize;
            y++)
        {
            for (int x = 0;
                 x < Battlefield.BoardSize;
                 x++)
            {
                if ((x + y) % 2 != 1)
                    continue;

                CreateUnit(
                    unitPrefab,
                    unitsRoot.transform,
                    cells[x, y],
                    x,
                    y,
                    PlayerTeam.Black,
                    blackUnitMaterial);
            }
        }

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);

        AssetDatabase.SaveAssets();

        Selection.activeObject =
            battlefield.gameObject;

        EditorUtility.DisplayDialog(
            "Шашки",
            "Стартовое поле сохранено непосредственно в сцене.\n\n" +
            "В Battlefield теперь должны быть:\n" +
            "Cells — 64 клетки\n" +
            "Units — 24 шашки\n\n" +
            "Повторно запускать эту команду при обычном открытии проекта не нужно.",
            "ОК");
    }

    private static Cell CreateCell(
        GameObject prefab,
        Transform parent,
        int x,
        int y,
        Material material)
    {
        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab)
            as GameObject;

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Create Cell");

        instance.transform.SetParent(
            parent,
            false);

        instance.transform.localPosition =
            new Vector3(x, 0f, y);

        instance.name =
            $"Клетка [{x}, {y}]";

        Renderer renderer =
            instance.GetComponent<Renderer>();

        renderer.sharedMaterial = material;

        return instance.GetComponent<Cell>();
    }

    private static void CreateUnit(
        GameObject prefab,
        Transform parent,
        Cell cell,
        int x,
        int y,
        PlayerTeam team,
        Material material)
    {
        GameObject instance =
            PrefabUtility.InstantiatePrefab(prefab)
            as GameObject;

        Undo.RegisterCreatedObjectUndo(
            instance,
            "Create Unit");

        instance.transform.SetParent(
            parent,
            false);

        instance.transform.position =
            cell.transform.position +
            Vector3.up * 0.35f;

        string teamName =
            team == PlayerTeam.White
                ? "Белая"
                : "Чёрная";

        instance.name =
            $"{teamName} шашка [{x}, {y}]";

        Renderer renderer =
            instance.GetComponent<Renderer>();

        renderer.sharedMaterial = material;
    }

    private static void RemoveOldBoard(
        Transform battlefieldTransform)
    {
        Transform cellsRoot =
            battlefieldTransform.Find("Cells");

        if (cellsRoot != null)
        {
            Undo.DestroyObjectImmediate(
                cellsRoot.gameObject);
        }

        Transform unitsRoot =
            battlefieldTransform.Find("Units");

        if (unitsRoot != null)
        {
            Undo.DestroyObjectImmediate(
                unitsRoot.gameObject);
        }
    }

    private static Material LoadMaterial(
        string fileName)
    {
        return AssetDatabase.LoadAssetAtPath<Material>(
            MaterialsPath + fileName);
    }

    private static void AssignBattlefieldMaterials(
        Battlefield battlefield,
        Material lightMaterial,
        Material darkMaterial,
        Material hoverMaterial,
        Material moveMaterial,
        Material attackMaterial,
        Material selectedMaterial,
        Material pendingMaterial,
        Material whiteUnitMaterial,
        Material blackUnitMaterial,
        Material whiteKingMaterial,
        Material blackKingMaterial)
    {
        SerializedObject serialized =
            new SerializedObject(battlefield);

        serialized
            .FindProperty("lightMaterial")
            .objectReferenceValue =
            lightMaterial;

        serialized
            .FindProperty("darkMaterial")
            .objectReferenceValue =
            darkMaterial;

        serialized
            .FindProperty("hoverMaterial")
            .objectReferenceValue =
            hoverMaterial;

        serialized
            .FindProperty("moveMaterial")
            .objectReferenceValue =
            moveMaterial;

        serialized
            .FindProperty("attackMaterial")
            .objectReferenceValue =
            attackMaterial;

        serialized
            .FindProperty("selectedMaterial")
            .objectReferenceValue =
            selectedMaterial;

        serialized
            .FindProperty("pendingMaterial")
            .objectReferenceValue =
            pendingMaterial;

        serialized
            .FindProperty("whiteUnitMaterial")
            .objectReferenceValue =
            whiteUnitMaterial;

        serialized
            .FindProperty("blackUnitMaterial")
            .objectReferenceValue =
            blackUnitMaterial;

        serialized
            .FindProperty("whiteKingMaterial")
            .objectReferenceValue =
            whiteKingMaterial;

        serialized
            .FindProperty("blackKingMaterial")
            .objectReferenceValue =
            blackKingMaterial;

        serialized.ApplyModifiedPropertiesWithoutUndo();
    }
}
