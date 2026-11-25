#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;


public static class ArcherAreaEditor
{
    private const string NetworkName = "ArcherNetworkConnected";
    private const string BackupFolder = "Assets/TD_Backups";
    private const string LevelSceneFile = "LevelScene.unity";

    [MenuItem("Tools/Map/Create Archer Area")]
    private static void Build()
    {
        if (!CanEditScene(out Scene scene))
            return;

        if (FindNetwork(scene) != null)
        {
            EditorUtility.DisplayDialog("Archer route", "Connected route already exists. Use Restore if you want to rebuild it.", "OK");
            return;
        }

        List<ArcherArea> areas = UnityEngine.Object.FindObjectsOfType<ArcherArea>(true)
            .Where(a => a.gameObject.scene == scene && a.gameObject.activeInHierarchy)
            .ToList();


        if (areas.Count != 7 || areas.Any(a => !(a.GetComponent<Collider2D>() is BoxCollider2D)))
        {
            EditorUtility.DisplayDialog("Archer route",
                "Expected exactly seven active ArcherArea objects with BoxCollider2D in LevelScene. Found " + areas.Count + ". Scene was not changed.", "OK");
            return;
        }

        Transform parent = areas[0].transform.parent;
        if (parent == null || areas.Any(a => a.transform.parent != parent))
        {
            EditorUtility.DisplayDialog("Archer route", "ArcherArea objects do not share the same parent. Scene was not changed.", "OK");
            return;
        }

        List<ArcherArea> vertical = areas.Where(a => a.GetComponent<BoxCollider2D>().bounds.size.y > a.GetComponent<BoxCollider2D>().bounds.size.x * 1.6f).ToList();
        if (vertical.Count != 1)
        {
            EditorUtility.DisplayDialog("Archer route", "Expected one vertical left-hand ArcherArea. Scene was not changed.", "OK");
            return;
        }

        ArcherArea left = vertical[0];
        float middleY = left.GetComponent<BoxCollider2D>().bounds.center.y;
        List<ArcherArea> top = areas.Where(a => a != left && a.GetComponent<BoxCollider2D>().bounds.center.y > middleY).ToList();
        List<ArcherArea> bottom = areas.Where(a => a != left && a.GetComponent<BoxCollider2D>().bounds.center.y < middleY).ToList();
        if (top.Count != 3 || bottom.Count != 3 ||
            (top.Max(a => a.GetComponent<BoxCollider2D>().bounds.center.y) - top.Min(a => a.GetComponent<BoxCollider2D>().bounds.center.y) > 0.3f) ||
            (bottom.Max(a => a.GetComponent<BoxCollider2D>().bounds.center.y) - bottom.Min(a => a.GetComponent<BoxCollider2D>().bounds.center.y) > 0.3f))
        {
            EditorUtility.DisplayDialog("Archer route", "The top/bottom area arrangement differs from the original map. Scene was not changed.", "OK");
            return;
        }

        Bounds leftBounds = left.GetComponent<BoxCollider2D>().bounds;
        float xLeft = Mathf.Min(leftBounds.min.x, top.Min(a => a.GetComponent<BoxCollider2D>().bounds.min.x), bottom.Min(a => a.GetComponent<BoxCollider2D>().bounds.min.x));
        float xLeftInner = leftBounds.max.x;
        float xTopRight = top.Max(a => a.GetComponent<BoxCollider2D>().bounds.max.x);
        float xBottomRight = bottom.Max(a => a.GetComponent<BoxCollider2D>().bounds.max.x);
        float yTopInner = top.Min(a => a.GetComponent<BoxCollider2D>().bounds.min.y);
        float yTopOuter = top.Max(a => a.GetComponent<BoxCollider2D>().bounds.max.y);
        float yBottomInner = bottom.Max(a => a.GetComponent<BoxCollider2D>().bounds.max.y);
        float yBottomOuter = bottom.Min(a => a.GetComponent<BoxCollider2D>().bounds.min.y);

        if (!(xLeft < xLeftInner && xLeftInner < Mathf.Min(xTopRight, xBottomRight) &&
              yBottomOuter < yBottomInner && yBottomInner < yTopInner && yTopInner < yTopOuter) ||
            yTopOuter - yTopInner > 0.7f || yBottomInner - yBottomOuter > 0.7f || xLeftInner - xLeft > 0.7f)
        {
            EditorUtility.DisplayDialog("Archer route", "Area bounds are not consistent with the original narrow U-shaped corridor. Scene was not changed.", "OK");
            return;
        }


        Vector2[] worldPolygon =
        {
            new Vector2(xLeft, yBottomOuter),
            new Vector2(xBottomRight, yBottomOuter),
            new Vector2(xBottomRight, yBottomInner),
            new Vector2(xLeftInner, yBottomInner),
            new Vector2(xLeftInner, yTopInner),
            new Vector2(xTopRight, yTopInner),
            new Vector2(xTopRight, yTopOuter),
            new Vector2(xLeft, yTopOuter)
        };


        foreach (Transform t in UnityEngine.Object.FindObjectsOfType<Transform>(true))
        {
            if (t.gameObject.scene != scene)
                continue;
            string prefabPath = PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(t.gameObject);
            if (!prefabPath.EndsWith("/PathCell.prefab", StringComparison.OrdinalIgnoreCase))
                continue;
            if (!ContainsPoint(worldPolygon, t.position))
                continue;
            EditorUtility.DisplayDialog("Archer route", "New route would cross a road cell near " + t.position + ". Nothing was changed.", "OK");
            return;
        }

        if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
            return;

        if (!AssetDatabase.IsValidFolder(BackupFolder))
            AssetDatabase.CreateFolder("Assets", "TD_Backups");
        string backupPath = AssetDatabase.GenerateUniqueAssetPath(BackupFolder + "/LevelScene_before_connected_archers.unity");
        if (!AssetDatabase.CopyAsset(scene.path, backupPath))
        {
            EditorUtility.DisplayDialog("Archer route", "Could not back up the scene. Nothing was changed.", "OK");
            return;
        }

        int capacity = 0;
        foreach (ArcherArea a in areas)
            capacity += Mathf.Max(1, a.Capacity);

        GameObject route = new GameObject(NetworkName);
        Undo.RegisterCreatedObjectUndo(route, "Create connected Archer route");
        route.transform.SetParent(parent, false);
        route.transform.localPosition = Vector3.zero;
        route.transform.localRotation = Quaternion.identity;
        route.transform.localScale = Vector3.one;

        PolygonCollider2D polygon = Undo.AddComponent<PolygonCollider2D>(route);
        polygon.isTrigger = true;
        Vector2[] localPoints = worldPolygon.Select(p => (Vector2)route.transform.InverseTransformPoint(p)).ToArray();
        polygon.pathCount = 1;
        polygon.SetPath(0, localPoints);

        ArcherArea network = Undo.AddComponent<ArcherArea>(route);
        SerializedObject serialized = new SerializedObject(network);
        serialized.FindProperty("_areaCollider").objectReferenceValue = polygon;
        serialized.FindProperty("_capacity").intValue = capacity;
        serialized.ApplyModifiedPropertiesWithoutUndo();

        foreach (ArcherArea a in areas)
        {
            Undo.RecordObject(a.gameObject, "Disable previous ArcherArea");
            a.gameObject.SetActive(false);
        }

        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = route;
        SceneView.RepaintAll();
        Debug.Log("[Tower Defense] Connected Archer route created. Preserved " + capacity +
                  " capacity slots, disabled seven old areas, backed up scene to " + backupPath);
        EditorUtility.DisplayDialog("Archer route",
            "Connected route created. Original 7 areas are disabled, not deleted.\n\nBackup: " + backupPath +
            "\n\nSave the scene (Cmd+S), then Play and test movement.", "OK");
    }

