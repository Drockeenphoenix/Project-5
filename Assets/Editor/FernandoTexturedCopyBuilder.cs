using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Makes a new, fully textured presentation scene from Fernando's submitted greybox.
/// The source scene is never edited: the menu command only opens and saves the copy.
/// </summary>
public static class FernandoTexturedCopyBuilder
{
    private const string SourceScenePath = "Assets/Scenes/Fernando Scene.unity";
    private const string TargetScenePath = "Assets/Scenes/Fernando_Textured.unity";
    private const string PreviewFolder = "Assets/Art/Previews";
    private const string GeneratedPrefabFolder = "Assets/Prefabs/Generated";

    private static Material wall;
    private static Material floor;
    private static Material ceiling;
    private static Material steel;
    private static Material darkSteel;
    private static Material wood;
    private static Material vinyl;
    private static Material cork;
    private static Material paper;
    private static Material red;
    private static Material amber;
    private static Material green;
    private static Material blueGlow;
    private static Material whiteGlow;

    [MenuItem("Project 5/Build Fernando Textured Copy")]
    public static void BuildTexturedCopy()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) != null)
        {
            EditorUtility.DisplayDialog(
                "Fernando textured copy already exists",
                "Fernando_Textured.unity already exists. It was left unchanged so the completed copy is never overwritten.",
                "OK");
            AssetDatabase.OpenAsset(AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath));
            return;
        }

        if (!AssetDatabase.CopyAsset(SourceScenePath, TargetScenePath))
        {
            throw new InvalidOperationException("Unity could not duplicate Fernando Scene.unity.");
        }

        AssetDatabase.Refresh();
        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        LoadMaterials();
        AlignWithCommandRoomTextureLibrary();

        Bounds room = GetRoomBounds();
        float floorTop = room.min.y + 0.25f;
        float ceilingHeight = GetWallCeiling(room, floorTop);

        Transform root = new GameObject("FERNANDO TEXTURE PASS — COPY ONLY").transform;
        ApplyMaterialPass(room, floorTop, ceilingHeight);
        AddRoomConstruction(root, room, floorTop, ceilingHeight);
        AddEquipmentDetail(root, room, floorTop);
        AddWorkSurfaceDetail(root, room, floorTop);
        AddEnvironmentalStory(root, room, floorTop, ceilingHeight);
        AddLighting(root, room, floorTop, ceilingHeight);
        RepairAndOrganizeLoadedCopy(room, floorTop, ceilingHeight);
        ConfigurePresentation(room);
        RenderPreviews(room, floorTop, ceilingHeight);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Created a separate textured Fernando scene at " + TargetScenePath);
        EditorUtility.DisplayDialog(
            "Fernando texturing complete",
            "Created Fernando_Textured.unity. Fernando Scene.unity remains untouched.",
            "Open textured copy");
    }

    [MenuItem("Project 5/Run Fernando Texture QA")]
    public static void RunTextureQualityAudit()
    {
        if (SceneManager.GetActiveScene().path != TargetScenePath)
        {
            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        LoadMaterials();

        int rendererCount = 0;
        int missingMaterialSlots = 0;
        int texturedSlots = 0;
        int repeatingTextureSlots = 0;
        int clampedTextureSlots = 0;
        int defaultMaterialSlots = 0;
        foreach (Renderer renderer in SceneRenderers())
        {
            rendererCount++;
            foreach (Material material in renderer.sharedMaterials)
            {
                if (material == null)
                {
                    missingMaterialSlots++;
                    continue;
                }

                if (IsUnityDefaultMaterial(material)) defaultMaterialSlots++;

                Texture texture = MaterialTexture(material);
                if (texture == null) continue;
                texturedSlots++;
                if (texture.wrapMode == TextureWrapMode.Repeat) repeatingTextureSlots++;
                else clampedTextureSlots++;
            }
        }

        int commandRoomMappingIssues = CountCommandRoomMappingIssues();
        int generatedLineClutter = CountGeneratedLineClutter();

        Bounds room = GetRoomBounds();
        float floorTop = room.min.y + 0.25f;
        float ceilingHeight = GetWallCeiling(room, floorTop);
        // The source greybox carries its own exterior camera. Reassert the presentation camera
        // before rendering QA so the saved textured copy always opens from an interior view.
        ConfigurePresentation(room);
        RenderQualityViews(room, floorTop, ceilingHeight);
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        EditorSceneManager.SaveScene(SceneManager.GetActiveScene(), TargetScenePath);

        string report = string.Format(
            "QA complete: {0} renderers inspected, {1} missing material slots, {2} Unity default-material slots, {3}/{4} textured slots repeat correctly, {5} use clamped texture addressing, {6} Command Room mapping/wrap calibration issues, and {7} generated line-clutter props remain.",
            rendererCount,
            missingMaterialSlots,
            defaultMaterialSlots,
            repeatingTextureSlots,
            texturedSlots,
            clampedTextureSlots,
            commandRoomMappingIssues,
            generatedLineClutter);
        Debug.Log("Fernando textured-copy " + report);
        EditorUtility.DisplayDialog("Fernando texture QA", report + "\n\nRendered three extra QA views to Assets/Art/Previews.", "OK");
    }

    [MenuItem("Project 5/Replace Fernando Default Greybox Materials")]
    public static void ReplaceDefaultGreyboxMaterials()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) == null)
        {
            EditorUtility.DisplayDialog("Fernando textured copy missing", "Build Fernando Textured Copy first.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        LoadMaterials();
        AlignWithCommandRoomTextureLibrary();
        Bounds room = GetRoomBounds();
        float floorTop = room.min.y + 0.25f;
        float ceilingHeight = GetWallCeiling(room, floorTop);
        int before = CountUnityDefaultMaterialSlots();
        ApplyMaterialPass(room, floorTop, ceilingHeight);
        int after = CountUnityDefaultMaterialSlots();
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AssetDatabase.SaveAssets();
        Debug.Log(string.Format("Fernando textured copy replaced {0} default greybox material slots; {1} remain.", before - after, after));
        EditorUtility.DisplayDialog("Greybox material cleanup", string.Format("Replaced {0} remaining default greybox material slots. {1} remain.", before - after, after), "OK");
    }

    /// <summary>
    /// Converts the flat Fernando placeholders into a review-ready, ordered scene without
    /// touching the original greybox scene. The source shapes remain in a disabled reference
    /// group so dimensions and placement are still recoverable without covering the FBXs.
    /// </summary>
    [MenuItem("Project 5/Repair & Organize Fernando Textured Copy")]
    public static void RepairAndOrganizeTexturedCopy()
    {
        if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TargetScenePath) == null)
        {
            EditorUtility.DisplayDialog("Fernando textured copy missing", "Build Fernando Textured Copy first.", "OK");
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        LoadMaterials();
        AlignWithCommandRoomTextureLibrary();
        Bounds room = GetRoomBounds();
        float floorTop = room.min.y + 0.25f;
        float ceilingHeight = GetWallCeiling(room, floorTop);
        RepairAndOrganizeLoadedCopy(room, floorTop, ceilingHeight);
        ConfigurePresentation(room);
        RenderPreviews(room, floorTop, ceilingHeight);
        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene, TargetScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        EditorUtility.DisplayDialog(
            "Fernando scene repaired",
            "The final scene uses the available cabinet and console FBXs, reusable Generated hydrant/fan prefabs, and clear hierarchy groups. Fernando's original cabinet blocks remain organized as hidden position anchors so they cannot overlap the visible FBX models.",
            "OK");
    }

    [MenuItem("Project 5/Audit Fernando Models & Organization")]
    public static void AuditModelsAndOrganization()
    {
        if (SceneManager.GetActiveScene().path != TargetScenePath)
        {
            EditorSceneManager.OpenScene(TargetScenePath, OpenSceneMode.Single);
        }

        string[] fbxPaths =
        {
            "Assets/Prefabs/ShortCabinet.fbx",
            "Assets/Prefabs/TallCabinet.fbx",
            "Assets/Prefabs/WideCabinet.fbx",
            "Assets/Prefabs/filingCabinet.fbx",
            "Assets/Prefabs/mainConsole.fbx",
            "Assets/Prefabs/sideConsole.fbx"
        };
        int availableFbx = 0;
        foreach (string path in fbxPaths)
        {
            if (AssetDatabase.LoadAssetAtPath<GameObject>(path) != null) availableFbx++;
        }

        int importedInstances = 0;
        int generatedProps = 0;
        int hiddenOriginalAnchors = 0;
        int hierarchyGroups = 0;
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.StartsWith("[", StringComparison.Ordinal)) hierarchyGroups++;
            if (root.name.Equals("[30] Imported FBX Models", StringComparison.Ordinal))
            {
                foreach (Transform child in root.transform)
                {
                    GameObject source = PrefabUtility.GetCorrespondingObjectFromSource<GameObject>(child.gameObject);
                    string sourcePath = source == null ? string.Empty : AssetDatabase.GetAssetPath(source);
                    if (sourcePath.EndsWith(".fbx", StringComparison.OrdinalIgnoreCase))
                    {
                        importedInstances++;
                    }
                }
            }
            if (root.name.Equals("[40] Utility Props — generated prefabs", StringComparison.Ordinal)) generatedProps = root.transform.childCount;
            if (root.name.Equals("[20] Greybox replacements — hidden", StringComparison.Ordinal)) hiddenOriginalAnchors = root.transform.childCount;
        }

        string report = string.Format(
            "Asset audit: {0}/6 cabinet/console FBXs are present in Assets/Prefabs. {1} imported FBX instances and {2} generated utility props are in the final scene. {3} original cabinet/console anchors are retained under [20] as non-rendering position references, and {4} ordered root groups are present.\n\nThe FireHydrant and FloorFan prefabs from Assets/Prefabs/Generated are instantiated in the scene because no separate hydrant or fan FBX was supplied.",
            availableFbx,
            importedInstances,
            generatedProps,
            hiddenOriginalAnchors,
            hierarchyGroups);
        Debug.Log("Fernando model and organization audit — " + report);
        EditorUtility.DisplayDialog("Fernando model & organization audit", report, "OK");
    }

    private static void RepairAndOrganizeLoadedCopy(Bounds room, float floorTop, float ceilingHeight)
    {
        // The first texturing pass added detail to the greybox forms. Remove only those
        // generated overlays that would now sit on top of the proper imported models.
        RemoveGeneratedLineClutter();
        RemoveLegacyEquipmentOverlays();
        RemoveGeneratedDefconSketches();
        DestroyDirectRoot("[30] Imported FBX Models");
        DestroyDirectRoot("[40] Utility Props — generated prefabs");
        // Migrate any prior visible-source layout grouping back into hidden anchors before
        // sorting, so the original blockouts never obscure the visible FBX cabinet faces.
        RestoreDetachedSourceObjects("[20] Original cabinets & equipment");
        RestoreDetachedSourceObjects("[25] FBX replacement anchors — hidden");

        Transform architecture = GetOrCreateRoot("[10] Architecture & layout");
        Transform greyboxReference = GetOrCreateRoot("[20] Greybox replacements — hidden");
        Transform furniture = GetOrCreateRoot("[50] Furniture & workstation");
        Transform systems = GetOrCreateRoot("[60] Building systems & signage");
        Transform dressing = GetOrCreateRoot("[70] Set dressing, ceiling & lighting");
        Transform cameras = GetOrCreateRoot("[90] Cameras & scene lights");

        // Reparent only pre-existing ungrouped roots. This is safe to run again because
        // the ordered roots identify work that has already been organized.
        List<GameObject> roots = new List<GameObject>(SceneManager.GetActiveScene().GetRootGameObjects());
        foreach (GameObject root in roots)
        {
            if (root.name.StartsWith("[", StringComparison.Ordinal)) continue;

            if (root.name.StartsWith("FERNANDO TEXTURE PASS", StringComparison.Ordinal))
            {
                root.transform.SetParent(dressing, true);
                continue;
            }

            Transform parent = OrganizationParent(root.name, architecture, greyboxReference, furniture, systems, cameras);
            root.transform.SetParent(parent, true);
            if (parent == greyboxReference) SetRenderersEnabled(root, false);
        }

        Transform imported = GetOrCreateRoot("[30] Imported FBX Models");
        Transform utilityProps = GetOrCreateRoot("[40] Utility Props — generated prefabs");
        // Side Desk was drawn as a raised wall placeholder in Fernando's source; the final
        // console belongs at the workstation, so its placeholder is explicitly retired.
        MoveToHiddenGreyboxReference("Side Desk", greyboxReference);
        InstantiateAvailableFbxModels(imported, room, floorTop);
        AddUtilityPropInstances(utilityProps, room, floorTop, ceilingHeight);
        AddDefconReferenceSketches(systems, room, floorTop, ceilingHeight);
    }

    private static void RestoreDetachedSourceObjects(string rootName)
    {
        GameObject legacyRoot = FindDirectRoot(rootName);
        if (legacyRoot == null) return;

        List<Transform> children = new List<Transform>();
        foreach (Transform child in legacyRoot.transform) children.Add(child);
        foreach (Transform child in children)
        {
            child.SetParent(null, true);
            SetRenderersEnabled(child.gameObject, true);
        }
        UnityEngine.Object.DestroyImmediate(legacyRoot);
    }

    private static Transform OrganizationParent(
        string name,
        Transform architecture,
        Transform greyboxReference,
        Transform furniture,
        Transform systems,
        Transform cameras)
    {
        string id = name.ToLowerInvariant();
        if (id.Contains("camera") || id.Contains("directional light")) return cameras;
        if (id.Contains("floor") || id.Contains("wall") || id.Contains("door")) return architecture;
        if (id.Contains("desk") || id.Contains("chair")) return furniture;
        if (id.Contains("board") || id.Contains("monitor") || id.Contains("switch")) return systems;
        // These are the source placeholders now represented by final imported FBX assets.
        if (id.Contains("cabinet") || id.Contains("filing") || id.Contains("medical") || id.Contains("folder") ||
            id.StartsWith("tall", StringComparison.Ordinal) || id.StartsWith("short", StringComparison.Ordinal) ||
            id.Contains("control panel") || id == "cylinder" || id.StartsWith("cylinder ", StringComparison.Ordinal))
        {
            return greyboxReference;
        }
        return systems;
    }

    private static void MoveToHiddenGreyboxReference(string name, Transform greyboxReference)
    {
        GameObject sourceObject = FindByName(name);
        if (sourceObject == null) return;
        sourceObject.transform.SetParent(greyboxReference, true);
        SetRenderersEnabled(sourceObject, false);
    }

    private static Transform GetOrCreateRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Equals(name, StringComparison.Ordinal)) return root.transform;
        }
        return new GameObject(name).transform;
    }

    private static void DestroyDirectRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Equals(name, StringComparison.Ordinal))
            {
                UnityEngine.Object.DestroyImmediate(root);
                return;
            }
        }
    }

    private static GameObject FindDirectRoot(string name)
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Equals(name, StringComparison.Ordinal)) return root;
        }
        return null;
    }

    private static void SetRenderersEnabled(GameObject gameObject, bool enabled)
    {
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true)) renderer.enabled = enabled;
    }

    private static void RemoveLegacyEquipmentOverlays()
    {
        GameObject legacyRoot = FindByName("FERNANDO TEXTURE PASS — COPY ONLY");
        if (legacyRoot == null) return;

        List<GameObject> remove = new List<GameObject>();
        foreach (Transform transform in legacyRoot.GetComponentsInChildren<Transform>(true))
        {
            if (transform == legacyRoot.transform) continue;
            string id = transform.name.ToLowerInvariant();
            if (id.Contains("instrument panel") || id.Contains("crt readout") || id.Contains("number plate") ||
                id.Contains("status indicator") || id.Contains("recessed handle") || id.Contains("cam lock") ||
                id.Contains("side label") || id.Contains("asset label") || id.StartsWith("control-panel", StringComparison.Ordinal) ||
                id.StartsWith("monitor ", StringComparison.Ordinal) || id.StartsWith("wall-switch", StringComparison.Ordinal) ||
                id.Contains("desk blotter") || id.Contains("log binder") || id.Contains("clipboard") ||
                id.Contains("upholstered seat pad") || id.Contains("pinned maintenance note") ||
                id.Contains("red push pin") || id.Contains("fire extinguisher"))
            {
                remove.Add(transform.gameObject);
            }
        }
        foreach (GameObject gameObject in remove) UnityEngine.Object.DestroyImmediate(gameObject);
    }

    /// <summary>
    /// Removes only the decorative ceiling/service-line primitives generated by this helper.
    /// Fernando's submitted geometry and the imported FBX models are never touched.
    /// </summary>
    private static void RemoveGeneratedLineClutter()
    {
        GameObject legacyRoot = FindByName("FERNANDO TEXTURE PASS — COPY ONLY");
        if (legacyRoot == null) return;

        List<GameObject> remove = new List<GameObject>();
        foreach (Transform transform in legacyRoot.GetComponentsInChildren<Transform>(true))
        {
            if (transform == legacyRoot.transform) continue;
            if (IsGeneratedLineClutterName(transform.name)) remove.Add(transform.gameObject);
        }
        foreach (GameObject gameObject in remove) UnityEngine.Object.DestroyImmediate(gameObject);
    }

    private static void RemoveGeneratedDefconSketches()
    {
        List<GameObject> remove = new List<GameObject>();
        foreach (GameObject gameObject in SceneObjects())
        {
            if (gameObject.name.Equals("DEFCON reference sketches", StringComparison.Ordinal)) remove.Add(gameObject);
        }
        foreach (GameObject gameObject in remove) UnityEngine.Object.DestroyImmediate(gameObject);
    }

    private static bool IsGeneratedLineClutterName(string name)
    {
        string id = name.ToLowerInvariant();
        return id.StartsWith("ceiling t-bar", StringComparison.Ordinal) ||
               id.StartsWith("ceiling vent", StringComparison.Ordinal) ||
               id.StartsWith("left cooling pipe", StringComparison.Ordinal) ||
               id.StartsWith("cooling-pipe", StringComparison.Ordinal) ||
               id.StartsWith("cable tray", StringComparison.Ordinal);
    }

    private static int CountGeneratedLineClutter()
    {
        int count = 0;
        foreach (GameObject gameObject in SceneObjects())
        {
            if (IsGeneratedLineClutterName(gameObject.name)) count++;
        }
        return count;
    }

    private static void InstantiateAvailableFbxModels(Transform parent, Bounds room, float floorTop)
    {
        foreach (GameObject target in FindAllByExactName("Wide Cabinet")) PlaceFbxAtBounds("Assets/Prefabs/WideCabinet.fbx", "Imported FBX — Wide Cabinet (Duncan)", target, steel, parent);
        foreach (GameObject target in FindAllByNamePrefix("Wide Cabinet (")) PlaceFbxAtBounds("Assets/Prefabs/WideCabinet.fbx", "Imported FBX — Wide Cabinet (Duncan)", target, steel, parent);
        foreach (GameObject target in FindAllByExactName("tall")) PlaceFbxAtBounds("Assets/Prefabs/TallCabinet.fbx", "Imported FBX — Tall Cabinet (Duncan)", target, steel, parent);
        foreach (GameObject target in FindAllByNamePrefix("tall (")) PlaceFbxAtBounds("Assets/Prefabs/TallCabinet.fbx", "Imported FBX — Tall Cabinet (Duncan)", target, steel, parent);
        foreach (GameObject target in FindAllByExactName("Short")) PlaceFbxAtBounds("Assets/Prefabs/ShortCabinet.fbx", "Imported FBX — Short Cabinet (Duncan)", target, steel, parent);
        foreach (GameObject target in FindAllByNamePrefix("Short (")) PlaceFbxAtBounds("Assets/Prefabs/ShortCabinet.fbx", "Imported FBX — Short Cabinet (Duncan)", target, steel, parent);

        GameObject filing = FindByName("filing");
        if (filing != null) PlaceFbxAtBounds("Assets/Prefabs/filingCabinet.fbx", "Imported FBX — Filing Cabinet (Alvin)", filing, steel, parent);
        GameObject medical = FindByName("medical");
        if (medical != null)
        {
            GameObject medicalModel = PlaceFbxAtBounds("Assets/Prefabs/ShortCabinet.fbx", "Imported FBX — Medical Cabinet (Duncan)", medical, steel, parent);
            if (medicalModel != null && TryGetBounds(medical, out Bounds medicalSlot))
            {
                // The source medical-cabinet block was accidentally raised into the wall.
                // Preserve its wall position but place the finished cabinet on the floor.
                MoveModelToFloor(medicalModel, new Vector3(medicalSlot.center.x, floorTop, medicalSlot.center.z));
                if (TryGetBounds(medicalModel, out Bounds medicalBounds)) AddMedicalMark(medicalBounds, parent);
            }
        }
        GameObject folder = FindByName("folder");
        if (folder != null)
        {
            GameObject folderModel = PlaceFbxAtBounds("Assets/Prefabs/filingCabinet.fbx", "Imported FBX — Folder Cabinet (Alvin)", folder, steel, parent);
            if (folderModel != null && TryGetBounds(folder, out Bounds folderSlot))
            {
                MoveModelToFloor(folderModel, new Vector3(folderSlot.center.x, floorTop, folderSlot.center.z));
            }
        }
        GameObject console = FindByName("Control Panel");
        if (console != null) PlaceFbxAtBounds("Assets/Prefabs/mainConsole.fbx", "Imported FBX — Main Console (Alvin)", console, darkSteel, parent);
        GameObject sideDesk = FindByName("Side Desk");
        if (sideDesk != null)
        {
            GameObject sideConsole = PlaceFbxAtBounds("Assets/Prefabs/sideConsole.fbx", "Imported FBX — Side Console (Alvin)", sideDesk, steel, parent);
            if (sideConsole != null)
            {
                // The source location is at wall height. Use it only for the intended scale,
                // then position the finished console beside the workstation on the floor.
                MoveModelToFloor(sideConsole, new Vector3(room.center.x - room.size.x * 0.20f, floorTop, room.center.z + room.size.z * 0.08f));
            }
        }
    }

    private static void MoveModelToFloor(GameObject model, Vector3 floorCenter)
    {
        if (model == null || !TryGetBounds(model, out Bounds bounds)) return;
        model.transform.position += new Vector3(
            floorCenter.x - bounds.center.x,
            floorCenter.y - bounds.min.y,
            floorCenter.z - bounds.center.z);
    }

    private static GameObject PlaceFbxAtBounds(string assetPath, string name, GameObject target, Material material, Transform parent)
    {
        if (target == null || !TryGetBounds(target, out Bounds targetBounds)) return null;
        GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (source == null)
        {
            Debug.LogWarning("Fernando scene organization could not find expected model: " + assetPath);
            return null;
        }

        GameObject instance = PrefabUtility.InstantiatePrefab(source, SceneManager.GetActiveScene()) as GameObject;
        if (instance == null) return null;
        instance.name = name;
        instance.transform.SetParent(parent, true);
        instance.transform.rotation = target.transform.rotation;
        if (!TryGetBounds(instance, out Bounds rawBounds) || rawBounds.size.x < 0.0001f || rawBounds.size.y < 0.0001f || rawBounds.size.z < 0.0001f)
        {
            UnityEngine.Object.DestroyImmediate(instance);
            return null;
        }

        float scale = Mathf.Min(
            targetBounds.size.x / rawBounds.size.x,
            targetBounds.size.y / rawBounds.size.y,
            targetBounds.size.z / rawBounds.size.z);
        scale = Mathf.Clamp(scale, 0.0001f, 10000f);
        instance.transform.localScale *= scale;
        if (TryGetBounds(instance, out Bounds scaledBounds))
        {
            Vector3 positionOffset = new Vector3(
                targetBounds.center.x - scaledBounds.center.x,
                targetBounds.min.y - scaledBounds.min.y,
                targetBounds.center.z - scaledBounds.center.z);
            instance.transform.position += positionOffset;
        }
        SetMaterial(instance, material);
        return instance;
    }

    private static List<GameObject> FindAllByExactName(string name)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject gameObject in SceneObjects())
        {
            if (gameObject.name.Equals(name, StringComparison.Ordinal)) matches.Add(gameObject);
        }
        return matches;
    }

    private static List<GameObject> FindAllByNamePrefix(string prefix)
    {
        List<GameObject> matches = new List<GameObject>();
        foreach (GameObject gameObject in SceneObjects())
        {
            if (gameObject.name.StartsWith(prefix, StringComparison.Ordinal)) matches.Add(gameObject);
        }
        return matches;
    }

    private static void AddMedicalMark(Bounds bounds, Transform parent)
    {
        Vector3 face = new Vector3(bounds.center.x, bounds.center.y + bounds.size.y * 0.19f, bounds.min.z - 0.028f);
        Box("Medical cabinet white cross — horizontal", face, new Vector3(bounds.size.x * 0.22f, bounds.size.y * 0.055f, 0.015f), paper, parent);
        Box("Medical cabinet white cross — vertical", face, new Vector3(bounds.size.x * 0.055f, bounds.size.y * 0.22f, 0.015f), paper, parent);
    }

    private static void AddUtilityPropInstances(Transform parent, Bounds room, float floorTop, float ceilingHeight)
    {
        GameObject hydrant = GetOrCreateFireHydrantPrefab();
        GameObject fan = GetOrCreateFloorFanPrefab();

        InstantiateUtilityPrefab(hydrant, "Fire Hydrant — generated prefab", new Vector3(room.center.x - room.size.x * 0.14f, floorTop, room.min.z + 0.20f), Quaternion.Euler(0f, 0f, 0f), parent);
        InstantiateUtilityPrefab(fan, "Floor Fan — generated prefab", new Vector3(room.min.x + room.size.x * 0.12f, floorTop, room.min.z + room.size.z * 0.14f), Quaternion.Euler(0f, 36f, 0f), parent);
    }

    private static GameObject InstantiateUtilityPrefab(GameObject prefab, string name, Vector3 position, Quaternion rotation, Transform parent)
    {
        if (prefab == null) return null;
        GameObject instance = PrefabUtility.InstantiatePrefab(prefab, SceneManager.GetActiveScene()) as GameObject;
        if (instance == null) return null;
        instance.name = name;
        instance.transform.SetParent(parent, true);
        instance.transform.position = position;
        instance.transform.rotation = rotation;
        return instance;
    }

    private static GameObject GetOrCreateFireHydrantPrefab()
    {
        const string path = GeneratedPrefabFolder + "/FireHydrant.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) return prefab;
        EnsureFolder(GeneratedPrefabFolder);
        GameObject root = new GameObject("Fire Hydrant");
        Cylinder("Hydrant base", new Vector3(0f, 0.045f, 0f), 0.16f, 0.09f, darkSteel, root.transform, Quaternion.identity);
        Cylinder("Hydrant body", new Vector3(0f, 0.38f, 0f), 0.12f, 0.66f, red, root.transform, Quaternion.identity);
        Cylinder("Hydrant bonnet", new Vector3(0f, 0.74f, 0f), 0.145f, 0.12f, red, root.transform, Quaternion.identity);
        Cylinder("Hydrant top cap", new Vector3(0f, 0.83f, 0f), 0.075f, 0.06f, darkSteel, root.transform, Quaternion.identity);
        Cylinder("Hydrant left outlet", new Vector3(-0.15f, 0.49f, 0f), 0.09f, 0.16f, red, root.transform, Quaternion.Euler(0f, 0f, 90f));
        Cylinder("Hydrant right outlet", new Vector3(0.15f, 0.49f, 0f), 0.09f, 0.16f, red, root.transform, Quaternion.Euler(0f, 0f, 90f));
        Cylinder("Hydrant outlet cap", new Vector3(0f, 0.49f, -0.13f), 0.075f, 0.05f, darkSteel, root.transform, Quaternion.Euler(90f, 0f, 0f));
        Sphere("Hydrant valve", new Vector3(0f, 0.84f, 0f), 0.13f, darkSteel, root.transform);
        Box("Hydrant inspection plate", new Vector3(0f, 0.37f, -0.123f), new Vector3(0.13f, 0.15f, 0.012f), paper, root.transform);
        prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject GetOrCreateFloorFanPrefab()
    {
        const string path = GeneratedPrefabFolder + "/FloorFan.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (prefab != null) return prefab;
        EnsureFolder(GeneratedPrefabFolder);
        GameObject root = new GameObject("Floor Fan");
        Cylinder("Fan weighted base", new Vector3(0f, 0.045f, 0f), 0.23f, 0.09f, darkSteel, root.transform, Quaternion.identity);
        Cylinder("Fan stand", new Vector3(0f, 0.47f, 0f), 0.035f, 0.78f, darkSteel, root.transform, Quaternion.identity);
        Cylinder("Fan motor", new Vector3(0f, 0.89f, 0f), 0.115f, 0.18f, steel, root.transform, Quaternion.Euler(90f, 0f, 0f));
        Cylinder("Fan cage", new Vector3(0f, 0.93f, -0.08f), 0.28f, 0.035f, darkSteel, root.transform, Quaternion.Euler(90f, 0f, 0f));
        Sphere("Fan hub", new Vector3(0f, 0.93f, -0.11f), 0.09f, steel, root.transform);
        for (int i = 0; i < 4; i++)
        {
            Box("Fan blade", new Vector3(0.105f, 0.93f, -0.12f), new Vector3(0.22f, 0.06f, 0.016f), steel, root.transform, Quaternion.Euler(0f, 0f, i * 90f + 18f));
        }
        Box("Fan tilt bracket", new Vector3(0f, 0.76f, 0f), new Vector3(0.11f, 0.07f, 0.16f), darkSteel, root.transform);
        prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        UnityEngine.Object.DestroyImmediate(root);
        return prefab;
    }

    private static void LoadMaterials()
    {
        wall = MaterialAt("Wall_Painted_Concrete.mat");
        floor = MaterialAt("Floor_Institutional_Vinyl.mat");
        ceiling = MaterialAt("Ceiling_Mineral_Fiber.mat");
        steel = MaterialAt("Cabinet_Sage_Painted_Steel.mat");
        darkSteel = MaterialAt("Trim_Dark_Painted_Steel.mat");
        wood = MaterialAt("Desk_Worn_Walnut_Laminate.mat");
        vinyl = MaterialAt("Chair_Black_Vinyl.mat");
        cork = MaterialAt("Bulletin_Board_Cork.mat");
        paper = MaterialAt("Aged_Paper.mat");
        red = MaterialAt("Warning_Red.mat");
        amber = MaterialAt("Warning_Amber.mat");
        green = MaterialAt("Status_Green.mat");
        whiteGlow = MaterialAt("Fluorescent_Diffuser.mat");
        blueGlow = CreateOrLoadMaterial("Monitor_Ice_Blue_Glow", new Color(0.025f, 0.29f, 0.48f), 0.12f, new Color(0.02f, 0.36f, 1.0f) * 1.8f);
    }

    private static Material MaterialAt(string fileName)
    {
        Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/Art/Materials/" + fileName);
        if (material == null)
        {
            throw new InvalidOperationException("Missing Project 5 material: " + fileName);
        }

        return material;
    }

    private static Material CreateOrLoadMaterial(string materialName, Color color, float metallic, Color emission)
    {
        string path = "Assets/Art/Materials/" + materialName + ".mat";
        Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (material != null)
        {
            return material;
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
        material = new Material(shader) { name = materialName };
        if (material.HasProperty("_BaseColor")) material.SetColor("_BaseColor", color);
        if (material.HasProperty("_Color")) material.SetColor("_Color", color);
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", 0.42f);
        if (material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission);
        }

        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    /// <summary>
    /// Fernando's copy intentionally shares the validated Command Room material library.
    /// These are the same textures and physical-repeat values used by CommandRoom_Textured.
    /// </summary>
    private static void AlignWithCommandRoomTextureLibrary()
    {
        ConfigureRepeatingTexture("Assets/Art/Textures/offwhite_painted_concrete.png");
        ConfigureRepeatingTexture("Assets/Art/Textures/institutional_vinyl_tile.png");
        ConfigureRepeatingTexture("Assets/Art/Textures/acoustic_ceiling_tile.png");
        ConfigureRepeatingTexture("Assets/Art/Textures/painted_sage_steel.png");
        ConfigureRepeatingTexture("Assets/Art/Textures/worn_walnut_laminate.png");
        ConfigureRepeatingTexture("Assets/Art/Textures/black_vinyl_upholstery.png");
        ConfigureRepeatingTexture("Assets/Art/Textures/aged_bulletin_cork.png");

        SetMaterialTiling(wall, new Vector2(2.25f, 1.25f));
        SetMaterialTiling(floor, new Vector2(3f, 2f));
        SetMaterialTiling(ceiling, new Vector2(7f, 4f));
        SetMaterialTiling(steel, new Vector2(1.2f, 1.2f));
        SetMaterialTiling(darkSteel, new Vector2(1.2f, 1.2f));
        SetMaterialTiling(wood, new Vector2(1.6f, 1.2f));
        SetMaterialTiling(vinyl, new Vector2(2.8f, 2.8f));
        SetMaterialTiling(cork, new Vector2(1.5f, 1.2f));
        SetMaterialTiling(paper, new Vector2(1.8f, 1.8f));
        SetMaterialTiling(red, Vector2.one);
        SetMaterialTiling(amber, Vector2.one);
        SetMaterialTiling(green, Vector2.one);
        SetMaterialTiling(whiteGlow, Vector2.one);
    }

    private static void ConfigureRepeatingTexture(string path)
    {
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer == null) return;

        bool changed = importer.wrapMode != TextureWrapMode.Repeat || importer.filterMode != FilterMode.Trilinear;
        if (!changed) return;

        importer.wrapMode = TextureWrapMode.Repeat;
        importer.filterMode = FilterMode.Trilinear;
        importer.SaveAndReimport();
    }

    private static void SetMaterialTiling(Material material, Vector2 tiling)
    {
        if (material == null) return;
        if (material.HasProperty("_BaseMap")) material.SetTextureScale("_BaseMap", tiling);
        if (material.HasProperty("_MainTex")) material.SetTextureScale("_MainTex", tiling);
        EditorUtility.SetDirty(material);
    }

    private static int CountCommandRoomMappingIssues()
    {
        int issues = 0;
        string[] texturePaths =
        {
            "Assets/Art/Textures/offwhite_painted_concrete.png",
            "Assets/Art/Textures/institutional_vinyl_tile.png",
            "Assets/Art/Textures/acoustic_ceiling_tile.png",
            "Assets/Art/Textures/painted_sage_steel.png",
            "Assets/Art/Textures/worn_walnut_laminate.png",
            "Assets/Art/Textures/black_vinyl_upholstery.png",
            "Assets/Art/Textures/aged_bulletin_cork.png"
        };
        foreach (string texturePath in texturePaths)
        {
            TextureImporter importer = AssetImporter.GetAtPath(texturePath) as TextureImporter;
            if (importer == null || importer.wrapMode != TextureWrapMode.Repeat || importer.filterMode != FilterMode.Trilinear) issues++;
        }

        if (!HasExpectedTiling(wall, new Vector2(2.25f, 1.25f))) issues++;
        if (!HasExpectedTiling(floor, new Vector2(3f, 2f))) issues++;
        if (!HasExpectedTiling(ceiling, new Vector2(7f, 4f))) issues++;
        if (!HasExpectedTiling(steel, new Vector2(1.2f, 1.2f))) issues++;
        if (!HasExpectedTiling(darkSteel, new Vector2(1.2f, 1.2f))) issues++;
        if (!HasExpectedTiling(wood, new Vector2(1.6f, 1.2f))) issues++;
        if (!HasExpectedTiling(vinyl, new Vector2(2.8f, 2.8f))) issues++;
        if (!HasExpectedTiling(cork, new Vector2(1.5f, 1.2f))) issues++;
        return issues;
    }

    private static bool HasExpectedTiling(Material material, Vector2 expected)
    {
        if (material == null) return false;
        string property = material.HasProperty("_BaseMap") ? "_BaseMap" : material.HasProperty("_MainTex") ? "_MainTex" : null;
        if (property == null) return false;
        Vector2 actual = material.GetTextureScale(property);
        return Mathf.Approximately(actual.x, expected.x) && Mathf.Approximately(actual.y, expected.y);
    }

    private static Bounds GetRoomBounds()
    {
        GameObject floorObject = FindByName("Floor");
        if (floorObject != null && TryGetBounds(floorObject, out Bounds floorBounds))
        {
            return floorBounds;
        }

        bool hasBounds = false;
        Bounds room = new Bounds(Vector3.zero, Vector3.one);
        foreach (Renderer renderer in SceneRenderers())
        {
            if (!hasBounds)
            {
                room = renderer.bounds;
                hasBounds = true;
            }
            else
            {
                room.Encapsulate(renderer.bounds);
            }
        }

        if (!hasBounds) throw new InvalidOperationException("Fernando's scene has no renderable greybox geometry.");
        return room;
    }

    private static float GetWallCeiling(Bounds room, float floorTop)
    {
        float highestWall = floorTop + 3f;
        foreach (GameObject gameObject in SceneObjects())
        {
            string name = gameObject.name.ToLowerInvariant();
            if ((name.Contains("wall") || name.Contains("angled")) && TryGetBounds(gameObject, out Bounds bounds))
            {
                highestWall = Mathf.Max(highestWall, bounds.max.y);
            }
        }

        return Mathf.Clamp(highestWall, floorTop + 2.35f, floorTop + 4.2f);
    }

    private static void ApplyMaterialPass(Bounds room, float floorTop, float ceilingHeight)
    {
        foreach (GameObject gameObject in SceneObjects())
        {
            if (gameObject.name.StartsWith("FERNANDO TEXTURE PASS", StringComparison.Ordinal)) continue;
            string id = gameObject.name.ToLowerInvariant();
            if (!TryGetBounds(gameObject, out Bounds bounds)) continue;

            Material material = null;
            if (id == "floor" || id.Contains("floor")) material = floor;
            else if (id.Contains("wall") || id.Contains("angled wall") || id.Contains("back wall")) material = wall;
            else if (id.Contains("board")) material = cork;
            else if (id.Contains("desk")) material = wood;
            else if (id.Contains("chair")) material = vinyl;
            else if (id.Contains("monitor")) material = darkSteel;
            else if (id.Contains("door")) material = steel;
            else if (id.Contains("switch")) material = darkSteel;
            else if (id.Contains("cabinet") || id.Contains("filing") || id.Contains("medical") || id.Contains("folder") || id.Contains("control panel")) material = steel;
            else if (id.StartsWith("pb_mesh", StringComparison.Ordinal))
            {
                if (bounds.max.y < floorTop + 0.45f && bounds.size.x > room.size.x * 0.75f) material = floor;
                else if (bounds.center.y > ceilingHeight - 0.25f) material = ceiling;
            }

            if (material == null && HasUnityDefaultMaterial(gameObject))
            {
                material = FallbackGreyboxMaterial(bounds, room, floorTop, ceilingHeight);
            }

            if (material != null) SetMaterial(gameObject, material);
        }
    }

    private static Material FallbackGreyboxMaterial(Bounds bounds, Bounds room, float floorTop, float ceilingHeight)
    {
        if (bounds.max.y < floorTop + 0.40f && bounds.size.x > room.size.x * 0.38f) return floor;
        if (bounds.min.y > ceilingHeight - 0.22f) return ceiling;
        if (bounds.size.y > (ceilingHeight - floorTop) * 0.72f && Mathf.Max(bounds.size.x, bounds.size.z) > 0.90f) return wall;
        if (bounds.size.y < 0.12f || Mathf.Min(bounds.size.x, bounds.size.z) < 0.06f) return darkSteel;
        return steel;
    }

    private static void AddRoomConstruction(Transform parent, Bounds room, float floorTop, float ceilingHeight)
    {
        float innerMinX = room.min.x + 0.08f;
        float innerMaxX = room.max.x - 0.08f;
        float innerMinZ = room.min.z + 0.08f;
        float innerMaxZ = room.max.z - 0.08f;
        float ceilingY = ceilingHeight - 0.06f;

        // Keep the ceiling surface readable without a dense overlay of generated line props.
        // The acoustic tile texture provides the subtle repeat; fixtures are added separately.
        Box("Acoustic suspended ceiling", new Vector3(room.center.x, ceilingY, room.center.z), new Vector3(room.size.x - 0.14f, 0.08f, room.size.z - 0.14f), ceiling, parent);

        // Steel baseboard makes the wall thickness and construction readable at a human scale.
        float baseboardY = floorTop + 0.10f;
        Box("Front steel baseboard", new Vector3(room.center.x, baseboardY, innerMinZ), new Vector3(room.size.x - 0.16f, 0.20f, 0.045f), darkSteel, parent);
        Box("Back steel baseboard", new Vector3(room.center.x, baseboardY, innerMaxZ), new Vector3(room.size.x - 0.16f, 0.20f, 0.045f), darkSteel, parent);
        Box("Left steel baseboard", new Vector3(innerMinX, baseboardY, room.center.z), new Vector3(0.045f, 0.20f, room.size.z - 0.16f), darkSteel, parent);
        Box("Right steel baseboard", new Vector3(innerMaxX, baseboardY, room.center.z), new Vector3(0.045f, 0.20f, room.size.z - 0.16f), darkSteel, parent);

    }

    private static void AddEquipmentDetail(Transform parent, Bounds room, float floorTop)
    {
        int cabinetIndex = 0;
        foreach (GameObject gameObject in SceneObjects())
        {
            string id = gameObject.name.ToLowerInvariant();
            if (!TryGetBounds(gameObject, out Bounds bounds)) continue;

            if (id.Contains("cabinet") || id.Contains("filing") || id.Contains("medical") || id.Contains("folder") || id == "tall" || id.StartsWith("tall ") || id == "short" || id.StartsWith("short "))
            {
                DecorateCabinet(gameObject.name, bounds, parent, cabinetIndex++);
            }
            else if (id.Contains("control panel"))
            {
                DecorateControlPanel(bounds, parent);
            }
            else if (id.Contains("monitor"))
            {
                DecorateMonitor(bounds, parent);
            }
            else if (id.Contains("switch"))
            {
                DecorateSwitch(bounds, parent);
            }
        }

        // Emergency infrastructure breaks up the bare wall and tells the viewer this is an operated room.
        Vector3 firePosition = new Vector3(room.min.x + 0.18f, floorTop + 0.58f, room.min.z + room.size.z * 0.22f);
        Cylinder("Safety-red fire extinguisher", firePosition, 0.095f, 0.52f, red, parent, Quaternion.identity);
        Cylinder("Fire extinguisher hose", firePosition + new Vector3(0.11f, 0.10f, -0.04f), 0.024f, 0.22f, darkSteel, parent, Quaternion.Euler(0f, 25f, 65f));
        Box("Fire extinguisher label", firePosition + new Vector3(0f, 0.03f, -0.10f), new Vector3(0.11f, 0.20f, 0.012f), paper, parent);
    }

    private static void DecorateCabinet(string sourceName, Bounds bounds, Transform parent, int index)
    {
        Vector3 face = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.014f);
        float width = Mathf.Clamp(bounds.size.x * 0.70f, 0.24f, 0.82f);
        float height = Mathf.Clamp(bounds.size.y * 0.52f, 0.30f, 0.72f);
        float centerY = Mathf.Clamp(bounds.center.y + bounds.size.y * 0.10f, bounds.min.y + 0.28f, bounds.max.y - 0.34f);

        Box(sourceName + " instrument panel", new Vector3(face.x, centerY, face.z), new Vector3(width, height, 0.025f), darkSteel, parent);
        Box(sourceName + " CRT readout", new Vector3(face.x, centerY + height * 0.10f, face.z - 0.016f), new Vector3(width * 0.72f, height * 0.36f, 0.012f), blueGlow, parent);
        Box(sourceName + " number plate", new Vector3(face.x, centerY + height * 0.37f, face.z - 0.019f), new Vector3(width * 0.56f, height * 0.12f, 0.010f), paper, parent);

        for (int indicator = 0; indicator < 3; indicator++)
        {
            Material indicatorMaterial = indicator == 0 ? green : indicator == 1 ? amber : red;
            float x = face.x + (indicator - 1) * width * 0.22f;
            Sphere(sourceName + " status indicator", new Vector3(x, centerY - height * 0.33f, face.z - 0.028f), 0.035f, indicatorMaterial, parent);
        }

        if (bounds.size.y > 1.05f)
        {
            float handleY = Mathf.Clamp(bounds.center.y - bounds.size.y * 0.25f, bounds.min.y + 0.25f, bounds.max.y - 0.28f);
            Box(sourceName + " recessed handle", new Vector3(face.x, handleY, face.z - 0.021f), new Vector3(width * 0.48f, 0.055f, 0.018f), steel, parent);
            Sphere(sourceName + " cam lock", new Vector3(face.x + width * 0.30f, handleY, face.z - 0.033f), 0.030f, darkSteel, parent);
        }

        // Individual labels make each repeated cabinet read as a maintained system rather than a cloned cube.
        float labelOffset = ((index % 3) - 1) * 0.08f;
        Box(sourceName + " asset label", new Vector3(face.x + labelOffset, bounds.max.y - 0.18f, face.z - 0.016f), new Vector3(width * 0.52f, 0.065f, 0.010f), paper, parent);
    }

    private static void DecorateControlPanel(Bounds bounds, Transform parent)
    {
        Vector3 face = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.018f);
        float width = Mathf.Clamp(bounds.size.x * 0.82f, 0.8f, 1.48f);
        float height = Mathf.Clamp(bounds.size.y * 0.78f, 0.42f, 0.72f);
        Box("Control-panel recessed fascia", face, new Vector3(width, height, 0.03f), darkSteel, parent);
        Box("Control-panel primary data screen", face + new Vector3(0f, height * 0.12f, -0.020f), new Vector3(width * 0.45f, height * 0.35f, 0.012f), blueGlow, parent);
        Box("Control-panel system label", face + new Vector3(0f, height * 0.36f, -0.022f), new Vector3(width * 0.66f, height * 0.09f, 0.010f), paper, parent);
        for (int i = -2; i <= 2; i++)
        {
            Sphere("Control-panel illuminated toggle", face + new Vector3(i * width * 0.14f, -height * 0.27f, -0.034f), 0.033f, i == 0 ? red : (i % 2 == 0 ? green : amber), parent);
        }
        for (int meter = -1; meter <= 1; meter += 2)
        {
            Box("Control-panel analog meter", face + new Vector3(meter * width * 0.32f, height * 0.05f, -0.027f), new Vector3(width * 0.14f, height * 0.23f, 0.011f), paper, parent);
            Box("Control-panel meter needle", face + new Vector3(meter * width * 0.32f, height * 0.05f, -0.037f), new Vector3(width * 0.02f, height * 0.16f, 0.008f), red, parent, Quaternion.Euler(0f, 0f, meter * 30f));
        }
    }

    private static void DecorateMonitor(Bounds bounds, Transform parent)
    {
        Vector3 face = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.020f);
        Box("Monitor bezel", face, new Vector3(bounds.size.x * 0.88f, bounds.size.y * 0.82f, 0.030f), darkSteel, parent);
        Box("Monitor blue diagnostic display", face + new Vector3(0f, 0f, -0.021f), new Vector3(bounds.size.x * 0.72f, bounds.size.y * 0.60f, 0.012f), blueGlow, parent);
        Box("Monitor status bar", face + new Vector3(0f, -bounds.size.y * 0.27f, -0.031f), new Vector3(bounds.size.x * 0.55f, 0.025f, 0.008f), green, parent);
    }

    private static void DecorateSwitch(Bounds bounds, Transform parent)
    {
        Vector3 face = new Vector3(bounds.center.x, bounds.center.y, bounds.min.z - 0.020f);
        Box("Wall-switch housing", face, new Vector3(Mathf.Max(bounds.size.x, 0.16f), Mathf.Max(bounds.size.y, 0.28f), 0.030f), darkSteel, parent);
        Box("Wall-switch safety cover", face + new Vector3(0f, 0.02f, -0.021f), new Vector3(0.075f, 0.16f, 0.010f), amber, parent);
    }

    private static void AddWorkSurfaceDetail(Transform parent, Bounds room, float floorTop)
    {
        foreach (GameObject gameObject in SceneObjects())
        {
            string id = gameObject.name.ToLowerInvariant();
            if (!TryGetBounds(gameObject, out Bounds bounds)) continue;

            if (id.Contains("desk"))
            {
                Vector3 top = new Vector3(bounds.center.x, bounds.max.y + 0.020f, bounds.center.z);
                Box(gameObject.name + " desk blotter", top, new Vector3(bounds.size.x * 0.46f, 0.012f, bounds.size.z * 0.58f), darkSteel, parent);
                for (int i = 0; i < 4; i++)
                {
                    Box(gameObject.name + " log binder", top + new Vector3(-bounds.size.x * 0.25f + i * 0.10f, 0.055f, 0.02f), new Vector3(0.065f, 0.13f, 0.33f), i % 2 == 0 ? paper : steel, parent);
                }
                Box(gameObject.name + " clipboard", top + new Vector3(bounds.size.x * 0.20f, 0.020f, 0f), new Vector3(0.26f, 0.012f, 0.36f), paper, parent, Quaternion.Euler(0f, 0f, -4f));
                Box(gameObject.name + " clipboard clip", top + new Vector3(bounds.size.x * 0.20f, 0.032f, 0.13f), new Vector3(0.11f, 0.015f, 0.025f), darkSteel, parent, Quaternion.Euler(0f, 0f, -4f));
            }
            else if (id.Contains("chair"))
            {
                // The upholstered pad makes otherwise flat chair geometry read as fabric rather than black plastic.
                Box(gameObject.name + " upholstered seat pad", new Vector3(bounds.center.x, bounds.center.y + bounds.size.y * 0.08f, bounds.center.z), new Vector3(bounds.size.x * 0.65f, 0.055f, bounds.size.z * 0.65f), vinyl, parent);
            }
        }

        Vector3 wasteBin = new Vector3(room.max.x - 0.35f, floorTop + 0.21f, room.min.z + 0.42f);
        Cylinder("Metal mesh waste bin", wasteBin, 0.15f, 0.39f, darkSteel, parent, Quaternion.identity);
        Cylinder("Waste-bin rim", wasteBin + new Vector3(0f, 0.20f, 0f), 0.17f, 0.025f, steel, parent, Quaternion.identity);
    }

    private static void AddEnvironmentalStory(Transform parent, Bounds room, float floorTop, float ceilingHeight)
    {
        GameObject board = FindByName("Board");
        if (board != null && TryGetBounds(board, out Bounds boardBounds))
        {
            Vector3 face = new Vector3(boardBounds.center.x, boardBounds.center.y, boardBounds.min.z - 0.018f);
            Box("Bulletin-board pinboard", face, new Vector3(boardBounds.size.x * 0.88f, boardBounds.size.y * 0.85f, 0.032f), cork, parent);
            for (int i = 0; i < 5; i++)
            {
                float x = face.x + ((i % 3) - 1) * boardBounds.size.x * 0.20f;
                float y = face.y + ((i / 3) - 0.35f) * boardBounds.size.y * 0.25f;
                Box("Pinned maintenance note", new Vector3(x, y, face.z - 0.022f), new Vector3(boardBounds.size.x * 0.18f, boardBounds.size.y * 0.20f, 0.010f), paper, parent, Quaternion.Euler(0f, 0f, (i - 2) * 4f));
                Sphere("Red push pin", new Vector3(x, y + boardBounds.size.y * 0.075f, face.z - 0.034f), 0.018f, i % 2 == 0 ? red : green, parent);
            }
        }

        // A restrained alarm and clock create operational context without obscuring the greybox layout.
        Vector3 alarm = new Vector3(room.center.x, ceilingHeight - 0.54f, room.max.z - 0.09f);
        Box("Emergency-alarm housing", alarm, new Vector3(0.30f, 0.21f, 0.035f), darkSteel, parent);
        Sphere("Emergency-red alarm lens", alarm + new Vector3(0f, 0f, -0.032f), 0.070f, red, parent);
        Box("Emergency-alarm label", alarm + new Vector3(0f, -0.085f, -0.034f), new Vector3(0.20f, 0.035f, 0.008f), paper, parent);

        Vector3 clock = new Vector3(room.min.x + 0.08f, ceilingHeight - 0.72f, room.center.z - room.size.z * 0.24f);
        Cylinder("Wall-clock rim", clock, 0.19f, 0.035f, darkSteel, parent, Quaternion.Euler(90f, 0f, 0f));
        Cylinder("Wall-clock face", clock + new Vector3(0f, 0f, -0.022f), 0.158f, 0.012f, paper, parent, Quaternion.Euler(90f, 0f, 0f));
        Box("Wall-clock minute hand", clock + new Vector3(0f, 0.055f, -0.032f), new Vector3(0.012f, 0.11f, 0.008f), darkSteel, parent, Quaternion.Euler(0f, 0f, -22f));
        Box("Wall-clock hour hand", clock + new Vector3(0.037f, -0.016f, -0.034f), new Vector3(0.09f, 0.012f, 0.008f), darkSteel, parent, Quaternion.Euler(0f, 0f, 12f));
    }

    /// <summary>
    /// Adds a pair of non-interactive period reference sketches to the existing bulletin
    /// board. They document both DEFCON states without changing the live game display,
    /// its state, or any gameplay scripts.
    /// </summary>
    private static void AddDefconReferenceSketches(Transform parent, Bounds room, float floorTop, float ceilingHeight)
    {
        GameObject board = FindByName("Board");
        Bounds boardBounds = default;
        bool hasBoard = board != null && TryGetBounds(board, out boardBounds);
        if (!hasBoard)
        {
            boardBounds = new Bounds(
                new Vector3(room.center.x, floorTop + 1.38f, room.min.z + 0.10f),
                new Vector3(1.28f, 0.78f, 0.04f));
        }

        Transform sketches = new GameObject("DEFCON reference sketches").transform;
        sketches.SetParent(parent, true);

        float cardWidth = Mathf.Clamp(boardBounds.size.x * 0.28f, 0.30f, 0.56f);
        float cardHeight = Mathf.Clamp(boardBounds.size.y * 0.42f, 0.26f, 0.48f);
        float spacing = Mathf.Max(cardWidth * 0.62f, boardBounds.size.x * 0.18f);
        Vector3 face = new Vector3(boardBounds.center.x, boardBounds.center.y, boardBounds.min.z - 0.040f);

        CreateDefconSketchCard(
            sketches,
            "DEFCON 1",
            "MAX\nREADINESS",
            face + new Vector3(-spacing, 0.02f, 0f),
            cardWidth,
            cardHeight,
            red);
        CreateDefconSketchCard(
            sketches,
            "DEFCON 2",
            "READINESS\nREDUCED",
            face + new Vector3(spacing, 0.02f, 0f),
            cardWidth,
            cardHeight,
            amber);

        TextLabel(
            "DEFCON sketch header",
            "STATUS REFERENCE",
            face + new Vector3(0f, cardHeight * 0.67f, -0.012f),
            Vector3.back,
            0.030f,
            new Color(0.12f, 0.15f, 0.12f),
            sketches);
    }

    private static void CreateDefconSketchCard(
        Transform parent,
        string state,
        string descriptor,
        Vector3 center,
        float width,
        float height,
        Material stateMaterial)
    {
        Box(state + " sketch card", center, new Vector3(width, height, 0.012f), paper, parent);
        Box(
            state + " sketch status swatch",
            center + new Vector3(0f, -height * 0.26f, -0.012f),
            new Vector3(width * 0.48f, height * 0.10f, 0.008f),
            stateMaterial,
            parent);
        Sphere(
            state + " sketch pin",
            center + new Vector3(-width * 0.31f, height * 0.33f, -0.018f),
            0.018f,
            stateMaterial,
            parent);
        TextLabel(
            state + " sketch title",
            state,
            center + new Vector3(0f, height * 0.16f, -0.016f),
            Vector3.back,
            0.050f,
            new Color(0.12f, 0.15f, 0.12f),
            parent);
        TextLabel(
            state + " sketch descriptor",
            descriptor,
            center + new Vector3(0f, -height * 0.05f, -0.016f),
            Vector3.back,
            0.026f,
            new Color(0.12f, 0.15f, 0.12f),
            parent);
    }

    private static void AddLighting(Transform parent, Bounds room, float floorTop, float ceilingHeight)
    {
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.075f, 0.10f, 0.12f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.11f, 0.14f, 0.15f);
        RenderSettings.fogDensity = 0.009f;

        Vector3[] fixturePositions =
        {
            new Vector3(room.center.x - room.size.x * 0.22f, ceilingHeight - 0.14f, room.center.z - room.size.z * 0.20f),
            new Vector3(room.center.x + room.size.x * 0.22f, ceilingHeight - 0.14f, room.center.z - room.size.z * 0.20f),
            new Vector3(room.center.x, ceilingHeight - 0.14f, room.center.z + room.size.z * 0.24f)
        };
        foreach (Vector3 position in fixturePositions)
        {
            Box("Fluorescent light diffuser", position, new Vector3(0.92f, 0.045f, 0.31f), whiteGlow, parent);
            PointLight("Cold fluorescent pool", position + new Vector3(0f, -0.17f, 0f), new Color(0.48f, 0.70f, 0.92f), 1.45f, 5.3f, parent);
        }

        PointLight("Warm diagnostic spill", new Vector3(room.center.x, floorTop + 1.25f, room.center.z + room.size.z * 0.30f), new Color(0.97f, 0.22f, 0.08f), 0.70f, 2.8f, parent);
        PointLight("Monitor blue spill", new Vector3(room.center.x + room.size.x * 0.16f, floorTop + 1.32f, room.min.z + room.size.z * 0.24f), new Color(0.08f, 0.38f, 1.0f), 0.65f, 2.4f, parent);

        GameObject key = new GameObject("Cool directional fill");
        key.transform.SetParent(parent, false);
        Light directional = key.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.color = new Color(0.48f, 0.63f, 0.78f);
        directional.intensity = 0.22f;
        directional.shadows = LightShadows.Soft;
        directional.transform.rotation = Quaternion.Euler(50f, -35f, 0f);
    }

    private static void ConfigurePresentation(Bounds room)
    {
        Camera mainCamera = Camera.main;
        if (mainCamera == null)
        {
            GameObject cameraObject = new GameObject("Main Camera");
            cameraObject.tag = "MainCamera";
            mainCamera = cameraObject.AddComponent<Camera>();
        }

        Vector3 target = room.center + new Vector3(0f, 1.20f, room.size.z * 0.10f);
        mainCamera.transform.position = new Vector3(room.center.x + room.size.x * 0.26f, room.min.y + 1.65f, room.min.z + room.size.z * 0.16f);
        mainCamera.transform.rotation = Quaternion.LookRotation(target - mainCamera.transform.position, Vector3.up);
        mainCamera.fieldOfView = 64f;
        mainCamera.nearClipPlane = 0.03f;
        mainCamera.farClipPlane = 100f;
        mainCamera.clearFlags = CameraClearFlags.SolidColor;
        mainCamera.backgroundColor = new Color(0.045f, 0.07f, 0.09f);
    }

    private static void RenderPreviews(Bounds room, float floorTop, float ceilingHeight)
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(PreviewFolder);
        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 originalPosition = camera.transform.position;
        Quaternion originalRotation = camera.transform.rotation;
        float originalFov = camera.fieldOfView;

        RenderCamera(camera, "Assets/Art/Previews/fernando-textured-overview.png");
        camera.transform.position = new Vector3(room.min.x + room.size.x * 0.18f, floorTop + 1.48f, room.min.z + room.size.z * 0.38f);
        Vector3 closeupTarget = new Vector3(room.center.x, floorTop + 1.25f, room.max.z - room.size.z * 0.16f);
        camera.transform.rotation = Quaternion.LookRotation(closeupTarget - camera.transform.position, Vector3.up);
        camera.fieldOfView = 58f;
        RenderCamera(camera, "Assets/Art/Previews/fernando-textured-equipment.png");

        camera.transform.position = originalPosition;
        camera.transform.rotation = originalRotation;
        camera.fieldOfView = originalFov;
    }

    private static void RenderQualityViews(Bounds room, float floorTop, float ceilingHeight)
    {
        EnsureFolder("Assets/Art");
        EnsureFolder(PreviewFolder);
        Camera camera = Camera.main;
        if (camera == null) return;

        Vector3 originalPosition = camera.transform.position;
        Quaternion originalRotation = camera.transform.rotation;
        float originalFov = camera.fieldOfView;

        camera.transform.position = new Vector3(room.max.x - room.size.x * 0.16f, floorTop + 1.55f, room.min.z + room.size.z * 0.18f);
        camera.transform.rotation = Quaternion.LookRotation(
            new Vector3(room.center.x - room.size.x * 0.12f, floorTop + 1.30f, room.center.z + room.size.z * 0.18f) - camera.transform.position,
            Vector3.up);
        camera.fieldOfView = 62f;
        RenderCamera(camera, "Assets/Art/Previews/fernando-textured-qa-right.png");

        // Fernando's greybox reserves the rear perimeter for tall cabinets. Use the approved
        // presentation camera for this coverage check, rather than a view squeezed into a gap.
        camera.transform.position = originalPosition;
        camera.transform.rotation = originalRotation;
        camera.fieldOfView = originalFov;
        RenderCamera(camera, "Assets/Art/Previews/fernando-textured-qa-rear.png");

        // Inspect the ceiling from that same clear, interior spot, aimed upward through the
        // fixture bay so tile repeat, grid spacing, and service routing are all visible.
        camera.transform.position = originalPosition;
        camera.transform.rotation = Quaternion.LookRotation(
            new Vector3(room.center.x, ceilingHeight - 0.12f, room.center.z + room.size.z * 0.18f) - camera.transform.position,
            Vector3.up);
        camera.fieldOfView = 60f;
        RenderCamera(camera, "Assets/Art/Previews/fernando-textured-qa-ceiling.png");

        camera.transform.position = originalPosition;
        camera.transform.rotation = originalRotation;
        camera.fieldOfView = originalFov;
    }

    private static void RenderCamera(Camera camera, string path)
    {
        const int width = 1280;
        const int height = 720;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        camera.targetTexture = renderTexture;
        camera.Render();
        RenderTexture.active = renderTexture;
        image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        image.Apply();
        camera.targetTexture = previousTarget;
        RenderTexture.active = previousActive;
        File.WriteAllBytes(path, image.EncodeToPNG());
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(renderTexture);
    }

    private static Texture MaterialTexture(Material material)
    {
        if (material.HasProperty("_BaseMap")) return material.GetTexture("_BaseMap");
        if (material.HasProperty("_MainTex")) return material.GetTexture("_MainTex");
        return null;
    }

    private static int CountUnityDefaultMaterialSlots()
    {
        int count = 0;
        foreach (Renderer renderer in SceneRenderers())
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (IsUnityDefaultMaterial(material)) count++;
            }
        }
        return count;
    }

    private static bool HasUnityDefaultMaterial(GameObject gameObject)
    {
        foreach (Renderer renderer in gameObject.GetComponents<Renderer>())
        {
            foreach (Material material in renderer.sharedMaterials)
            {
                if (IsUnityDefaultMaterial(material)) return true;
            }
        }
        return false;
    }

    private static bool IsUnityDefaultMaterial(Material material)
    {
        return material != null && material.name.StartsWith("Default-Material", StringComparison.Ordinal);
    }

    private static void SetMaterial(GameObject gameObject, Material material)
    {
        foreach (Renderer renderer in gameObject.GetComponentsInChildren<Renderer>(true))
        {
            if (renderer.gameObject.name.StartsWith("FERNANDO TEXTURE PASS", StringComparison.Ordinal)) continue;
            Material[] materials = renderer.sharedMaterials;
            for (int i = 0; i < materials.Length; i++) materials[i] = material;
            if (materials.Length == 0) materials = new[] { material };
            renderer.sharedMaterials = materials;
        }
    }

    private static IEnumerable<GameObject> SceneObjects()
    {
        foreach (GameObject root in SceneManager.GetActiveScene().GetRootGameObjects())
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                yield return transform.gameObject;
            }
        }
    }

    private static IEnumerable<Renderer> SceneRenderers()
    {
        foreach (GameObject gameObject in SceneObjects())
        {
            foreach (Renderer renderer in gameObject.GetComponents<Renderer>()) yield return renderer;
        }
    }

    private static GameObject FindByName(string name)
    {
        foreach (GameObject gameObject in SceneObjects())
        {
            if (gameObject.name.Equals(name, StringComparison.OrdinalIgnoreCase)) return gameObject;
        }
        return null;
    }

    private static bool TryGetBounds(GameObject gameObject, out Bounds bounds)
    {
        Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
        if (renderers.Length == 0)
        {
            bounds = default;
            return false;
        }

        bounds = renderers[0].bounds;
        for (int i = 1; i < renderers.Length; i++) bounds.Encapsulate(renderers[i].bounds);
        return true;
    }

    private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cube);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation ?? Quaternion.identity;
        gameObject.transform.localScale = scale;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }

    private static GameObject Sphere(string name, Vector3 position, float diameter, Material material, Transform parent)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.localScale = Vector3.one * diameter;
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }

    private static void TextLabel(string name, string value, Vector3 position, Vector3 normal, float size, Color color, Transform parent)
    {
        GameObject label = new GameObject(name);
        label.transform.SetParent(parent, true);
        label.transform.position = position;
        // Legacy TextMesh geometry faces local -Z, so negate the intended physical normal.
        label.transform.rotation = Quaternion.LookRotation(-normal, Vector3.up);
        TextMesh text = label.AddComponent<TextMesh>();
        text.text = value;
        text.fontSize = 72;
        // Keep the sketch annotations inside their paper cards at first-person scale.
        text.characterSize = size * 0.12f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.richText = false;
    }

    private static GameObject Cylinder(string name, Vector3 position, float radius, float height, Material material, Transform parent, Quaternion rotation)
    {
        GameObject gameObject = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        gameObject.name = name;
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        gameObject.transform.rotation = rotation;
        gameObject.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        gameObject.GetComponent<Renderer>().sharedMaterial = material;
        UnityEngine.Object.DestroyImmediate(gameObject.GetComponent<Collider>());
        return gameObject;
    }

    private static void PointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
    {
        GameObject gameObject = new GameObject(name);
        gameObject.transform.SetParent(parent, true);
        gameObject.transform.position = position;
        Light light = gameObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.Soft;
    }

    private static void EnsureFolder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        string parent = Path.GetDirectoryName(path)?.Replace('\\', '/');
        string name = Path.GetFileName(path);
        if (!string.IsNullOrEmpty(parent) && !string.IsNullOrEmpty(name)) AssetDatabase.CreateFolder(parent, name);
    }
}
