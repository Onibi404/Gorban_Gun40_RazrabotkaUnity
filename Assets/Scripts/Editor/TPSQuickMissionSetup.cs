#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.SceneManagement;

/// <summary>One-click scene setup; does not edit existing player, weapons or spawner.</summary>
public static class TPSQuickMissionSetup
{
    private const string RootName = "TPS_ThreeZoneMission";
    private const string MaterialFolder = "Assets/_Project/Materials/TPSMission";

    [MenuItem("Tools/TPS Shooter/Setup 3-Zone Mission (51-60)")]
    public static void CreateMission()
    {
        Scene scene = SceneManager.GetActiveScene();
        if (scene.name != "scene_sandbox")
        {
            EditorUtility.DisplayDialog("TPS Shooter", "Open scene_sandbox first (not MainMenu).", "OK");
            return;
        }

        if (GameObject.Find(RootName) != null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "TPS_ThreeZoneMission already exists. Setup has not duplicated it.", "OK");
            return;
        }

        PlayerHealth player = Object.FindObjectOfType<PlayerHealth>(true);
        LevelFlowController flow = Object.FindObjectOfType<LevelFlowController>(true);
        KillCounter counter = Object.FindObjectOfType<KillCounter>(true);

        if (player == null || flow == null || counter == null)
        {
            EditorUtility.DisplayDialog("TPS Shooter",
                "Could not find PlayerHealth, LevelFlowController or KillCounter. " +
                "Make sure scene_sandbox is open and these components are present.", "OK");
            return;
        }

        Vector3 forward = Vector3.ProjectOnPlane(player.transform.forward, Vector3.up).normalized;
        if (forward.sqrMagnitude < 0.1f) forward = Vector3.forward;

        Vector3 playerPosition = player.transform.position;
        NavMeshHit startHit;
        bool hasNavMesh = NavMesh.SamplePosition(playerPosition, out startHit, 4f, NavMesh.AllAreas);
        Vector3 start = hasNavMesh ? startHit.position : playerPosition;
        var selected = new List<Vector3> { start };
        Vector3 pos1 = FindReachable(start, forward, 6f, hasNavMesh, selected);
        selected.Add(pos1);
        Vector3 pos2 = FindReachable(start, forward, 16f, hasNavMesh, selected);
        selected.Add(pos2);
        Vector3 pos3 = FindReachable(start, forward, 27f, hasNavMesh, selected);

        GameObject root = new GameObject(RootName);
        Undo.RegisterCreatedObjectUndo(root, "Create TPS three-zone mission");
        TPSMissionDirector mission = root.AddComponent<TPSMissionDirector>();
        mission.Configure(flow, player, counter, 6);

        Material green = GetMaterial("TPS_Zone1_Green", new Color(0.1f, 0.9f, 0.35f));
        Material cyan = GetMaterial("TPS_Zone2_Cyan", new Color(0.05f, 0.85f, 1f));
        Material violet = GetMaterial("TPS_Exit_Violet", new Color(0.67f, 0.25f, 0.95f));
        Material yellow = GetMaterial("TPS_Keycard_Yellow", new Color(1f, 0.82f, 0.05f));
        Material steel = GetMaterial("TPS_Gate_Steel", new Color(0.24f, 0.31f, 0.40f));
        Material red = GetMaterial("TPS_Trap_Red", new Color(0.9f, 0.12f, 0.13f));

        GameObject zone1 = CreateZone(root.transform, "Zone_01_Start", pos1,
            TPSMissionZone.ZoneId.First, mission, green);
        GameObject zone2 = CreateZone(root.transform, "Zone_02_AfterGate", pos2,
            TPSMissionZone.ZoneId.Second, mission, cyan);
        CreateZone(root.transform, "Zone_03_Evacuation", pos3,
            TPSMissionZone.ZoneId.Exit, mission, violet);

        CreateCard(zone1.transform, mission, yellow);

        Vector3 direction = Vector3.ProjectOnPlane(pos2 - pos1, Vector3.up).normalized;
        if (direction.sqrMagnitude < 0.1f) direction = forward;
        Vector3 gatePosition = pos2 - direction * 3.5f;
        NavMeshHit gateNav;
        if (NavMesh.SamplePosition(gatePosition, out gateNav, 2.5f, NavMesh.AllAreas))
            gatePosition = gateNav.position;
        CreateGate(root.transform, "LockedGate_PressE", gatePosition, direction,
            mission, player.transform, steel, yellow);

        // Optional additional interactives: timed medkit and damage zone.
        CreateMedkit(zone2.transform, player, green);
        CreateDamagePad(root.transform, pos2 + Vector3.Cross(Vector3.up, direction) * 4f,
            player, red);