    [MenuItem("Tools/Map/Restore Archer Areas")]
    private static void Restore()
    {
        if (!CanEditScene(out Scene scene))
            return;

        GameObject network = FindNetwork(scene);
        if (network == null)
        {
            EditorUtility.DisplayDialog("Archer route", "Connected Archer route was not found. Scene was not changed.", "OK");
            return;
        }

        Transform parent = network.transform.parent;
        int restored = 0;
        if (parent != null)
        {
            foreach (Transform child in parent)
            {
                if (child == network.transform)
                    continue;
                ArcherArea a = child.GetComponent<ArcherArea>();
                if (a == null || child.gameObject.activeSelf)
                    continue;
                Undo.RecordObject(child.gameObject, "Restore original ArcherArea");
                child.gameObject.SetActive(true);
                restored++;
            }
        }
        Undo.DestroyObjectImmediate(network);
        EditorSceneManager.MarkSceneDirty(scene);
        Debug.Log("[Tower Defense] Reverted connected Archer route; restored " + restored + " old ArcherAreas.");
        EditorUtility.DisplayDialog("Archer route", "Restored " + restored + " original ArcherArea objects. Save the scene (Cmd+S).", "OK");
    }

    private static bool CanEditScene(out Scene scene)
    {
        scene = SceneManager.GetActiveScene();
        if (EditorApplication.isPlayingOrWillChangePlaymode ||
            !scene.IsValid() || string.IsNullOrEmpty(scene.path) ||
            Path.GetFileName(scene.path) != LevelSceneFile)
        {
            EditorUtility.DisplayDialog("Archer route", "Open Assets/Scenes/LevelScene.unity and stop Play Mode first.", "OK");
            return false;
        }
        return true;
    }

    private static GameObject FindNetwork(Scene scene)
    {
        return UnityEngine.Object.FindObjectsOfType<ArcherArea>(true)
            .Where(a => a.gameObject.scene == scene && a.gameObject.name == NetworkName)
            .Select(a => a.gameObject).FirstOrDefault();
    }

    private static bool ContainsPoint(IReadOnlyList<Vector2> polygon, Vector2 point)
    {
        bool inside = false;
        for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
        {
            Vector2 a = polygon[i];
            Vector2 b = polygon[j];
            if ((a.y > point.y) == (b.y > point.y))
                continue;
            float x = a.x + (point.y - a.y) * (b.x - a.x) / (b.y - a.y);
            if (point.x < x)
                inside = !inside;
        }
        return inside;
    }
}
#endif
