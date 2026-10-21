using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;

/// <summary>
/// Builds a complete, material-led pass of the Project 5 control-room greybox.
/// It deliberately makes a separate scene so the original greybox remains available.
/// </summary>
public static class Project5CommandRoomBuilder
{
    private const string MaterialsFolder = "Assets/Art/Materials";
    private const string ScenePath = "Assets/Scenes/CommandRoom_Textured.unity";

    private static Material wall;
    private static Material floor;
    private static Material ceiling;
    private static Material ceilingGrid;
    private static Material vinyl;
    private static Material cork;
    private static Material sageSteel;
    private static Material darkSteel;
    private static Material wood;
    private static Material paper;
    private static Material safetyRed;
    private static Material red;
    private static Material amber;
    private static Material green;
    private static Material whiteGlow;

    [MenuItem("Project 5/Build Textured Command Room")]
    public static void Build()
    {
        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Materials");

        Texture2D wallTexture = PrepareTexture("Assets/Art/Textures/offwhite_painted_concrete.png");
        Texture2D floorTexture = PrepareTexture("Assets/Art/Textures/institutional_vinyl_tile.png");
        Texture2D ceilingTexture = PrepareTexture("Assets/Art/Textures/acoustic_ceiling_tile.png");
        Texture2D steelTexture = PrepareTexture("Assets/Art/Textures/painted_sage_steel.png");
        Texture2D woodTexture = PrepareTexture("Assets/Art/Textures/worn_walnut_laminate.png");
        Texture2D vinylTexture = PrepareTexture("Assets/Art/Textures/black_vinyl_upholstery.png");
        Texture2D corkTexture = PrepareTexture("Assets/Art/Textures/aged_bulletin_cork.png");

        wall = CreateMaterial("Wall Painted Concrete", wallTexture, Color.white, 0.0f, 0.16f, new Vector2(2.25f, 1.25f));
        floor = CreateMaterial("Floor Institutional Vinyl", floorTexture, Color.white, 0.0f, 0.32f, new Vector2(3f, 2f));
        ceiling = CreateMaterial("Ceiling Mineral Fiber", ceilingTexture, new Color(0.98f, 0.94f, 0.86f), 0.0f, 0.08f, new Vector2(7f, 4f));
        ceilingGrid = CreateMaterial("Ceiling Grid Painted Steel", steelTexture, new Color(0.46f, 0.51f, 0.47f), 0.48f, 0.22f, new Vector2(1.2f, 1.2f));
        vinyl = CreateMaterial("Chair Black Vinyl", vinylTexture, Color.white, 0.0f, 0.22f, new Vector2(2.8f, 2.8f));
        cork = CreateMaterial("Bulletin Board Cork", corkTexture, Color.white, 0.0f, 0.18f, new Vector2(1.5f, 1.2f));
        sageSteel = CreateMaterial("Cabinet Sage Painted Steel", steelTexture, new Color(0.86f, 0.96f, 0.88f), 0.55f, 0.28f, new Vector2(1.2f, 1.2f));
        darkSteel = CreateMaterial("Trim Dark Painted Steel", steelTexture, new Color(0.22f, 0.28f, 0.25f), 0.68f, 0.22f, new Vector2(1.2f, 1.2f));
        wood = CreateMaterial("Desk Worn Walnut Laminate", woodTexture, Color.white, 0.0f, 0.30f, new Vector2(1.6f, 1.2f));
        paper = CreateMaterial("Aged Paper", wallTexture, new Color(1.0f, 0.93f, 0.74f), 0.0f, 0.14f, new Vector2(1.8f, 1.8f));
        safetyRed = CreateMaterial("Safety Red Paint", steelTexture, new Color(0.72f, 0.055f, 0.025f), 0.42f, 0.30f, new Vector2(1f, 1f));
        red = CreateMaterial("Warning Red", steelTexture, new Color(1.0f, 0.13f, 0.05f), 0.35f, 0.24f, new Vector2(1f, 1f), new Color(1f, 0.03f, 0f) * 2.5f);
        amber = CreateMaterial("Warning Amber", steelTexture, new Color(1f, 0.44f, 0.03f), 0.25f, 0.24f, new Vector2(1f, 1f), new Color(1f, 0.18f, 0f) * 1.4f);
        green = CreateMaterial("Status Green", steelTexture, new Color(0.08f, 0.65f, 0.21f), 0.20f, 0.22f, new Vector2(1f, 1f), new Color(0.01f, 0.35f, 0.04f) * 1.5f);
        whiteGlow = CreateMaterial("Fluorescent Diffuser", ceilingTexture, new Color(0.82f, 0.93f, 1f), 0.0f, 0.16f, new Vector2(1f, 1f), new Color(0.34f, 0.60f, 1f) * 2.2f);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        RenderSettings.ambientMode = AmbientMode.Flat;
        RenderSettings.ambientLight = new Color(0.055f, 0.075f, 0.085f);
        RenderSettings.fog = true;
        RenderSettings.fogColor = new Color(0.07f, 0.09f, 0.09f);
        RenderSettings.fogDensity = 0.014f;

        Transform architecture = NewRoot("ARCHITECTURE");
        Transform equipment = NewRoot("EQUIPMENT");
        Transform furniture = NewRoot("FURNITURE");
        Transform storytelling = NewRoot("ENVIRONMENTAL DETAILS");
        Transform lighting = NewRoot("LIGHTING");

        BuildArchitecture(architecture, lighting);
        BuildEquipment(equipment);
        BuildFurniture(furniture);
        BuildStoryDetails(storytelling);
        BuildLighting(lighting);
        BuildCamera();

        EditorSceneManager.SaveScene(scene, ScenePath);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("Project 5 textured command room created at " + ScenePath);
    }

    [MenuItem("Project 5/Render Command Room Preview")]
    public static void RenderPreview()
    {
        EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        Camera camera = Camera.main;
        if (camera == null)
        {
            throw new InvalidOperationException("The command room preview camera was not found.");
        }

        EnsureFolder("Assets/Art");
        EnsureFolder("Assets/Art/Previews");
        RenderCameraToPng(camera, "Assets/Art/Previews/command-room-preview.png");

        Vector3 originalPosition = camera.transform.position;
        Quaternion originalRotation = camera.transform.rotation;

        camera.transform.position = new Vector3(-3.18f, 1.62f, -1.92f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(0.12f, 1.30f, 0.80f) - camera.transform.position, Vector3.up);
        RenderCameraToPng(camera, "Assets/Art/Previews/command-room-left-preview.png");

        camera.transform.position = new Vector3(2.48f, 1.56f, -0.12f);
        camera.transform.rotation = Quaternion.LookRotation(new Vector3(3.33f, 1.22f, -1.35f) - camera.transform.position, Vector3.up);
        RenderCameraToPng(camera, "Assets/Art/Previews/command-room-phone-preview.png");

        camera.transform.position = originalPosition;
        camera.transform.rotation = originalRotation;
        Debug.Log("Command room previews rendered to Assets/Art/Previews");
    }

