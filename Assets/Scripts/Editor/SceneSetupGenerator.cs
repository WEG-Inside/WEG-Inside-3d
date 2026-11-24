using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static WegInside.EditorTools.MotorModelGenerator;

namespace WegInside.EditorTools
{
    public static class SceneSetupGenerator
    {
        public const string ScenePath = "Assets/Scenes/MotorExperience.unity";

        [MenuItem("WEG Inside/Gerar cena e motor")]
        public static void Generate()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Encerre o Play Mode para gerar a experiência.");
            for (int i = 0; i < SceneManager.sceneCount; i++)
                if (SceneManager.GetSceneAt(i).isDirty)
                    throw new InvalidOperationException("Salve as cenas abertas antes de gerar a experiência.");

            foreach (string name in new[] {"Scenes","Prefabs","Meshes","Materials","Data","UI"})
                if (!AssetDatabase.IsValidFolder("Assets/"+name)) AssetDatabase.CreateFolder("Assets",name);

            Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene,NewSceneMode.Single);
            GameObject prefab = MotorModelGenerator.Generate();
            if (!prefab) throw new InvalidOperationException("O prefab Motor_W22 não foi salvo.");
            SetupScene(scene,prefab);
            AssetDatabase.SaveAssets();
            if (!EditorSceneManager.SaveScene(scene,ScenePath))
                throw new InvalidOperationException("A cena MotorExperience não foi salva.");
            RegisterBuildScene();
        }

        static void SetupScene(Scene scene, GameObject prefab)
        {
            var motor = (GameObject)PrefabUtility.InstantiatePrefab(prefab,scene);
            motor.transform.position = Vector3.up * 1.343f;
            CreateStudio();

            var cameraNode = new GameObject("Câmera orbital");
            cameraNode.tag = "MainCamera";
            var camera = cameraNode.AddComponent<Camera>();
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = ColorFrom("#242a32");
            camera.fieldOfView = 35;
            camera.nearClipPlane = .07f;
            camera.farClipPlane = 90;
            camera.allowHDR = true;
            camera.allowMSAA = true;
            camera.GetUniversalAdditionalCameraData().antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            cameraNode.transform.SetPositionAndRotation(new Vector3(-4.4f,4.4f,-6.1f),Quaternion.Euler(23,36,0));
            var orbit = cameraNode.AddComponent<OrbitCameraController>();

            var systems = new GameObject("Controles da experiência");
            var controller = systems.AddComponent<MotorController>();
            Reference(controller,"motorRoot",motor.transform);
            Reference(controller,"orbit",orbit);
            Reference(systems.GetComponent<XRayController>(),"transparentHousing",Transparent);
            Reference(systems.GetComponent<PartHighlightController>(),"highlightMaterial",Highlight);

            var settings = SetupPanel();
            var uiObject = new GameObject("Interface WEG Inside");
            var document = uiObject.AddComponent<UIDocument>();
            document.panelSettings = settings;
            document.visualTreeAsset = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>("Assets/UI/MotorExperience.uxml");
            if (!document.visualTreeAsset)
                throw new InvalidOperationException("O UXML da experiência não foi importado.");
            var ui = uiObject.AddComponent<ExperienceUIController>();
            Reference(ui,"motor",controller);
            Reference(orbit,"motor",controller);
            Reference(orbit,"ui",ui);
        }

        static PanelSettings SetupPanel()
        {
            const string path = "Assets/UI/MotorPanelSettings.asset";
            var settings = AssetDatabase.LoadAssetAtPath<PanelSettings>(path);
            if (!settings)
            {
                settings = ScriptableObject.CreateInstance<PanelSettings>();
                AssetDatabase.CreateAsset(settings,path);
            }
            settings.themeStyleSheet = AssetDatabase.LoadAssetAtPath<ThemeStyleSheet>("Assets/UI/MotorExperience.tss");
            settings.scaleMode = PanelScaleMode.ScaleWithScreenSize;
            settings.referenceResolution = new Vector2Int(1920,1080);
            settings.screenMatchMode = PanelScreenMatchMode.MatchWidthOrHeight;
            settings.match = .45f;
            EditorUtility.SetDirty(settings);
            return settings;
        }

        static void CreateStudio()
        {
            var studio = new GameObject("Estúdio industrial").transform;
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Piso";
            ground.transform.SetParent(studio,false);
            ground.transform.localScale = new Vector3(15,1,15);
            UnityEngine.Object.DestroyImmediate(ground.GetComponent<Collider>());
            ground.GetComponent<MeshRenderer>().sharedMaterial = CreateMaterial("Studio_Floor","#20252b",.14f,.29f);

            Lighting(studio,"Luz principal",new Vector3(46,37,0),"#fff0df",1.58f,true);
            Lighting(studio,"Preenchimento",new Vector3(32,232,0),"#d7e9ff",.80f,false);
            Lighting(studio,"Luz de recorte",new Vector3(39,155,0),"#c4ddff",1.29f,false);
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = ColorFrom("#93a1b0");
            RenderSettings.ambientEquatorColor = ColorFrom("#64717d");
            RenderSettings.ambientGroundColor = ColorFrom("#37414c");
            RenderSettings.defaultReflectionMode = DefaultReflectionMode.Custom;
            RenderSettings.customReflectionTexture = StudioReflection();
            RenderSettings.reflectionIntensity = .74f;
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.Linear;
            RenderSettings.fogColor = ColorFrom("#242a32");
            RenderSettings.fogStartDistance = 28;
            RenderSettings.fogEndDistance = 60;
        }

        static void Lighting(Transform studio, string name, Vector3 direction, string color, float power, bool shadows)
        {
            var node = new GameObject(name);
            node.transform.SetParent(studio,false);
            node.transform.rotation = Quaternion.Euler(direction);
            var light = node.AddComponent<Light>();
            light.type = LightType.Directional;
            light.color = ColorFrom(color);
            light.intensity = power;
            if (shadows)
            {
                light.shadows = LightShadows.Soft;
                light.shadowStrength = .75f;
                light.shadowBias = .035f;
                light.shadowNormalBias = .18f;
                RenderSettings.sun = light;
            }
        }

        static Cubemap StudioReflection()
        {
            const string path = "Assets/Materials/Studio_Reflection.cubemap";
            var existing = AssetDatabase.LoadAssetAtPath<Cubemap>(path);
            if (existing) return existing;
            const int resolution = 64;
            var cube = new Cubemap(resolution,TextureFormat.RGBAHalf,true) { name = "Studio_Reflection", filterMode = FilterMode.Trilinear };
            var pixels = new Color[resolution*resolution];
            Vector3 primary = new Vector3(-.58f,.82f,-.44f).normalized;
            Vector3 fill = new Vector3(.62f,.54f,.70f).normalized;
            for (int face = 0; face < 6; face++)
            {
                for (int y = 0; y < resolution; y++)
                    for (int x = 0; x < resolution; x++)
                    {
                        float u = 2f*(x+.5f)/resolution-1, v = 2f*(y+.5f)/resolution-1;
                        Vector3 dir = face switch
                        {
                            0 => new(1,-v,-u), 1 => new(-1,-v,u), 2 => new(u,1,v),
                            3 => new(u,-1,-v), 4 => new(u,-v,1), _ => new(-u,-v,-1)
                        };
                        dir.Normalize();
                        float softbox = Mathf.Pow(Mathf.Max(0,Vector3.Dot(dir,primary)),9)*2.1f;
                        float bounce = Mathf.Pow(Mathf.Max(0,Vector3.Dot(dir,fill)),12)*1.6f;
                        pixels[y*resolution+x] = Color.Lerp(new Color(.055f,.064f,.079f),new Color(.28f,.32f,.38f),.5f+.5f*dir.y)
                            + new Color(1f,.92f,.78f)*softbox + new Color(.68f,.82f,1f)*bounce;
                    }
                cube.SetPixels(pixels,(CubemapFace)face);
            }
            cube.Apply(true);
            AssetDatabase.CreateAsset(cube,path);
            return cube;
        }

        static void Reference(UnityEngine.Object target, string field, UnityEngine.Object value)
        {
            var serialized = new SerializedObject(target);
            serialized.FindProperty(field).objectReferenceValue = value;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        static void RegisterBuildScene()
        {
            var entries = new List<EditorBuildSettingsScene>(EditorBuildSettings.scenes);
            entries.RemoveAll(entry => entry.path == ScenePath);
            entries.Insert(0,new EditorBuildSettingsScene(ScenePath,true));
            EditorBuildSettings.scenes = entries.ToArray();
        }
    }
}
