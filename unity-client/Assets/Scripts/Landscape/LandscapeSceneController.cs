using System.Collections.Generic;
using EmoScape.Bootstrap;
using EmoScape.Networking;
using EmoScape.Shared;
using UnityEngine;
using UnityEngine.UIElements;

namespace EmoScape.Landscape
{
    /// <summary>
    /// Root of Landscape.unity. Builds the whole scene procedurally (camera, lights,
    /// particles, nebula, emotional spline, UI) at runtime — direct port of
    /// frontend/index.html's top-level script.
    /// </summary>
    public class LandscapeSceneController : MonoBehaviour
    {
        Camera cam;
        EmotionalSplineBuilder splineBuilder;
        NodeTooltipController tooltip;
        OrbitCameraRig orbitRig;
        TopBarController topBar;
        ReflectDrawerController reflectDrawer;
        DiaryPanelController diaryPanel;
        VisualElement legend;
        List<SessionDto> sessions = new();

        void Start()
        {
            BuildCamera();
            BuildLighting();
            BuildBackground();
            BuildSpline();
            BuildUI();
            _ = LoadSessionsAsync();
        }

        void BuildCamera()
        {
            var camGo = new GameObject("Main Camera");
            cam = camGo.AddComponent<Camera>();
            cam.tag = "MainCamera";
            camGo.AddComponent<AudioListener>();
            cam.fieldOfView = 55f;
            cam.nearClipPlane = 0.1f;
            cam.farClipPlane = 1000f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = SessionColorUtil.HexColor(0x020208);
            camGo.transform.position = new Vector3(0, 8, 28);
            camGo.transform.LookAt(Vector3.zero);

            orbitRig = camGo.AddComponent<OrbitCameraRig>();
            PostProcessingBootstrapper.EnablePostProcessingOn(cam);

            FogSetup.Apply(SessionColorUtil.HexColor(0x020208), 0.015f);
        }

        void BuildLighting()
        {
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = SessionColorUtil.HexColor(0x0d0d2e);

            CreatePointLight("KeyLight", 0x7c3aed, 4f, 60f, new Vector3(0, 15, 0));
            CreatePointLight("FillLightA", 0x4f46e5, 2f, 40f, new Vector3(-20, 5, -10));
            CreatePointLight("FillLightB", 0x6d28d9, 2f, 40f, new Vector3(20, 5, 10));
        }

        void CreatePointLight(string name, int hexColor, float intensity, float range, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(transform, false);
            go.transform.position = pos;
            var light = go.AddComponent<Light>();
            light.type = LightType.Point;
            light.color = SessionColorUtil.HexColor(hexColor);
            light.intensity = intensity;
            light.range = range;
        }

        void BuildBackground()
        {
            var particlesGo = new GameObject("Particles");
            particlesGo.transform.SetParent(transform, false);
            var particles = particlesGo.AddComponent<ParticleFieldBuilder>();
            particles.BuildAmbientField(5500, new Vector3(240, 140, 240), 0.67f, 0.2f, 0.78f, 0.42f, 0.28f, 0.2f, 1.8f);
            particles.BuildStarField(600, new Vector3(300, 200, 300), SessionColorUtil.HexColor(0x8866cc), 0.8f, 0.5f);

            var nebulaGo = new GameObject("Nebula");
            nebulaGo.transform.SetParent(transform, false);
            nebulaGo.AddComponent<NebulaBackgroundBuilder>().Build();
        }

        void BuildSpline()
        {
            var splineGo = new GameObject("EmotionalSpline");
            splineGo.transform.SetParent(transform, false);
            splineBuilder = splineGo.AddComponent<EmotionalSplineBuilder>();
            splineBuilder.SetCamera(cam);
        }

        void BuildUI()
        {
            var panelSettings = ScriptableObject.CreateInstance<PanelSettings>();
            panelSettings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            panelSettings.referenceResolution = new Vector2Int(1920, 1080);
            // Required: a runtime PanelSettings with no theme stylesheet builds its visual
            // tree but never actually renders it ("No Theme Style Sheet set to PanelSettings").
            panelSettings.themeStyleSheet = Resources.Load<ThemeStyleSheet>("UI/UnityDefaultRuntimeTheme");

            var uiGo = new GameObject("UI");
            uiGo.transform.SetParent(transform, false);
            var doc = uiGo.AddComponent<UIDocument>();
            doc.panelSettings = panelSettings;
            doc.visualTreeAsset = Resources.Load<VisualTreeAsset>("UI/Landscape/MainUI");

            var root = doc.rootVisualElement;
            root.style.flexGrow = 1; // UIDocument's root does not auto-stretch to fill the panel
            root.style.width = new Length(100, LengthUnit.Percent);
            root.style.height = new Length(100, LengthUnit.Percent);
            legend = root.Q<VisualElement>("legend");

            topBar = new TopBarController(root, ShowLandscape, ShowReflect, ShowDiary, AppRoot.GoToRoom);
            reflectDrawer = new ReflectDrawerController(root, OnSessionEnded);
            diaryPanel = new DiaryPanelController(root);
            tooltip = new NodeTooltipController(root, cam);
        }

        void ShowLandscape()
        {
            reflectDrawer.Close();
            diaryPanel.Close();
        }

        void ShowReflect()
        {
            diaryPanel.Close();
            reflectDrawer.Open();
        }

        void ShowDiary()
        {
            reflectDrawer.Close();
            diaryPanel.Open(sessions);
        }

        async void OnSessionEnded() => await LoadSessionsAsync();

        async System.Threading.Tasks.Task LoadSessionsAsync()
        {
            try
            {
                sessions = await ApiClient.Instance.GetSessionsAsync();
            }
            catch
            {
                sessions = new List<SessionDto>();
            }
            splineBuilder.BuildSpline(sessions);
            topBar.SetSessionCount(sessions.Count);
            legend.style.display = sessions.Count > 0 ? DisplayStyle.Flex : DisplayStyle.None;
        }

        void Update()
        {
            tooltip?.Tick(orbitRig != null && orbitRig.IsDragging);
        }
    }
}