    private static void RenderCameraToPng(Camera camera, string outputPath)
    {
        const int width = 1280;
        const int height = 720;
        RenderTexture renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
        Texture2D image = new Texture2D(width, height, TextureFormat.RGB24, false);
        RenderTexture previous = RenderTexture.active;
        camera.targetTexture = renderTexture;
        camera.Render();
        RenderTexture.active = renderTexture;
        image.ReadPixels(new Rect(0f, 0f, width, height), 0, 0);
        image.Apply();
        camera.targetTexture = null;
        RenderTexture.active = previous;
        File.WriteAllBytes(outputPath, image.EncodeToPNG());
        AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
        UnityEngine.Object.DestroyImmediate(image);
        UnityEngine.Object.DestroyImmediate(renderTexture);
    }

    private static void BuildArchitecture(Transform parent, Transform lighting)
    {
        const float roomHeight = 3.05f;
        const float thickness = 0.18f;

        Box("Floor 30cm Vinyl Tile", new Vector3(0f, -0.08f, 0f), new Vector3(8f, 0.16f, 5f), floor, parent);
        Box("Acoustic Drop Ceiling", new Vector3(0f, roomHeight, 0f), new Vector3(8f, 0.12f, 5f), ceiling, parent);

        Wall("Front Wall Left", new Vector2(-4f, -2.5f), new Vector2(1.90f, -2.5f), roomHeight, thickness, wall, parent);
        Wall("Front Wall Right", new Vector2(3.22f, -2.5f), new Vector2(4f, -2.5f), roomHeight, thickness, wall, parent);
        Wall("Left Wall", new Vector2(-4f, -2.5f), new Vector2(-4f, 1.28f), roomHeight, thickness, wall, parent);
        Wall("Right Wall", new Vector2(4f, -2.5f), new Vector2(4f, 1.28f), roomHeight, thickness, wall, parent);
        Wall("Rear Left Angle", new Vector2(-4f, 1.28f), new Vector2(-2.76f, 2.5f), roomHeight, thickness, wall, parent);
        Wall("Rear Wall", new Vector2(-2.76f, 2.5f), new Vector2(2.76f, 2.5f), roomHeight, thickness, wall, parent);
        Wall("Rear Right Angle", new Vector2(2.76f, 2.5f), new Vector2(4f, 1.28f), roomHeight, thickness, wall, parent);

        // Low continuous steel baseboard makes the wall thickness and room construction readable.
        Wall("Baseboard Front Left", new Vector2(-4f, -2.39f), new Vector2(1.90f, -2.39f), 0.16f, 0.05f, darkSteel, parent, 0.08f);
        Wall("Baseboard Front Right", new Vector2(3.22f, -2.39f), new Vector2(4f, -2.39f), 0.16f, 0.05f, darkSteel, parent, 0.08f);
        Wall("Baseboard Left", new Vector2(-3.89f, -2.5f), new Vector2(-3.89f, 1.28f), 0.16f, 0.05f, darkSteel, parent, 0.08f);
        Wall("Baseboard Right", new Vector2(3.89f, -2.5f), new Vector2(3.89f, 1.28f), 0.16f, 0.05f, darkSteel, parent, 0.08f);
        Wall("Baseboard Back", new Vector2(-2.76f, 2.39f), new Vector2(2.76f, 2.39f), 0.16f, 0.05f, darkSteel, parent, 0.08f);

        // A real ceiling needs a grid, fixtures, and a vent—not a white lid.
        for (float x = -3.6f; x <= 3.61f; x += 0.6f)
        {
            Box("Ceiling T Bar X", new Vector3(x, 2.97f, 0f), new Vector3(0.035f, 0.045f, 4.78f), ceilingGrid, parent);
        }
        for (float z = -2.1f; z <= 2.11f; z += 0.6f)
        {
            Box("Ceiling T Bar Z", new Vector3(0f, 2.97f, z), new Vector3(7.78f, 0.045f, 0.035f), ceilingGrid, parent);
        }

        CreateDoor(parent);
        CreateVent(new Vector3(3.15f, 2.94f, 1.48f), parent);
        CreateOverheadServices(parent);
        CreateExitSign(new Vector3(2.56f, 2.48f, -2.31f), parent);
        CreateFluorescent(new Vector3(-1.80f, 2.90f, -0.65f), lighting);
        CreateFluorescent(new Vector3(1.55f, 2.90f, -0.65f), lighting);
        CreateFluorescent(new Vector3(-0.10f, 2.90f, 1.45f), lighting);
    }

    private static void BuildEquipment(Transform parent)
    {
        float[] xPositions = { -2.20f, -1.10f, 0f, 1.10f, 2.20f };
        for (int i = 0; i < xPositions.Length; i++)
        {
            CreateEquipmentCabinet("Rear Equipment Cabinet " + (i + 1), new Vector3(xPositions[i], 0f, 2.13f), 1.02f, 2.42f, parent, i);
        }

        CreateDefconDisplay(new Vector3(0f, 2.73f, 2.28f), parent);
        CreateControlConsole(new Vector3(0f, 0f, 0.35f), parent);
        CreateSideWorkstation(new Vector3(-2.95f, 0f, 0.15f), parent);
        CreateFileCabinet(new Vector3(3.42f, 0f, 0.82f), parent);
        CreateTapeShelves(new Vector3(3.58f, 0f, -0.25f), parent);
        CreatePhoneStation(new Vector3(3.30f, 0f, -1.55f), parent);

        // Conduit breaks up the walls and makes the support infrastructure feel constructed.
        Cylinder("Rear Wall Conduit", new Vector3(0f, 2.45f, 2.28f), 0.045f, 5.35f, darkSteel, parent, Quaternion.Euler(0f, 0f, 90f));
        Cylinder("Right Wall Conduit", new Vector3(3.83f, 2.35f, -0.45f), 0.04f, 3.0f, darkSteel, parent, Quaternion.Euler(90f, 0f, 0f));
        for (int i = -2; i <= 2; i++)
        {
            Cylinder("Conduit Clamp", new Vector3(i * 1.05f, 2.45f, 2.23f), 0.07f, 0.035f, sageSteel, parent, Quaternion.Euler(90f, 0f, 0f));
        }
    }

