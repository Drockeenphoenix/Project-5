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
            "QA complete: {0} renderers inspected, {1} missing material slots, {2} Unity default-material slots, {3}/{4} textured slots repeat correctly, {5} use clamped texture addressing.",
            rendererCount,
            missingMaterialSlots,
            defaultMaterialSlots,
            repeatingTextureSlots,
            texturedSlots,
            clampedTextureSlots);
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

        // A real service-room ceiling needs acoustic tile seams, T-bars, fixtures, and ventilation.
        Box("Acoustic suspended ceiling", new Vector3(room.center.x, ceilingY, room.center.z), new Vector3(room.size.x - 0.14f, 0.08f, room.size.z - 0.14f), ceiling, parent);
        for (float x = innerMinX; x <= innerMaxX; x += 0.60f)
        {
            Box("Ceiling T-bar — cross room", new Vector3(x, ceilingY - 0.055f, room.center.z), new Vector3(0.027f, 0.030f, room.size.z - 0.18f), darkSteel, parent);
        }
        for (float z = innerMinZ; z <= innerMaxZ; z += 0.60f)
        {
            Box("Ceiling T-bar — along room", new Vector3(room.center.x, ceilingY - 0.058f, z), new Vector3(room.size.x - 0.18f, 0.030f, 0.027f), darkSteel, parent);
        }

        // Steel baseboard makes the wall thickness and construction readable at a human scale.
        float baseboardY = floorTop + 0.10f;
        Box("Front steel baseboard", new Vector3(room.center.x, baseboardY, innerMinZ), new Vector3(room.size.x - 0.16f, 0.20f, 0.045f), darkSteel, parent);
        Box("Back steel baseboard", new Vector3(room.center.x, baseboardY, innerMaxZ), new Vector3(room.size.x - 0.16f, 0.20f, 0.045f), darkSteel, parent);
        Box("Left steel baseboard", new Vector3(innerMinX, baseboardY, room.center.z), new Vector3(0.045f, 0.20f, room.size.z - 0.16f), darkSteel, parent);
        Box("Right steel baseboard", new Vector3(innerMaxX, baseboardY, room.center.z), new Vector3(0.045f, 0.20f, room.size.z - 0.16f), darkSteel, parent);

        // Vent, cable tray, and exposed pipework make the neutral room feel maintained and functional.
        Vector3 vent = new Vector3(room.center.x - room.size.x * 0.22f, ceilingY - 0.07f, room.center.z + room.size.z * 0.18f);
        Box("Ceiling vent frame", vent, new Vector3(0.72f, 0.03f, 0.72f), darkSteel, parent);
        for (int i = -3; i <= 3; i++)
        {
            Box("Ceiling vent slat", vent + new Vector3(i * 0.085f, -0.028f, 0f), new Vector3(0.020f, 0.018f, 0.58f), steel, parent);
        }

        float pipeX = innerMinX + 0.13f;
        Cylinder("Left cooling pipe", new Vector3(pipeX, ceilingY - 0.36f, room.center.z), 0.065f, room.size.z * 0.72f, steel, parent, Quaternion.Euler(90f, 0f, 0f));
        for (float z = room.center.z - room.size.z * 0.28f; z < room.center.z + room.size.z * 0.31f; z += 1.05f)
        {
            Box("Cooling-pipe wall strap", new Vector3(pipeX - 0.030f, ceilingY - 0.36f, z), new Vector3(0.04f, 0.19f, 0.06f), darkSteel, parent);
        }

        Vector3 tray = new Vector3(room.center.x + room.size.x * 0.20f, ceilingY - 0.30f, innerMaxZ - 0.20f);
        Box("Cable tray front rail", tray + new Vector3(0f, 0f, -0.13f), new Vector3(room.size.x * 0.42f, 0.055f, 0.028f), darkSteel, parent);
        Box("Cable tray rear rail", tray + new Vector3(0f, 0f, 0.13f), new Vector3(room.size.x * 0.42f, 0.055f, 0.028f), darkSteel, parent);
        for (float x = tray.x - room.size.x * 0.19f; x < tray.x + room.size.x * 0.20f; x += 0.32f)
        {
            Box("Cable tray rung", new Vector3(x, tray.y, tray.z), new Vector3(0.025f, 0.025f, 0.31f), steel, parent);
        }
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