        EditorUtility.SetDirty(mission);
        EditorSceneManager.MarkSceneDirty(scene);
        Selection.activeGameObject = root;
        EditorGUIUtility.PingObject(root);

        string message =
            "Created TPS_ThreeZoneMission and linked it to the existing victory HUD.\n\n" +
            "IMPORTANT: Move the three Zone objects onto REACHABLE parts of the baked NavMesh " +
            "and place LockedGate_PressE on the route between zones 1 and 2. " +
            "Keep KeyCard inside Zone_01_Start. Then save the scene (Cmd+S).";
        if (!hasNavMesh)
            message += "\n\nNavMesh was NOT detected near the player. " +
                       "Placement is approximate; move the objects manually.";
        EditorUtility.DisplayDialog("TPS Shooter: mission prepared", message, "OK");
    }

    private static Vector3 FindReachable(Vector3 origin, Vector3 forward,
        float desiredDistance, bool hasNavMesh, List<Vector3> alreadyUsed)
    {
        if (!hasNavMesh) return origin + forward * desiredDistance;

        float[] angles = { 0f, 25f, -25f, 50f, -50f, 90f, -90f, 135f, -135f, 180f };
        float[] distances = { 1f, 0.8f, 1.2f, 0.6f };
        Vector3 best = origin + forward * desiredDistance;
        float bestScore = float.NegativeInfinity;

        foreach (float factor in distances)
        {
            foreach (float angle in angles)
            {
                Vector3 direction = Quaternion.AngleAxis(angle, Vector3.up) * forward;
                Vector3 candidate = origin + direction * (desiredDistance * factor);
                NavMeshHit hit;
                if (!NavMesh.SamplePosition(candidate, out hit, 3.5f, NavMesh.AllAreas))
                    continue;

                bool tooClose = false;
                foreach (Vector3 previous in alreadyUsed)
                {
                    if (Vector3.Distance(previous, hit.position) < 4.5f)
                    {
                        tooClose = true;
                        break;
                    }
                }
                if (tooClose) continue;

                NavMeshPath path = new NavMeshPath();
                if (!NavMesh.CalculatePath(origin, hit.position, NavMesh.AllAreas, path) ||
                    path.status != NavMeshPathStatus.PathComplete)
                    continue;

                float distance = Vector3.Distance(origin, hit.position);
                float score = -Mathf.Abs(distance - desiredDistance) - Mathf.Abs(angle) * 0.015f;
                if (score <= bestScore) continue;
                bestScore = score;
                best = hit.position;
            }
        }

        return bestScore > float.NegativeInfinity ? best : origin + forward * desiredDistance;
    }

    private static GameObject CreateZone(Transform parent, string name, Vector3 position,
        TPSMissionZone.ZoneId id, TPSMissionDirector mission, Material material)
    {
        GameObject zone = new GameObject(name);
        zone.transform.SetParent(parent);
        zone.transform.position = position;
        BoxCollider trigger = zone.AddComponent<BoxCollider>();
        trigger.isTrigger = true;
        trigger.center = new Vector3(0f, 1.4f, 0f);
        trigger.size = new Vector3(5f, 2.8f, 5f);
        Rigidbody body = zone.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        TPSMissionZone zoneComponent = zone.AddComponent<TPSMissionZone>();
        zoneComponent.Configure(mission, id);

        GameObject floorMarker = CreateVisual(PrimitiveType.Cylinder,
            "Visible_Marker", zone.transform, Vector3.up * 0.1f,
            new Vector3(1.25f, 0.035f, 1.25f), material);
        floorMarker.transform.localRotation = Quaternion.identity;
        CreateVisual(PrimitiveType.Sphere, "Visible_Beacon", zone.transform,
            Vector3.up * 2.2f, Vector3.one * 0.45f, material);
        Light light = zone.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = material.color;
        light.intensity = 2f;
        light.range = 8f;
        light.shadows = LightShadows.None;
        return zone;
    }

    private static void CreateCard(Transform parent, TPSMissionDirector mission,
        Material material)
    {
        GameObject card = GameObject.CreatePrimitive(PrimitiveType.Cube);
        card.name = "KeyCard_Yellow_Pickup";
        card.transform.SetParent(parent);
        card.transform.localPosition = new Vector3(1.2f, 1.1f, 0.8f);
        card.transform.localScale = new Vector3(0.65f, 0.15f, 0.45f);
        card.GetComponent<Renderer>().sharedMaterial = material;
        card.GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody body = card.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        card.AddComponent<TPSAccessCardPickup>().Configure(mission);
    }

    private static void CreateGate(Transform parent, string name, Vector3 position,
        Vector3 direction, TPSMissionDirector mission, Transform player,
        Material steel, Material yellow)
    {
        GameObject gate = new GameObject(name);
        gate.transform.SetParent(parent);
        gate.transform.position = position;
        gate.transform.rotation = Quaternion.LookRotation(direction, Vector3.up);

        Rigidbody left = CreatePanel("SlidingPanel_Left", gate.transform,
            new Vector3(-0.6f, 1.3f, 0f), steel);
        Rigidbody right = CreatePanel("SlidingPanel_Right", gate.transform,
            new Vector3(0.6f, 1.3f, 0f), steel);
        CreateVisual(PrimitiveType.Cube, "Frame_Top", gate.transform,
            new Vector3(0f, 2.7f, 0f), new Vector3(3.7f, 0.2f, 0.5f), yellow);
        CreateVisual(PrimitiveType.Cube, "Frame_Left", gate.transform,
            new Vector3(-1.8f, 1.3f, 0f), new Vector3(0.2f, 2.5f, 0.5f), steel);
        CreateVisual(PrimitiveType.Cube, "Frame_Right", gate.transform,
            new Vector3(1.8f, 1.3f, 0f), new Vector3(0.2f, 2.5f, 0.5f), steel);

        gate.AddComponent<TPSMissionGate>().Configure(mission, player, left, right);
    }

    private static Rigidbody CreatePanel(string name, Transform parent,
        Vector3 localPosition, Material material)
    {
        GameObject panel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        panel.name = name;
        panel.transform.SetParent(parent);
        panel.transform.localPosition = localPosition;
        panel.transform.localRotation = Quaternion.identity;
        panel.transform.localScale = new Vector3(1.2f, 2.5f, 0.2f);
        panel.GetComponent<Renderer>().sharedMaterial = material;
        Rigidbody body = panel.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        body.interpolation = RigidbodyInterpolation.Interpolate;
        return body;
    }

    private static void CreateMedkit(Transform zoneTwo,
        PlayerHealth player, Material material)
    {
        GameObject medkit = GameObject.CreatePrimitive(PrimitiveType.Cube);
        medkit.name = "Medkit_HealsOverTime";
        medkit.transform.SetParent(zoneTwo);
        medkit.transform.localPosition = new Vector3(-1.3f, 0.55f, 0.6f);
        medkit.transform.localScale = new Vector3(0.6f, 0.5f, 0.6f);
        medkit.GetComponent<Renderer>().sharedMaterial = material;
        medkit.GetComponent<BoxCollider>().isTrigger = true;
        Rigidbody body = medkit.AddComponent<Rigidbody>();
        body.useGravity = false;
        body.isKinematic = true;
        medkit.AddComponent<TPSTimedHealingPickup>().Configure(player);
    }

    private static void CreateDamagePad(Transform parent, Vector3 position,
        PlayerHealth player, Material material)
    {
        GameObject pad = GameObject.CreatePrimitive(PrimitiveType.Cube);
        pad.name = "Optional_RedDamageTrap";
        pad.transform.SetParent(parent);
        pad.transform.position = position + Vector3.up * 0.09f;
        pad.transform.localScale = new Vector3(1.8f, 0.18f, 1.8f);
        pad.GetComponent<Renderer>().sharedMaterial = material;
        BoxCollider collider = pad.GetComponent<BoxCollider>();
        collider.isTrigger = true;
        Rigidbody body = pad.AddComponent<Rigidbody>();
        body.isKinematic = true;
        body.useGravity = false;
        pad.AddComponent<TPSMissionDamagePad>().Configure(player);
    }

    private static GameObject CreateVisual(PrimitiveType type, string name, Transform parent,
        Vector3 position, Vector3 scale, Material material)
    {
        GameObject visual = GameObject.CreatePrimitive(type);
        visual.name = name;
        visual.transform.SetParent(parent);
        visual.transform.localPosition = position;
        visual.transform.localScale = scale;
        visual.GetComponent<Renderer>().sharedMaterial = material;
        Collider collider = visual.GetComponent<Collider>();
        if (collider != null) Object.DestroyImmediate(collider);
        return visual;
    }

    private static Material GetMaterial(string name, Color color)
    {
        EnsureFolder("Assets/_Project");
        EnsureFolder("Assets/_Project/Materials");
        EnsureFolder(MaterialFolder);
        string path = MaterialFolder + "/" + name + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null) return material;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null) shader = Shader.Find("Standard");
        material = new Material(shader) { name = name, color = color };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int separator = path.LastIndexOf('/');
        string parent = path.Substring(0, separator);
        string name = path.Substring(separator + 1);
        AssetDatabase.CreateFolder(parent, name);
    }
}
#endif