    private static void BuildFurniture(Transform parent)
    {
        CreateDesk("File Desk", new Vector3(2.30f, 0f, -0.85f), 1.60f, 0.78f, 0.72f, parent);
        CreateChair("Control Chair", new Vector3(0f, 0f, -0.62f), 0f, parent);
        CreateChair("Side Station Chair", new Vector3(-2.35f, 0f, 0.10f), 90f, parent);

        // An intentionally small metal waste bin and a rolling stool add occupied, human scale.
        Cylinder("Waste Bin", new Vector3(2.95f, 0.27f, -2.05f), 0.17f, 0.50f, darkSteel, parent, Quaternion.identity);
        CreateStool(new Vector3(-3.10f, 0f, -1.40f), parent);
    }

    private static void BuildStoryDetails(Transform parent)
    {
        CreateBulletinBoard(new Vector3(-0.35f, 1.40f, -2.37f), parent);
        CreateAlarm(new Vector3(0.25f, 2.20f, 2.29f), parent);
        CreateWallClock(new Vector3(-3.85f, 2.28f, -0.70f), parent);
        CreateFireExtinguisher(new Vector3(-3.72f, 0f, -1.74f), parent);
        CreateWallControlBox(new Vector3(3.84f, 1.12f, -1.12f), parent);
        CreateIntercom(new Vector3(3.84f, 1.90f, -1.80f), parent);

        // Files, clipboard, and pen tray give the officer a believable working residue.
        for (int i = 0; i < 4; i++)
        {
            Box("Labeled Log Binder", new Vector3(1.94f + i * 0.115f, 0.86f, -0.85f), new Vector3(0.085f, 0.22f, 0.48f), i % 2 == 0 ? paper : sageSteel, parent);
        }
        Box("Clipboard", new Vector3(2.42f, 0.91f, -0.85f), new Vector3(0.34f, 0.018f, 0.46f), paper, parent, Quaternion.Euler(0f, 4f, 0f));
        Box("Clipboard Steel Clip", new Vector3(2.42f, 0.932f, -0.66f), new Vector3(0.18f, 0.022f, 0.035f), darkSteel, parent, Quaternion.Euler(0f, 4f, 0f));
        for (int line = 0; line < 5; line++)
        {
            Box("Handwritten Log Line", new Vector3(2.42f, 0.924f, -0.76f - line * 0.055f), new Vector3(0.22f, 0.008f, 0.009f), darkSteel, parent, Quaternion.Euler(0f, 4f, 0f));
        }
        Box("Pen Tray", new Vector3(2.79f, 0.90f, -0.86f), new Vector3(0.13f, 0.03f, 0.36f), darkSteel, parent);
        for (int i = 0; i < 3; i++)
        {
            Cylinder("Pencil", new Vector3(2.76f + i * 0.04f, 0.93f, -0.86f), 0.008f, 0.22f, amber, parent, Quaternion.Euler(90f, 0f, 0f));
        }
    }

    private static void BuildLighting(Transform parent)
    {
        GameObject sun = new GameObject("Cool Directional Fill");
        sun.transform.SetParent(parent, false);
        Light directional = sun.AddComponent<Light>();
        directional.type = LightType.Directional;
        directional.color = new Color(0.48f, 0.64f, 0.76f);
        directional.intensity = 0.22f;
        directional.shadows = LightShadows.Soft;
        sun.transform.rotation = Quaternion.Euler(50f, -35f, 0f);

        PointLight("Button Red Isolation Light", new Vector3(0f, 1.53f, 0.27f), new Color(1f, 0.04f, 0.01f), 2.3f, 2.2f, parent);
        PointLight("Rear Panel Status Spill", new Vector3(0f, 2.25f, 1.88f), new Color(0.22f, 0.74f, 0.50f), 0.8f, 3.0f, parent);
        PointLight("Doorway Fill", new Vector3(3.35f, 1.95f, -2.10f), new Color(0.31f, 0.48f, 0.56f), 0.7f, 2.5f, parent);
    }

    private static void CreateDoor(Transform parent)
    {
        Vector3 position = new Vector3(2.56f, 1.26f, -2.42f);
        Box("Steel Blast Door", position, new Vector3(1.18f, 2.48f, 0.10f), sageSteel, parent);
        Box("Door Frame Left", new Vector3(1.95f, 1.37f, -2.39f), new Vector3(0.09f, 2.72f, 0.16f), darkSteel, parent);
        Box("Door Frame Right", new Vector3(3.17f, 1.37f, -2.39f), new Vector3(0.09f, 2.72f, 0.16f), darkSteel, parent);
        Box("Door Lintel", new Vector3(2.56f, 2.68f, -2.39f), new Vector3(1.30f, 0.10f, 0.16f), darkSteel, parent);
        // The small gaps and pieces of hardware keep the opening from reading as a flat wall texture.
        Box("Door Perimeter Seal", new Vector3(2.56f, 2.49f, -2.485f), new Vector3(1.04f, 0.035f, 0.025f), vinyl, parent);
        Box("Door Lower Kick Plate", new Vector3(2.56f, 0.38f, -2.485f), new Vector3(0.98f, 0.36f, 0.025f), darkSteel, parent);
        Box("Door Threshold", new Vector3(2.56f, 0.025f, -2.18f), new Vector3(1.24f, 0.035f, 0.38f), darkSteel, parent);
        for (int hinge = 0; hinge < 3; hinge++)
        {
            Cylinder("Blast Door Hinge", new Vector3(2.01f, 0.55f + hinge * 0.68f, -2.49f), 0.038f, 0.14f, darkSteel, parent, Quaternion.Euler(90f, 0f, 0f));
        }
        Box("Door Push Bar", new Vector3(2.56f, 1.20f, -2.49f), new Vector3(0.56f, 0.06f, 0.05f), darkSteel, parent);
        Cylinder("Door Lock Wheel", new Vector3(2.94f, 1.48f, -2.50f), 0.10f, 0.04f, darkSteel, parent, Quaternion.Euler(90f, 0f, 0f));
    }

