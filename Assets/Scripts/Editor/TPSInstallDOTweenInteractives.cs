#if UNITY_EDITOR
using System.Collections.Generic;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Scene-only installation. Preserves all existing zone positions and mission links.
/// Does not change weapon, player, Zenject, menu or the spawn system.
/// </summary>
public static class TPSInstallDOTweenInteractives
{
    private const string RootName = "TPS_ThreeZoneMission";
    private const string AdditionsName = "TPS_TweenInteractives_61_70";
    private const string MaterialPath = "Assets/_Project/Materials/TPSMission/";

    [MenuItem("Tools/TPS Shooter/Install DOTween Interactives (61-70)")]
    private static void Install()
    {
        if (SceneManager.GetActiveScene().name != "scene_sandbox")
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "First open scene_sandbox (not MainMenu).", "OK");
            return;
        }

        GameObject missionObject = GameObject.Find(RootName);
        if (missionObject == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "TPS_ThreeZoneMission not found. Keep the mission from steps 51-60.", "OK");
            return;
        }

        Transform zone2 = missionObject.transform.Find("Zone_02_AfterGate");
        Transform trap = missionObject.transform.Find("Optional_RedDamageTrap");
        Transform zone1 = missionObject.transform.Find("Zone_01_Start");
        Transform gate = missionObject.transform.Find("LockedGate_PressE");
        PlayerHealth player = Object.FindObjectOfType<PlayerHealth>(true);
        LevelFlowController flow = Object.FindObjectOfType<LevelFlowController>(true);
        if (zone1 == null || zone2 == null || trap == null || gate == null ||
            player == null || flow == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Missing a zone, gate, red trap, PlayerHealth or LevelFlowController. " +
                "Do not rerun the earlier mission generator. Check the scene Hierarchy.", "OK");
            return;
        }

        // An idempotent installer: no extra duplicates on repeated clicks.
        if (zone2.Find(AdditionsName) != null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "DOTween interactives already installed. No duplicates created.", "OK");
            return;
        }

        Material steel = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "TPS_Gate_Steel.mat");
        Material cyan = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "TPS_Zone2_Cyan.mat");
        Material red = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath + "TPS_Trap_Red.mat");

        GameObject additions = new GameObject(AdditionsName);
        Undo.RegisterCreatedObjectUndo(additions, "Install TPS DOTween interactives");
        additions.transform.SetParent(zone2, false);
        additions.transform.localPosition = Vector3.zero;

        CreatePlatform(additions.transform, player, cyan);
        CreateHingedDoor(additions.transform, player.transform, steel);
        AttachSpikeVisuals(trap, red);
        AttachUIAnimations(flow);

        // The main gate has already been converted to tween movement by the
        // patched TPSMissionGate.cs. The two pickups have new animation code.
        // Their scene references are unchanged, so no reconfiguration is needed.
        TPSMissionGate gateComponent = gate.GetComponent<TPSMissionGate>();
        if (gateComponent == null)
            Debug.LogWarning("TPS: Existing gate has no TPSMissionGate component.", gate);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Selection.activeGameObject = additions;
        EditorGUIUtility.PingObject(additions);
        EditorUtility.DisplayDialog("TPS Shooter — installed",
            "Installed: sliding DOTween gate (patched script), hinged door, " +
            "moving platform, moving spikes, pickup animations, HUD pulse and " +
            "victory/defeat panel reveal.\n\n" +
            "The original mission and zone positions are unchanged. " +
            "New demonstration props are children of Zone_02_AfterGate.\n" +
            "Save scene with Cmd+S and test in Play Mode.", "OK");
    }

    private static void CreatePlatform(Transform parent, PlayerHealth player, Material material)
    {
        GameObject platform = new GameObject("MovingPlatform_Yoyo_61_70");
        platform.transform.SetParent(parent, false);
        platform.transform.localPosition = new Vector3(2.1f, 0.18f, -0.2f);
        Rigidbody body = platform.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject deck = GameObject.CreatePrimitive(PrimitiveType.Cube);
        deck.name = "Platform_Deck_Collider";
        deck.transform.SetParent(platform.transform, false);
        deck.transform.localPosition = Vector3.zero;
        deck.transform.localScale = new Vector3(2.4f, 0.25f, 2.4f);
        SetMaterial(deck, material);

        GameObject rideTrigger = new GameObject("PlayerRideTrigger");
        rideTrigger.transform.SetParent(platform.transform, false);
        BoxCollider detector = rideTrigger.AddComponent<BoxCollider>();
        detector.isTrigger = true;
        detector.center = new Vector3(0f, 1.3f, 0f);
        detector.size = new Vector3(2.35f, 2.45f, 2.35f);
        platform.AddComponent<TPSTweenMovingPlatform>().Configure(player);
    }

    private static void CreateHingedDoor(Transform parent, Transform player, Material material)
    {
        GameObject hinge = new GameObject("HingedDoor_PressE_61_70");
        hinge.transform.SetParent(parent, false);
        hinge.transform.localPosition = new Vector3(-2.2f, 0f, 2.0f);
        Rigidbody body = hinge.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;

        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = "HingedDoor_Panel";
        panel.transform.SetParent(hinge.transform, false);
        panel.transform.localPosition = new Vector3(0.70f, 1.2f, 0f);
        panel.transform.localScale = new Vector3(1.4f, 2.4f, 0.15f);
        SetMaterial(panel, material);
        hinge.AddComponent<TPSTweenHingedDoor>().Configure(player);
    }

    private static void AttachSpikeVisuals(Transform trap, Material material)
    {
        if (trap.GetComponent<TPSTweenSpikeTrap>() != null) return;
        GameObject visualRoot = new GameObject("AnimatedSpikes_61_70");
        visualRoot.transform.SetParent(trap, false);
        visualRoot.transform.localPosition = Vector3.zero;

        Vector3 loss = trap.lossyScale;
        visualRoot.transform.localScale = new Vector3(
            Mathf.Abs(loss.x) > 0.0001f ? 1f / loss.x : 1f,
            Mathf.Abs(loss.y) > 0.0001f ? 1f / loss.y : 1f,
            Mathf.Abs(loss.z) > 0.0001f ? 1f / loss.z : 1f);
        List<Transform> spikes = new List<Transform>(9);
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                GameObject spike = GameObject.CreatePrimitive(PrimitiveType.Cube);
                spike.name = "AnimatedSpike_" + (x + 1) + "_" + (z + 1);
                spike.transform.SetParent(visualRoot.transform, false);
                spike.transform.localPosition = new Vector3(x * 0.52f, -0.60f, z * 0.52f);
                spike.transform.localScale = new Vector3(0.16f, 0.82f, 0.16f);
                Collider c = spike.GetComponent<Collider>();
                if (c != null) Object.DestroyImmediate(c);
                SetMaterial(spike, material);
                spikes.Add(spike.transform);
            }
        }
        // The trap is still the same optional red trigger and retains its
        // cancellable periodic damage script from the mission package.
        trap.gameObject.AddComponent<TPSTweenSpikeTrap>().Configure(spikes.ToArray());
    }

    private static void AttachUIAnimations(LevelFlowController flow)
    {
        SerializedObject serialized = new SerializedObject(flow);
        SerializedProperty objective = serialized.FindProperty("_objectiveText");
        TMP_Text label = objective != null ? objective.objectReferenceValue as TMP_Text : null;
        if (label != null && label.GetComponent<TPSObjectiveTweenFeedback>() == null)
            Undo.AddComponent<TPSObjectiveTweenFeedback>(label.gameObject);
        else if (label == null)
            Debug.LogWarning("TPS: LevelFlow has no _objectiveText: HUD pulse skipped.", flow);

        AttachPanel(serialized, "_victoryPanel");
        AttachPanel(serialized, "_defeatPanel");
    }

    private static void AttachPanel(SerializedObject serialized, string field)
    {
        SerializedProperty property = serialized.FindProperty(field);
        GameObject panel = property != null ? property.objectReferenceValue as GameObject : null;
        if (panel == null) return;
        if (panel.GetComponent<CanvasGroup>() == null) Undo.AddComponent<CanvasGroup>(panel);
        if (panel.GetComponent<TPSResultPanelTween>() == null)
            Undo.AddComponent<TPSResultPanelTween>(panel);
    }

    private static void SetMaterial(GameObject go, Material material)
    {
        Renderer renderer = go.GetComponent<Renderer>();
        if (renderer != null && material != null) renderer.sharedMaterial = material;
    }
}
#endif
