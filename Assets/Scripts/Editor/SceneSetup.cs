#if UNITY_EDITOR
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class SceneSetup
{
    private const string ServerScenePath = "Assets/Scenes/ServerScene.unity";
    private const string ClientScenePath = "Assets/Scenes/ClientScene.unity";
    private const string CarPrefabPath = "Assets/Prefabs/Car.prefab";
    private const string MaterialsDir = "Assets/Materials";

    [MenuItem("Tools/Setup Project Scenes")]
    public static void SetupAll()
    {
        Directory.CreateDirectory("Assets/Prefabs");
        Directory.CreateDirectory("Assets/Scenes");
        Directory.CreateDirectory(MaterialsDir);

        GameObject carPrefab = CreateCarPrefab();
        CreateServerScene(carPrefab);
        CreateClientScene();

        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene(ServerScenePath, true),
            new EditorBuildSettingsScene(ClientScenePath, true)
        };

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("[SceneSetup] ServerScene and ClientScene configured with TMP_Text, fixed wheel pivots, and smooth local camera.");
    }

    private static Material GetOrCreateMat(string name, Color color)
    {
        string path = $"{MaterialsDir}/{name}.mat";
        Material mat = AssetDatabase.LoadAssetAtPath<Material>(path);
        if (mat == null)
        {
            Shader shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null) shader = Shader.Find("Standard");
            mat = new Material(shader);
            AssetDatabase.CreateAsset(mat, path);
        }

        if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
        mat.color = color;
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static GameObject CreateCarPrefab()
    {
        Material bodyMat = GetOrCreateMat("CarBody", new Color(0.18f, 0.45f, 0.82f));
        Material cabinMat = GetOrCreateMat("CarCabin", new Color(0.15f, 0.20f, 0.28f));
        Material tireMat = GetOrCreateMat("TireDark", new Color(0.12f, 0.12f, 0.14f));
        Material rimMat = GetOrCreateMat("WheelRim", new Color(0.82f, 0.84f, 0.88f));
        Material brakeMat = GetOrCreateMat("BrakeRed", Color.red);
        Material lightMat = GetOrCreateMat("HeadlightWhite", Color.white);

        GameObject car = new GameObject("Car");
        Rigidbody rb = car.AddComponent<Rigidbody>();
        rb.mass = 1200f;
        rb.linearDamping = 0.05f;
        rb.angularDamping = 0.5f;
        rb.interpolation = RigidbodyInterpolation.Interpolate;

        CarController controller = car.AddComponent<CarController>();

        // Chassis
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cube);
        body.name = "Body";
        body.transform.SetParent(car.transform, false);
        body.transform.localPosition = new Vector3(0f, 0.5f, 0f);
        body.transform.localScale = new Vector3(1.8f, 0.5f, 4.0f);
        body.GetComponent<Renderer>().sharedMaterial = bodyMat;

        GameObject cabin = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cabin.name = "Cabin";
        cabin.transform.SetParent(car.transform, false);
        cabin.transform.localPosition = new Vector3(0f, 1.0f, -0.2f);
        cabin.transform.localScale = new Vector3(1.5f, 0.6f, 2.0f);
        cabin.GetComponent<Renderer>().sharedMaterial = cabinMat;

        // Driver Door (Element 5) - Hinge at front-left, swings outward (+Y rotates -Z toward -X)
        GameObject doorHinge = new GameObject("DriverDoorHinge");
        doorHinge.transform.SetParent(car.transform, false);
        doorHinge.transform.localPosition = new Vector3(-0.92f, 0.6f, 0.4f);

        GameObject doorPanel = GameObject.CreatePrimitive(PrimitiveType.Cube);
        doorPanel.name = "DoorPanel";
        doorPanel.transform.SetParent(doorHinge.transform, false);
        doorPanel.transform.localPosition = new Vector3(0f, 0f, -0.5f);
        doorPanel.transform.localScale = new Vector3(0.08f, 0.5f, 1.0f);
        doorPanel.GetComponent<Renderer>().sharedMaterial = bodyMat;
        Object.DestroyImmediate(doorPanel.GetComponent<Collider>());

        controller.driverDoor = doorHinge.transform;
        controller.doorClosedEuler = Vector3.zero;
        controller.doorOpenEuler = new Vector3(0f, 65f, 0f);

        // Headlights (Element 4)
        GameObject hlL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hlL.name = "Headlight_L";
        hlL.transform.SetParent(car.transform, false);
        hlL.transform.localPosition = new Vector3(-0.65f, 0.55f, 2.02f);
        hlL.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);
        hlL.GetComponent<Renderer>().sharedMaterial = lightMat;
        Object.DestroyImmediate(hlL.GetComponent<Collider>());
        Light spotL = hlL.AddComponent<Light>();
        spotL.type = LightType.Spot;
        spotL.intensity = 15f;
        spotL.range = 30f;
        spotL.spotAngle = 60f;

        GameObject hlR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        hlR.name = "Headlight_R";
        hlR.transform.SetParent(car.transform, false);
        hlR.transform.localPosition = new Vector3(0.65f, 0.55f, 2.02f);
        hlR.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);
        hlR.GetComponent<Renderer>().sharedMaterial = lightMat;
        Object.DestroyImmediate(hlR.GetComponent<Collider>());
        Light spotR = hlR.AddComponent<Light>();
        spotR.type = LightType.Spot;
        spotR.intensity = 15f;
        spotR.range = 30f;
        spotR.spotAngle = 60f;

        controller.headlights = new[] { hlL, hlR };

        // Brake Lights (Element 3)
        GameObject blL = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blL.name = "BrakeLight_L";
        blL.transform.SetParent(car.transform, false);
        blL.transform.localPosition = new Vector3(-0.65f, 0.55f, -2.02f);
        blL.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);
        blL.GetComponent<Renderer>().sharedMaterial = brakeMat;
        Object.DestroyImmediate(blL.GetComponent<Collider>());

        GameObject blR = GameObject.CreatePrimitive(PrimitiveType.Cube);
        blR.name = "BrakeLight_R";
        blR.transform.SetParent(car.transform, false);
        blR.transform.localPosition = new Vector3(0.65f, 0.55f, -2.02f);
        blR.transform.localScale = new Vector3(0.3f, 0.15f, 0.1f);
        blR.GetComponent<Renderer>().sharedMaterial = brakeMat;
        Object.DestroyImmediate(blR.GetComponent<Collider>());

        blL.SetActive(false);
        blR.SetActive(false);
        controller.brakeLights = new[] { blL, blR };

        // Wheels (Colliders + Properly Oriented Visual Pivots)
        Vector3 flPos = new Vector3(-0.95f, 0.35f, 1.3f);
        Vector3 frPos = new Vector3(0.95f, 0.35f, 1.3f);
        Vector3 rlPos = new Vector3(-0.95f, 0.35f, -1.3f);
        Vector3 rrPos = new Vector3(0.95f, 0.35f, -1.3f);

        controller.frontLeftWheel = CreateWheelCollider(car.transform, "WC_FL", flPos);
        controller.frontRightWheel = CreateWheelCollider(car.transform, "WC_FR", frPos);
        controller.rearLeftWheel = CreateWheelCollider(car.transform, "WC_RL", rlPos);
        controller.rearRightWheel = CreateWheelCollider(car.transform, "WC_RR", rrPos);

        controller.frontLeftMesh = CreateWheelVisualPivot(car.transform, "WheelPivot_FL", flPos, tireMat, rimMat);
        controller.frontRightMesh = CreateWheelVisualPivot(car.transform, "WheelPivot_FR", frPos, tireMat, rimMat);
        controller.rearLeftMesh = CreateWheelVisualPivot(car.transform, "WheelPivot_RL", rlPos, tireMat, rimMat);
        controller.rearRightMesh = CreateWheelVisualPivot(car.transform, "WheelPivot_RR", rrPos, tireMat, rimMat);

        // Camera Anchors (positioned and aimed directly at each car element)
        controller.camChaseTarget = CreateCamAnchor(car.transform, "Cam_Chase", new Vector3(0f, 2.8f, -6.0f), new Vector3(0f, 0.7f, 0.5f));
        controller.camSteeringTarget = CreateCamAnchor(car.transform, "Cam_Steering", new Vector3(-2.3f, 0.9f, 2.1f), new Vector3(-0.95f, 0.35f, 1.3f));
        controller.camThrottleTarget = CreateCamAnchor(car.transform, "Cam_Throttle", new Vector3(-2.3f, 0.9f, -2.1f), new Vector3(-0.95f, 0.35f, -1.3f));
        controller.camBrakesTarget = CreateCamAnchor(car.transform, "Cam_Brakes", new Vector3(0f, 1.1f, -4.0f), new Vector3(0f, 0.55f, -2.0f));
        controller.camHeadlightsTarget = CreateCamAnchor(car.transform, "Cam_Headlights", new Vector3(0f, 1.1f, 4.0f), new Vector3(0f, 0.55f, 2.0f));
        controller.camDoorTarget = CreateCamAnchor(car.transform, "Cam_Door", new Vector3(-2.8f, 1.1f, -0.1f), new Vector3(-0.9f, 0.65f, -0.1f));

        GameObject saved = PrefabUtility.SaveAsPrefabAsset(car, CarPrefabPath);
        Object.DestroyImmediate(car);
        return saved;
    }

    private static WheelCollider CreateWheelCollider(Transform parent, string name, Vector3 pos)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        WheelCollider wc = go.AddComponent<WheelCollider>();
        wc.radius = 0.35f;
        wc.suspensionDistance = 0.15f;
        wc.mass = 20f;
        wc.wheelDampingRate = 0.5f;

        JointSpring spring = wc.suspensionSpring;
        spring.spring = 30000f;
        spring.damper = 4500f;
        spring.targetPosition = 0.5f;
        wc.suspensionSpring = spring;

        return wc;
    }

    private static Transform CreateWheelVisualPivot(Transform parent, string name, Vector3 pos, Material tireMat, Material rimMat)
    {
        // Parent pivot receives world pose from WheelCollider.GetWorldPose
        GameObject pivot = new GameObject(name);
        pivot.transform.SetParent(parent, false);
        pivot.transform.localPosition = pos;

        // Child cylinder rotated 90 degrees on Z so its circular face is aligned with the wheel axle (X axis)
        GameObject tire = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        tire.name = "TireMesh";
        tire.transform.SetParent(pivot.transform, false);
        tire.transform.localPosition = Vector3.zero;
        tire.transform.localEulerAngles = new Vector3(0f, 0f, 90f);
        tire.transform.localScale = new Vector3(0.7f, 0.12f, 0.7f);
        Object.DestroyImmediate(tire.GetComponent<Collider>());
        tire.GetComponent<Renderer>().sharedMaterial = tireMat;

        // Spoke bar so wheel rolling rotation is clearly visible
        GameObject spoke = GameObject.CreatePrimitive(PrimitiveType.Cube);
        spoke.name = "WheelSpoke";
        spoke.transform.SetParent(pivot.transform, false);
        spoke.transform.localPosition = Vector3.zero;
        spoke.transform.localScale = new Vector3(0.26f, 0.48f, 0.08f);
        Object.DestroyImmediate(spoke.GetComponent<Collider>());
        spoke.GetComponent<Renderer>().sharedMaterial = rimMat;

        return pivot.transform;
    }

    private static Transform CreateCamAnchor(Transform parent, string name, Vector3 localPos, Vector3 lookAtLocalTarget)
    {
        GameObject go = new GameObject(name);
        go.transform.SetParent(parent, false);
        go.transform.localPosition = localPos;
        go.transform.localRotation = Quaternion.LookRotation((lookAtLocalTarget - localPos).normalized, Vector3.up);
        return go.transform;
    }

    private static Material GetOrCreateCheckerFloorMaterial()
    {
        string texPath = $"{MaterialsDir}/CheckerFloor.png";
        int size = 128;
        int half = size / 2;
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, true);
        Color darkTile = new Color(0.18f, 0.20f, 0.24f, 1f);
        Color lightTile = new Color(0.34f, 0.38f, 0.44f, 1f);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                bool isLight = (x < half) ^ (y < half);
                tex.SetPixel(x, y, isLight ? lightTile : darkTile);
            }
        }
        tex.Apply();
        File.WriteAllBytes(texPath, tex.EncodeToPNG());
        Object.DestroyImmediate(tex);

        AssetDatabase.ImportAsset(texPath, ImportAssetOptions.ForceUpdate);
        if (AssetImporter.GetAtPath(texPath) is TextureImporter importer)
        {
            importer.wrapMode = TextureWrapMode.Repeat;
            importer.filterMode = FilterMode.Trilinear;
            importer.anisoLevel = 8;
            importer.SaveAndReimport();
        }

        Texture2D savedTex = AssetDatabase.LoadAssetAtPath<Texture2D>(texPath);
        Material mat = GetOrCreateMat("GroundChecker", Color.white);
        Vector2 tiling = new Vector2(75f, 75f);

        mat.mainTexture = savedTex;
        mat.mainTextureScale = tiling;
        if (mat.HasProperty("_BaseMap"))
        {
            mat.SetTexture("_BaseMap", savedTex);
            mat.SetTextureScale("_BaseMap", tiling);
        }
        if (mat.HasProperty("_Smoothness"))
        {
            mat.SetFloat("_Smoothness", 0.2f);
        }
        EditorUtility.SetDirty(mat);
        return mat;
    }

    private static void CreateServerScene(GameObject carPrefab)
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        Material groundMat = GetOrCreateCheckerFloorMaterial();

        // Light
        GameObject lightGo = new GameObject("Directional Light");
        Light dir = lightGo.AddComponent<Light>();
        dir.type = LightType.Directional;
        dir.intensity = 1.2f;
        lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

        // Ground Plane
        GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
        ground.name = "Ground";
        ground.transform.localScale = new Vector3(30f, 1f, 30f);
        ground.GetComponent<Renderer>().sharedMaterial = groundMat;

        // Car
        GameObject car = (GameObject)PrefabUtility.InstantiatePrefab(carPrefab);
        car.name = "Car";
        car.transform.position = new Vector3(0f, 0.3f, 0f);
        CarController controller = car.GetComponent<CarController>();

        // Camera
        GameObject camGo = new GameObject("Main Camera");
        camGo.tag = "MainCamera";
        camGo.transform.position = new Vector3(0f, 3.1f, -6.0f);
        camGo.transform.rotation = Quaternion.Euler(18f, 0f, 0f);
        Camera cam = camGo.AddComponent<Camera>();
        camGo.AddComponent<AudioListener>();
        controller.targetCamera = cam;

        // UI Canvas (using TMP_Text)
        CreateEventSystem();
        Canvas canvas = CreateCanvas("ServerUI");

        TMP_Text ipText = CreateTMPText(canvas.transform, "IPText", "Server IP: Detecting...", 20, new Vector2(20f, -20f), new Vector2(650f, 35f), Color.white);
        TMP_Text statusText = CreateTMPText(canvas.transform, "StatusText", "Waiting for client...", 18, new Vector2(20f, -60f), new Vector2(650f, 35f), Color.yellow);

        // Server Manager
        GameObject serverGo = new GameObject("ServerManager");
        CarServer server = serverGo.AddComponent<CarServer>();
        server.carController = controller;
        server.ipAddressText = ipText;
        server.statusText = statusText;

        EditorSceneManager.SaveScene(scene, ServerScenePath);
    }

    private static void CreateClientScene()
    {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

        // Camera
        GameObject camGo = new GameObject("Main Camera");
        Camera cam = camGo.AddComponent<Camera>();
        cam.clearFlags = CameraClearFlags.SolidColor;
        cam.backgroundColor = new Color(0.1f, 0.12f, 0.16f);

        CreateEventSystem();
        Canvas canvas = CreateCanvas("ClientUI");

        GameObject clientManager = new GameObject("ClientManager");
        CarClient client = clientManager.AddComponent<CarClient>();

        // Top Bar: IP Input + Connect Button + Status
        CreateTMPText(canvas.transform, "Title", "CAR CONTROLLER (ANDROID CLIENT)", 22, new Vector2(20f, -15f), new Vector2(500f, 30f), Color.cyan);

        TMP_InputField ipInput = CreateTMPInputField(canvas.transform, "IPInput", "127.0.0.1", new Vector2(20f, -55f), new Vector2(260f, 45f));
        Button connectBtn = CreateButton(canvas.transform, "ConnectBtn", "Connect", new Vector2(290f, -55f), new Vector2(130f, 45f), new Color(0.2f, 0.6f, 0.3f));
        TMP_Text statusText = CreateTMPText(canvas.transform, "StatusText", "Disconnected", 18, new Vector2(435f, -60f), new Vector2(500f, 35f), Color.white);

        UnityEventTools.AddPersistentListener(connectBtn.onClick, client.ConnectToServer);

        client.ipInputField = ipInput;
        client.connectButton = connectBtn;
        client.statusText = statusText;

        // Bottom Left: Steering Controls (Element 1)
        CreateTMPText(canvas.transform, "SteerLabel", "STEERING", 18, new Vector2(40f, -140f), new Vector2(200f, 30f), Color.white);

        HoldButton leftBtn = CreateHoldButton(canvas.transform, "BtnLeft", "<< LEFT", new Vector2(30f, -180f), new Vector2(140f, 160f), new Color(0.2f, 0.3f, 0.45f));
        HoldButton rightBtn = CreateHoldButton(canvas.transform, "BtnRight", "RIGHT >>", new Vector2(180f, -180f), new Vector2(140f, 160f), new Color(0.2f, 0.3f, 0.45f));

        client.steerLeftButton = leftBtn;
        client.steerRightButton = rightBtn;

        // Bottom Right: Throttle & Brake Pedals (Elements 2 & 3)
        HoldButton brakePedal = CreateHoldButton(canvas.transform, "BrakePedal", "BRAKE", new Vector2(620f, -180f), new Vector2(140f, 160f), new Color(0.6f, 0.2f, 0.2f));
        HoldButton gasPedal = CreateHoldButton(canvas.transform, "GasPedal", "GAS", new Vector2(770f, -180f), new Vector2(140f, 160f), new Color(0.2f, 0.6f, 0.2f));

        client.brakePedal = brakePedal;
        client.gasPedal = gasPedal;

        // Center Panel: Car Parts (Elements 4 & 5)
        CreateTMPText(canvas.transform, "PartsLabel", "CAR PARTS", 18, new Vector2(370f, -140f), new Vector2(200f, 30f), Color.white);

        Button hlBtn = CreateButton(canvas.transform, "HeadlightsBtn", "Headlights: OFF", new Vector2(340f, -180f), new Vector2(220f, 70f), new Color(0.25f, 0.35f, 0.5f));
        UnityEventTools.AddPersistentListener(hlBtn.onClick, client.ToggleHeadlights);
        client.headlightsButton = hlBtn;
        client.headlightsBtnText = hlBtn.GetComponentInChildren<TMP_Text>();

        Button doorBtn = CreateButton(canvas.transform, "DoorBtn", "Door: CLOSED", new Vector2(340f, -265f), new Vector2(220f, 70f), new Color(0.25f, 0.35f, 0.5f));
        UnityEventTools.AddPersistentListener(doorBtn.onClick, client.ToggleDoor);
        client.doorButton = doorBtn;
        client.doorBtnText = doorBtn.GetComponentInChildren<TMP_Text>();

        // Camera Feed Focus Buttons
        CreateTMPText(canvas.transform, "CamLabel", "CAMERA FOCUS", 18, new Vector2(370f, -360f), new Vector2(200f, 30f), Color.yellow);

        CreateCamFocusBtn(canvas.transform, "CamChase", "Chase Cam", 0, client, new Vector2(50f, -400f), new Vector2(130f, 50f));
        CreateCamFocusBtn(canvas.transform, "CamSteer", "Steering", 1, client, new Vector2(190f, -400f), new Vector2(130f, 50f));
        CreateCamFocusBtn(canvas.transform, "CamWheels", "Wheels", 2, client, new Vector2(330f, -400f), new Vector2(130f, 50f));
        CreateCamFocusBtn(canvas.transform, "CamBrakes", "Brakes", 3, client, new Vector2(470f, -400f), new Vector2(130f, 50f));
        CreateCamFocusBtn(canvas.transform, "CamLights", "Headlights", 4, client, new Vector2(610f, -400f), new Vector2(130f, 50f));
        CreateCamFocusBtn(canvas.transform, "CamDoor", "Driver Door", 5, client, new Vector2(750f, -400f), new Vector2(130f, 50f));

        EditorSceneManager.SaveScene(scene, ClientScenePath);
    }

    private static void CreateCamFocusBtn(Transform parent, string name, string label, int focusIndex, CarClient client, Vector2 pos, Vector2 size)
    {
        Button btn = CreateButton(parent, name, label, pos, size, new Color(0.2f, 0.25f, 0.35f));
        UnityEventTools.AddIntPersistentListener(btn.onClick, client.SetCameraFocus, focusIndex);
    }

    private static Canvas CreateCanvas(string name)
    {
        GameObject go = new GameObject(name);
        Canvas canvas = go.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        CanvasScaler scaler = go.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(960f, 540f);
        go.AddComponent<GraphicRaycaster>();
        return canvas;
    }

    private static void CreateEventSystem()
    {
        GameObject go = new GameObject("EventSystem");
        go.AddComponent<EventSystem>();
        go.AddComponent<InputSystemUIInputModule>();
    }

    private static TMP_FontAsset GetTMPFont()
    {
        if (TMP_Settings.defaultFontAsset != null)
            return TMP_Settings.defaultFontAsset;

        return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(
            "Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
    }

    private static TMP_Text CreateTMPText(
        Transform parent,
        string name,
        string content,
        float fontSize,
        Vector2 pos,
        Vector2 size,
        Color color,
        TextAlignmentOptions alignment = TextAlignmentOptions.MidlineLeft)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        TextMeshProUGUI tmp = go.GetComponent<TextMeshProUGUI>();
        TMP_FontAsset font = GetTMPFont();
        if (font != null) tmp.font = font;
        tmp.text = content;
        tmp.fontSize = fontSize;
        tmp.color = color;
        tmp.alignment = alignment;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static Button CreateButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = color;
        Button btn = go.GetComponent<Button>();

        CreateTMPText(go.transform, "Label", label, 16f, Vector2.zero, size, Color.white, TextAlignmentOptions.Center);
        return btn;
    }

    private static HoldButton CreateHoldButton(Transform parent, string name, string label, Vector2 pos, Vector2 size, Color color)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(HoldButton));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = color;
        HoldButton hb = go.GetComponent<HoldButton>();

        CreateTMPText(go.transform, "Label", label, 18f, Vector2.zero, size, Color.white, TextAlignmentOptions.Center);
        return hb;
    }

    private static TMP_InputField CreateTMPInputField(Transform parent, string name, string defaultText, Vector2 pos, Vector2 size)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(TMP_InputField));
        go.transform.SetParent(parent, false);
        RectTransform rt = go.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0f, 1f);
        rt.anchorMax = new Vector2(0f, 1f);
        rt.pivot = new Vector2(0f, 1f);
        rt.anchoredPosition = pos;
        rt.sizeDelta = size;

        go.GetComponent<Image>().color = new Color(0.2f, 0.22f, 0.28f);
        TMP_InputField input = go.GetComponent<TMP_InputField>();

        GameObject textArea = new GameObject("Text Area", typeof(RectTransform), typeof(RectMask2D));
        textArea.transform.SetParent(go.transform, false);
        RectTransform areaRt = textArea.GetComponent<RectTransform>();
        areaRt.anchorMin = Vector2.zero;
        areaRt.anchorMax = Vector2.one;
        areaRt.offsetMin = new Vector2(10f, 4f);
        areaRt.offsetMax = new Vector2(-10f, -4f);

        TMP_Text placeholder = CreateTMPText(textArea.transform, "Placeholder", "Enter IP...", 16f, Vector2.zero, size - new Vector2(20f, 8f), Color.gray);
        TMP_Text textComp = CreateTMPText(textArea.transform, "Text", defaultText, 18f, Vector2.zero, size - new Vector2(20f, 8f), Color.white);

        input.textViewport = areaRt;
        input.textComponent = textComp;
        input.placeholder = (Graphic)placeholder;
        input.fontAsset = GetTMPFont();
        input.text = defaultText;

        return input;
    }
}
#endif