    private static void CreateVent(Vector3 position, Transform parent)
    {
        Box("Ceiling Vent Recess", position, new Vector3(0.72f, 0.035f, 0.72f), ceilingGrid, parent);
        for (int i = -3; i <= 3; i++)
        {
            Box("Vent Slat", position + new Vector3(i * 0.085f, -0.028f, 0f), new Vector3(0.025f, 0.020f, 0.58f), ceilingGrid, parent);
        }
    }

    private static void CreateOverheadServices(Transform parent)
    {
        // Pipework, cable tray, and brackets keep the ceiling from reading as an isolated art asset.
        Cylinder("Left Cooling Pipe", new Vector3(-3.68f, 2.55f, -0.22f), 0.075f, 3.60f, ceilingGrid, parent, Quaternion.Euler(90f, 0f, 0f));
        for (int i = -1; i <= 1; i++)
        {
            Box("Cooling Pipe Wall Strap", new Vector3(-3.82f, 2.55f, i * 1.10f - 0.22f), new Vector3(0.05f, 0.22f, 0.07f), ceilingGrid, parent);
        }

        Vector3 tray = new Vector3(1.45f, 2.68f, 2.04f);
        Box("Cable Tray Front Rail", tray + new Vector3(0f, 0f, -0.14f), new Vector3(3.45f, 0.065f, 0.035f), darkSteel, parent);
        Box("Cable Tray Back Rail", tray + new Vector3(0f, 0f, 0.14f), new Vector3(3.45f, 0.065f, 0.035f), darkSteel, parent);
        for (float x = -1.50f; x <= 1.51f; x += 0.38f)
        {
            Box("Cable Tray Rung", tray + new Vector3(x, 0f, 0f), new Vector3(0.025f, 0.045f, 0.34f), sageSteel, parent);
        }
        Cylinder("Power Cable Red", tray + new Vector3(0f, -0.055f, -0.055f), 0.025f, 3.15f, red, parent, Quaternion.Euler(0f, 0f, 90f));
        Cylinder("Signal Cable Green", tray + new Vector3(0f, -0.055f, 0.020f), 0.020f, 3.15f, green, parent, Quaternion.Euler(0f, 0f, 90f));
        Cylinder("Signal Cable Black", tray + new Vector3(0f, -0.055f, 0.085f), 0.020f, 3.15f, vinyl, parent, Quaternion.Euler(0f, 0f, 90f));
    }

    private static void CreateExitSign(Vector3 position, Transform parent)
    {
        Box("Emergency Exit Sign Housing", position, new Vector3(0.60f, 0.19f, 0.05f), darkSteel, parent);
        Box("Emergency Exit Sign Face", position + Vector3.forward * 0.032f, new Vector3(0.50f, 0.12f, 0.012f), green, parent);
        TextLabel("Emergency Exit Sign Text", "EXIT", position + Vector3.forward * 0.045f, Vector3.forward, 0.090f, new Color(0.90f, 1f, 0.88f), parent);
    }

    private static void CreateFluorescent(Vector3 position, Transform parent)
    {
        Box("Fluorescent Fixture Housing", position, new Vector3(2.12f, 0.08f, 0.29f), ceilingGrid, parent);
        Box("Fluorescent Diffuser", position + Vector3.down * 0.053f, new Vector3(1.93f, 0.024f, 0.16f), whiteGlow, parent);
        PointLight("Cold Fluorescent Pool", position + Vector3.down * 0.18f, new Color(0.58f, 0.77f, 1f), 3.6f, 5.8f, parent);
    }

    private static void CreateEquipmentCabinet(string name, Vector3 floorPosition, float width, float height, Transform parent, int variation)
    {
        float depth = 0.42f;
        Vector3 bodyCenter = floorPosition + new Vector3(0f, height * 0.5f, 0f);
        Transform cabinet = NewRoot(name, parent);
        Box("Cabinet Housing", bodyCenter, new Vector3(width, height, depth), sageSteel, cabinet);
        Box("Cabinet Door", bodyCenter + new Vector3(0f, 0f, -depth * 0.52f), new Vector3(width - 0.10f, height - 0.13f, 0.035f), sageSteel, cabinet);

        float front = floorPosition.z - depth * 0.56f;
        Box("Cabinet Recessed Plinth", new Vector3(floorPosition.x, 0.10f, floorPosition.z - 0.01f), new Vector3(width - 0.12f, 0.18f, depth - 0.05f), darkSteel, cabinet);
        Box("Cabinet Door Pull", new Vector3(floorPosition.x + width * 0.30f, 1.02f, front - 0.032f), new Vector3(0.055f, 0.25f, 0.040f), darkSteel, cabinet);
        Sphere("Cabinet Cam Lock", new Vector3(floorPosition.x + width * 0.30f, 1.18f, front - 0.058f), 0.028f, darkSteel, cabinet);
        for (int y = 0; y < 2; y++)
        {
            Sphere("Cabinet Door Fastener", new Vector3(floorPosition.x - width * 0.40f, 0.78f + y * 0.86f, front - 0.050f), 0.018f, darkSteel, cabinet);
            Sphere("Cabinet Door Fastener", new Vector3(floorPosition.x + width * 0.40f, 0.78f + y * 0.86f, front - 0.050f), 0.018f, darkSteel, cabinet);
        }
        Box("Instrument Panel", new Vector3(floorPosition.x, 1.60f, front), new Vector3(width - 0.26f, 0.46f, 0.045f), darkSteel, cabinet);
        Box("Meter", new Vector3(floorPosition.x - 0.20f, 1.72f, front - 0.028f), new Vector3(0.22f, 0.18f, 0.026f), paper, cabinet);
        Box("Meter Needle", new Vector3(floorPosition.x - 0.20f, 1.70f, front - 0.046f), new Vector3(0.012f, 0.10f, 0.012f), red, cabinet, Quaternion.Euler(0f, 0f, variation % 2 == 0 ? -32f : 24f));
        Box("Data Display", new Vector3(floorPosition.x + 0.18f, 1.72f, front - 0.030f), new Vector3(0.20f, 0.15f, 0.025f), green, cabinet);

        for (int i = -1; i <= 1; i++)
        {
            Material lamp = i == 0 ? amber : (i < 0 ? green : red);
            Sphere("Panel Indicator", new Vector3(floorPosition.x + i * 0.18f, 1.37f, front - 0.035f), 0.04f, lamp, cabinet);
        }

        for (int row = 0; row < 4; row++)
        {
            Box("Vent Slot", new Vector3(floorPosition.x, 0.36f + row * 0.10f, front - 0.028f), new Vector3(width - 0.34f, 0.025f, 0.018f), darkSteel, cabinet);
        }
        Box("Cabinet Label", new Vector3(floorPosition.x, 2.23f, front - 0.03f), new Vector3(width - 0.32f, 0.11f, 0.02f), paper, cabinet);
        TextLabel("Cabinet Number", "SYSTEM " + (variation + 1).ToString("00"), new Vector3(floorPosition.x, 2.23f, front - 0.047f), Vector3.back, 0.035f, new Color(0.11f, 0.14f, 0.12f), cabinet);
    }

    private static void CreateControlConsole(Vector3 position, Transform parent)
    {
        Transform console = NewRoot("Central Command Console", parent);
        Box("Console Base", position + new Vector3(0f, 0.46f, 0f), new Vector3(2.24f, 0.92f, 1.04f), sageSteel, console);
        Box("Console Kick Plate", position + new Vector3(0f, 0.15f, -0.55f), new Vector3(2.16f, 0.20f, 0.04f), darkSteel, console);
        Box("Sloped Control Surface", position + new Vector3(0f, 1.03f, 0.02f), new Vector3(2.08f, 0.10f, 0.72f), darkSteel, console, Quaternion.Euler(20f, 0f, 0f));
        Box("Control Surface Inset", position + new Vector3(0f, 1.08f, -0.05f), new Vector3(1.82f, 0.025f, 0.48f), sageSteel, console, Quaternion.Euler(20f, 0f, 0f));

        for (int x = -3; x <= 3; x++)
        {
            for (int z = 0; z < 2; z++)
            {
                Material buttonMaterial = (x == 0 && z == 1) ? amber : darkSteel;
                Cylinder("Console Toggle", position + new Vector3(x * 0.22f, 1.15f + z * 0.02f, 0.03f + z * 0.18f), 0.035f, 0.045f, buttonMaterial, console, Quaternion.identity);
            }
        }
        Cylinder("Irreversible Command Button Housing", position + new Vector3(0f, 1.14f, -0.32f), 0.20f, 0.09f, darkSteel, console, Quaternion.identity);
        Cylinder("Irreversible Command Button", position + new Vector3(0f, 1.21f, -0.32f), 0.14f, 0.10f, red, console, Quaternion.identity);
        Box("Button Guard Left", position + new Vector3(-0.24f, 1.27f, -0.32f), new Vector3(0.035f, 0.15f, 0.30f), safetyRed, console, Quaternion.Euler(0f, 0f, 10f));
        Box("Button Guard Right", position + new Vector3(0.24f, 1.27f, -0.32f), new Vector3(0.035f, 0.15f, 0.30f), safetyRed, console, Quaternion.Euler(0f, 0f, -10f));
        Box("Button Guard Crossbar", position + new Vector3(0f, 1.34f, -0.20f), new Vector3(0.50f, 0.035f, 0.035f), safetyRed, console);
        Box("Button Engraving Plate", position + new Vector3(0f, 1.09f, -0.47f), new Vector3(0.74f, 0.08f, 0.02f), paper, console, Quaternion.Euler(20f, 0f, 0f));
        TextLabel("Button Instruction", "PRESS   /   LEAVE", position + new Vector3(0f, 1.11f, -0.49f), new Vector3(0f, 0.34f, -0.94f), 0.025f, new Color(0.10f, 0.12f, 0.10f), console);
        TextLabel("Console Header", "COMMAND  RELAY", position + new Vector3(0f, 0.86f, -0.55f), Vector3.back, 0.040f, new Color(0.14f, 0.18f, 0.15f), console);
    }

    private static void CreateDefconDisplay(Vector3 position, Transform parent)
    {
        Transform display = NewRoot("DEFCON Status Display", parent);
        Box("DEFCON Display Housing", position, new Vector3(2.34f, 0.48f, 0.08f), darkSteel, display);
        Box("DEFCON Display Screen", position + Vector3.back * 0.052f, new Vector3(2.12f, 0.34f, 0.02f), red, display);
        TextLabel("DEFCON Readout", "DEFCON 2", position + new Vector3(0f, 0.02f, -0.071f), Vector3.back, 0.115f, new Color(1f, 0.94f, 0.78f), display);
        TextLabel("DEFCON Descriptor", "READINESS REDUCED", position + new Vector3(0f, -0.16f, -0.071f), Vector3.back, 0.045f, new Color(1f, 0.94f, 0.78f), display);
        for (int i = -1; i <= 1; i++)
        {
            Sphere("DEFCON Indicator", position + new Vector3(i * 0.92f, 0f, -0.075f), 0.035f, i == 0 ? amber : green, display);
        }
    }

    private static void CreateSideWorkstation(Vector3 position, Transform parent)
    {
        Transform station = NewRoot("Rear Left Monitor Station", parent);
        Box("Monitor Station Base", position + new Vector3(0f, 0.41f, 0f), new Vector3(0.80f, 0.82f, 1.20f), sageSteel, station);
        Box("Monitor Station Top", position + new Vector3(0f, 0.86f, 0f), new Vector3(0.92f, 0.08f, 1.30f), wood, station);
        Box("CRT Monitor Shell", position + new Vector3(0f, 1.18f, 0.29f), new Vector3(0.56f, 0.50f, 0.34f), darkSteel, station);
        Box("CRT Green Screen", position + new Vector3(0f, 1.18f, 0.105f), new Vector3(0.43f, 0.30f, 0.025f), green, station);
        TextLabel("CRT Readout", "UNCONFIRMED\nSIGNAL", position + new Vector3(0f, 1.20f, 0.087f), Vector3.back, 0.025f, new Color(0.81f, 1f, 0.75f), station);
        Box("Keyboard", position + new Vector3(0f, 0.93f, -0.33f), new Vector3(0.52f, 0.05f, 0.30f), darkSteel, station);
        for (int row = 0; row < 3; row++)
        {
            for (int x = -3; x <= 3; x++)
            {
                Box("Keyboard Key", position + new Vector3(x * 0.065f, 0.967f, -0.40f + row * 0.07f), new Vector3(0.045f, 0.018f, 0.050f), sageSteel, station);
            }
        }
    }

    private static void CreateFileCabinet(Vector3 position, Transform parent)
    {
        Transform cabinet = NewRoot("File Cabinet", parent);
        Box("File Cabinet Housing", position + new Vector3(0f, 0.90f, 0f), new Vector3(0.74f, 1.80f, 0.58f), sageSteel, cabinet);
        for (int i = 0; i < 4; i++)
        {
            float y = 0.31f + i * 0.40f;
            Box("Drawer Front", position + new Vector3(0f, y, -0.32f), new Vector3(0.63f, 0.34f, 0.035f), sageSteel, cabinet);
            Box("Drawer Pull", position + new Vector3(0f, y, -0.35f), new Vector3(0.26f, 0.035f, 0.025f), darkSteel, cabinet);
            Box("Drawer Label", position + new Vector3(0.21f, y + 0.08f, -0.35f), new Vector3(0.13f, 0.08f, 0.012f), paper, cabinet);
        }
    }

    private static void CreateTapeShelves(Vector3 position, Transform parent)
    {
        Transform shelves = NewRoot("Tape Shelf", parent);
        Box("Tape Shelf Frame", position + new Vector3(0f, 1.08f, 0f), new Vector3(0.55f, 2.16f, 0.30f), darkSteel, shelves);
        for (int shelf = 0; shelf < 5; shelf++)
        {
            float y = 0.28f + shelf * 0.41f;
            Box("Shelf Lip", position + new Vector3(0f, y, -0.18f), new Vector3(0.48f, 0.045f, 0.03f), sageSteel, shelves);
            for (int tape = -2; tape <= 2; tape++)
            {
                Box("Reel Tape Box", position + new Vector3(tape * 0.083f, y + 0.13f, -0.18f), new Vector3(0.068f, 0.20f, 0.12f), (tape + shelf) % 3 == 0 ? paper : sageSteel, shelves);
            }
        }
    }

    private static void CreatePhoneStation(Vector3 position, Transform parent)
    {
        Transform phone = NewRoot("Emergency Phone Station", parent);
        Box("Phone Table", position + new Vector3(0f, 0.56f, 0f), new Vector3(0.68f, 1.12f, 0.48f), sageSteel, phone);
        Box("Phone Table Top", position + new Vector3(0f, 1.16f, 0f), new Vector3(0.78f, 0.07f, 0.58f), wood, phone);
        Box("Telephone Base", position + new Vector3(0f, 1.23f, -0.02f), new Vector3(0.34f, 0.10f, 0.23f), vinyl, phone);
        Box("Telephone Number Plate", position + new Vector3(0f, 1.285f, -0.005f), new Vector3(0.18f, 0.012f, 0.12f), darkSteel, phone);
        for (int row = 0; row < 3; row++)
        {
            for (int column = -1; column <= 1; column++)
            {
                Sphere("Telephone Dial Key", position + new Vector3(column * 0.052f, 1.300f, -0.040f + row * 0.040f), 0.012f, paper, phone);
            }
        }
        for (int slot = -2; slot <= 2; slot++)
        {
            Box("Telephone Speaker Slot", position + new Vector3(slot * 0.026f, 1.300f, 0.082f), new Vector3(0.012f, 0.012f, 0.038f), darkSteel, phone);
        }
        Box("Telephone Handset", position + new Vector3(0f, 1.34f, -0.02f), new Vector3(0.47f, 0.07f, 0.09f), vinyl, phone);
        Cylinder("Handset Earpiece Left", position + new Vector3(-0.20f, 1.34f, -0.02f), 0.06f, 0.08f, vinyl, phone, Quaternion.Euler(90f, 0f, 0f));
        Cylinder("Handset Earpiece Right", position + new Vector3(0.20f, 1.34f, -0.02f), 0.06f, 0.08f, vinyl, phone, Quaternion.Euler(90f, 0f, 0f));
        for (int i = 0; i < 7; i++)
        {
            Sphere("Coiled Phone Cord", position + new Vector3(0.22f, 1.13f - i * 0.055f, 0.14f + (i % 2) * 0.02f), 0.026f, vinyl, phone);
        }
    }

    private static void CreateDesk(string name, Vector3 position, float width, float depth, float height, Transform parent)
    {
        Transform desk = NewRoot(name, parent);
        Box("Wood Worktop", position + new Vector3(0f, height, 0f), new Vector3(width, 0.085f, depth), wood, desk);
        float legHeight = height - 0.06f;
        foreach (float x in new[] { -width * 0.42f, width * 0.42f })
        {
            foreach (float z in new[] { -depth * 0.38f, depth * 0.38f })
            {
                Box("Desk Steel Leg", position + new Vector3(x, legHeight * 0.5f, z), new Vector3(0.07f, legHeight, 0.07f), darkSteel, desk);
            }
        }
        Box("Desk Modesty Panel", position + new Vector3(0f, height * 0.50f, depth * 0.40f), new Vector3(width * 0.82f, height * 0.54f, 0.035f), wood, desk);
    }

    private static void CreateChair(string name, Vector3 position, float yaw, Transform parent)
    {
        Transform chair = NewRoot(name, parent);
        chair.rotation = Quaternion.Euler(0f, yaw, 0f);
        Box("Chair Seat", position + new Vector3(0f, 0.54f, 0f), new Vector3(0.50f, 0.10f, 0.48f), vinyl, chair);
        Box("Chair Back", position + new Vector3(0f, 0.94f, 0.19f), new Vector3(0.48f, 0.74f, 0.10f), vinyl, chair, Quaternion.Euler(-8f, 0f, 0f));
        Box("Chair Lumbar Pad", position + new Vector3(0f, 0.84f, 0.125f), new Vector3(0.34f, 0.23f, 0.045f), vinyl, chair, Quaternion.Euler(-8f, 0f, 0f));
        Cylinder("Chair Gas Lift", position + new Vector3(0f, 0.28f, 0f), 0.06f, 0.45f, darkSteel, chair, Quaternion.identity);
        for (int i = 0; i < 5; i++)
        {
            float angle = i * 72f * Mathf.Deg2Rad;
            Vector3 spoke = new Vector3(Mathf.Cos(angle) * 0.25f, 0.13f, Mathf.Sin(angle) * 0.25f);
            Cylinder("Chair Wheel Spoke", position + new Vector3(0f, 0.13f, 0f) + spoke * 0.5f, 0.025f, 0.48f, darkSteel, chair, Quaternion.Euler(0f, 0f, 90f + i * 72f));
            Sphere("Chair Caster", position + new Vector3(spoke.x, 0.08f, spoke.z), 0.055f, darkSteel, chair);
        }
        Box("Left Arm Rest", position + new Vector3(-0.31f, 0.70f, 0f), new Vector3(0.08f, 0.08f, 0.28f), vinyl, chair);
        Box("Right Arm Rest", position + new Vector3(0.31f, 0.70f, 0f), new Vector3(0.08f, 0.08f, 0.28f), vinyl, chair);
        Box("Left Arm Support", position + new Vector3(-0.31f, 0.62f, 0.03f), new Vector3(0.045f, 0.22f, 0.045f), darkSteel, chair);
        Box("Right Arm Support", position + new Vector3(0.31f, 0.62f, 0.03f), new Vector3(0.045f, 0.22f, 0.045f), darkSteel, chair);
        Box("Chair Back Support", position + new Vector3(0f, 0.62f, 0.18f), new Vector3(0.07f, 0.44f, 0.06f), darkSteel, chair, Quaternion.Euler(-8f, 0f, 0f));
    }

    private static void CreateStool(Vector3 position, Transform parent)
    {
        Cylinder("Rolling Stool Seat", position + new Vector3(0f, 0.53f, 0f), 0.22f, 0.10f, vinyl, parent, Quaternion.identity);
        Cylinder("Stool Lift", position + new Vector3(0f, 0.26f, 0f), 0.045f, 0.45f, darkSteel, parent, Quaternion.identity);
        for (int i = 0; i < 4; i++)
        {
            float angle = i * 90f * Mathf.Deg2Rad;
            Vector3 edge = new Vector3(Mathf.Cos(angle) * 0.18f, 0.10f, Mathf.Sin(angle) * 0.18f);
            Sphere("Stool Caster", position + edge, 0.05f, darkSteel, parent);
        }
    }

    private static void CreateBulletinBoard(Vector3 position, Transform parent)
    {
        Transform board = NewRoot("Bulletin Board", parent);
        Box("Bulletin Board Backer", position, new Vector3(1.70f, 0.95f, 0.05f), wood, board);
        Box("Bulletin Board Cork", position + Vector3.forward * 0.035f, new Vector3(1.52f, 0.77f, 0.02f), cork, board);
        Vector3[] noteOffsets =
        {
            new Vector3(-0.43f, 0.16f, 0.06f), new Vector3(-0.05f, -0.08f, 0.06f),
            new Vector3(0.38f, 0.14f, 0.06f), new Vector3(0.38f, -0.22f, 0.06f)
        };
        for (int i = 0; i < noteOffsets.Length; i++)
        {
            Vector3 note = position + noteOffsets[i];
            Box("Pinned Note", note, new Vector3(0.31f, 0.22f, 0.015f), i == 2 ? sageSteel : paper, board, Quaternion.Euler(0f, 0f, (i - 1) * 4f));
            Sphere("Push Pin", note + Vector3.forward * 0.022f + Vector3.up * 0.08f, 0.022f, i % 2 == 0 ? red : amber, board);
        }
        TextLabel("Board Header", "SHIFT LOG  //  0200", position + new Vector3(0f, 0.33f, 0.065f), Vector3.forward, 0.030f, Color.black, board);
    }

    private static void CreateAlarm(Vector3 position, Transform parent)
    {
        Box("Alarm Mounting Plate", position, new Vector3(0.34f, 0.30f, 0.04f), darkSteel, parent);
        Cylinder("Red Alarm Beacon", position + Vector3.back * 0.07f, 0.11f, 0.10f, red, parent, Quaternion.Euler(90f, 0f, 0f));
        TextLabel("Alarm Label", "ALERT", position + new Vector3(0f, -0.12f, -0.04f), Vector3.back, 0.028f, new Color(0.96f, 0.90f, 0.78f), parent);
    }

    private static void CreateWallClock(Vector3 position, Transform parent)
    {
        Cylinder("Wall Clock Rim", position, 0.20f, 0.05f, darkSteel, parent, Quaternion.Euler(90f, 0f, 0f));
        Cylinder("Wall Clock Face", position + Vector3.right * 0.035f, 0.16f, 0.02f, paper, parent, Quaternion.Euler(0f, 0f, 90f));
        Box("Clock Hand Long", position + new Vector3(0.045f, 0.05f, 0f), new Vector3(0.014f, 0.14f, 0.014f), darkSteel, parent, Quaternion.Euler(0f, 0f, -50f));
        Box("Clock Hand Short", position + new Vector3(0.048f, -0.025f, 0f), new Vector3(0.014f, 0.09f, 0.014f), darkSteel, parent, Quaternion.Euler(0f, 0f, 70f));
    }

    private static void CreateFireExtinguisher(Vector3 floorPosition, Transform parent)
    {
        Transform extinguisher = NewRoot("Fire Extinguisher", parent);
        Box("Extinguisher Wall Bracket", floorPosition + new Vector3(-0.07f, 0.54f, 0.03f), new Vector3(0.04f, 0.62f, 0.18f), darkSteel, extinguisher);
        Cylinder("Extinguisher Cylinder", floorPosition + new Vector3(0f, 0.38f, 0f), 0.12f, 0.62f, safetyRed, extinguisher, Quaternion.identity);
        Cylinder("Extinguisher Neck", floorPosition + new Vector3(0f, 0.73f, 0f), 0.045f, 0.12f, darkSteel, extinguisher, Quaternion.identity);
        Box("Extinguisher Handle", floorPosition + new Vector3(0f, 0.82f, -0.05f), new Vector3(0.23f, 0.035f, 0.10f), darkSteel, extinguisher);
        Box("Extinguisher Label", floorPosition + new Vector3(0f, 0.41f, -0.115f), new Vector3(0.14f, 0.22f, 0.014f), paper, extinguisher);
        Cylinder("Extinguisher Hose", floorPosition + new Vector3(0.13f, 0.56f, -0.01f), 0.018f, 0.32f, vinyl, extinguisher, Quaternion.Euler(90f, 0f, 0f));
    }

    private static void CreateWallControlBox(Vector3 position, Transform parent)
    {
        Transform box = NewRoot("Wall Electrical Control", parent);
        Box("Control Box Housing", position, new Vector3(0.07f, 0.44f, 0.30f), sageSteel, box);
        Box("Control Box Panel", position + Vector3.left * 0.045f, new Vector3(0.018f, 0.32f, 0.22f), darkSteel, box);
        Sphere("Control Box Green Lamp", position + new Vector3(-0.058f, 0.09f, -0.055f), 0.030f, green, box);
        Sphere("Control Box Amber Lamp", position + new Vector3(-0.058f, 0.01f, -0.055f), 0.030f, amber, box);
        Sphere("Control Box Red Lamp", position + new Vector3(-0.058f, -0.07f, -0.055f), 0.030f, red, box);
        Box("Control Box Label Plate", position + new Vector3(-0.058f, 0.16f, 0.035f), new Vector3(0.012f, 0.058f, 0.13f), paper, box);
        TextLabel("Control Box Label", "POWER", position + new Vector3(-0.066f, 0.16f, 0.035f), Vector3.left, 0.024f, new Color(0.10f, 0.12f, 0.10f), box);
    }

    private static void CreateIntercom(Vector3 position, Transform parent)
    {
        Transform intercom = NewRoot("Wall Intercom", parent);
        Box("Intercom Housing", position, new Vector3(0.07f, 0.38f, 0.22f), sageSteel, intercom);
        Box("Intercom Speaker Grille", position + Vector3.left * 0.046f, new Vector3(0.018f, 0.18f, 0.14f), darkSteel, intercom);
        for (int i = -2; i <= 2; i++)
        {
            Box("Intercom Grille Slot", position + new Vector3(-0.058f, i * 0.028f, 0f), new Vector3(0.012f, 0.012f, 0.12f), sageSteel, intercom);
        }
        Sphere("Intercom Status Lamp", position + new Vector3(-0.060f, -0.13f, 0.065f), 0.025f, amber, intercom);
        Box("Intercom Label Plate", position + new Vector3(-0.060f, 0.14f, 0f), new Vector3(0.012f, 0.056f, 0.12f), paper, intercom);
        TextLabel("Intercom Label", "COMMS", position + new Vector3(-0.068f, 0.14f, 0f), Vector3.left, 0.022f, new Color(0.10f, 0.12f, 0.10f), intercom);
    }

    private static void BuildCamera()
    {
        GameObject cameraObject = new GameObject("Player Preview Camera");
        cameraObject.tag = "MainCamera";
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.fieldOfView = 63f;
        camera.nearClipPlane = 0.04f;
        camera.farClipPlane = 50f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.028f, 0.04f, 0.045f);
        cameraObject.transform.position = new Vector3(3.20f, 1.62f, -2.06f);
        cameraObject.transform.rotation = Quaternion.LookRotation(new Vector3(-0.38f, 1.30f, 0.94f) - cameraObject.transform.position, Vector3.up);
    }

    private static void Wall(string name, Vector2 start, Vector2 end, float height, float thickness, Material material, Transform parent, float y = -1f)
    {
        Vector2 delta = end - start;
        Vector2 center = (start + end) * 0.5f;
        float midY = y < 0f ? height * 0.5f : y;
        GameObject go = Box(name, new Vector3(center.x, midY, center.y), new Vector3(delta.magnitude, height, thickness), material, parent);
        go.transform.rotation = Quaternion.Euler(0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg, 0f);
    }

    private static GameObject Box(string name, Vector3 position, Vector3 scale, Material material, Transform parent, Quaternion? rotation = null)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = scale;
        go.transform.rotation = rotation ?? Quaternion.identity;
        go.transform.SetParent(parent, true);
        Renderer renderer = go.GetComponent<Renderer>();
        renderer.sharedMaterial = material;
        return go;
    }

    private static GameObject Cylinder(string name, Vector3 position, float radius, float height, Material material, Transform parent, Quaternion rotation)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = new Vector3(radius * 2f, height * 0.5f, radius * 2f);
        go.transform.rotation = rotation;
        go.transform.SetParent(parent, true);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
    }

    private static GameObject Sphere(string name, Vector3 position, float radius, Material material, Transform parent)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;
        go.transform.position = position;
        go.transform.localScale = Vector3.one * radius * 2f;
        go.transform.SetParent(parent, true);
        go.GetComponent<Renderer>().sharedMaterial = material;
        return go;
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
        text.characterSize = size * 0.30f;
        text.anchor = TextAnchor.MiddleCenter;
        text.alignment = TextAlignment.Center;
        text.color = color;
        text.richText = false;
    }

    private static void PointLight(string name, Vector3 position, Color color, float intensity, float range, Transform parent)
    {
        GameObject lightObject = new GameObject(name);
        lightObject.transform.SetParent(parent, true);
        lightObject.transform.position = position;
        Light light = lightObject.AddComponent<Light>();
        light.type = LightType.Point;
        light.color = color;
        light.intensity = intensity;
        light.range = range;
        light.shadows = LightShadows.None;
    }

    private static Transform NewRoot(string name, Transform parent = null)
    {
        GameObject root = new GameObject(name);
        if (parent != null)
        {
            root.transform.SetParent(parent, true);
        }
        return root.transform;
    }

    private static Texture2D PrepareTexture(string path)
    {
        AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.maxTextureSize = 1024;
            importer.textureCompression = TextureImporterCompression.Compressed;
            importer.SaveAndReimport();
        }
        return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
    }

    private static Material CreateMaterial(string name, Texture2D texture, Color tint, float metallic, float smoothness, Vector2 tiling, Color? emission = null)
    {
        string path = MaterialsFolder + "/" + name.Replace(" ", "_") + ".mat";
        if (AssetDatabase.LoadAssetAtPath<Material>(path) != null)
        {
            AssetDatabase.DeleteAsset(path);
        }

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        Material material = new Material(shader);
        material.name = name;
        if (material.HasProperty("_BaseMap"))
        {
            material.SetTexture("_BaseMap", texture);
            material.SetColor("_BaseColor", tint);
            material.SetTextureScale("_BaseMap", tiling);
        }
        else
        {
            material.SetTexture("_MainTex", texture);
            material.SetColor("_Color", tint);
            material.SetTextureScale("_MainTex", tiling);
        }
        if (material.HasProperty("_Metallic")) material.SetFloat("_Metallic", metallic);
        if (material.HasProperty("_Smoothness")) material.SetFloat("_Smoothness", smoothness);
        if (emission.HasValue && material.HasProperty("_EmissionColor"))
        {
            material.EnableKeyword("_EMISSION");
            material.SetColor("_EmissionColor", emission.Value);
        }
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static void EnsureFolder(string folder)
    {
        if (AssetDatabase.IsValidFolder(folder)) return;
        string parent = Path.GetDirectoryName(folder).Replace("\\", "/");
        string name = Path.GetFileName(folder);
        if (!AssetDatabase.IsValidFolder(parent)) EnsureFolder(parent);
        AssetDatabase.CreateFolder(parent, name);
    }
}
