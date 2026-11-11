using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SecondChance.Editor
{
    public static class JamSetup
    {
        public const string ScenePath = "Assets/Game/Scenes/Arena.unity";
        public const string SettingsPath = "Assets/Game/Settings/GameSettings.asset";

        [MenuItem("Second Chance/Create Arena (first time)")]
        public static void CreateArena()
        {
            if (EditorApplication.isPlaying) throw new System.InvalidOperationException("Stop Play Mode first.");
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(ScenePath) != null)
                throw new System.InvalidOperationException("Arena already exists; it will not be overwritten.");
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EnsureFolder("Assets/Game/Scenes"); EnsureFolder("Assets/Game/Settings"); EnsureFolder("Assets/Game/Materials");
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            if (settings == null) { settings = ScriptableObject.CreateInstance<GameSettings>(); AssetDatabase.CreateAsset(settings, SettingsPath); }
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            var floor = Material("Floor", new Color(.055f,.075f,.11f));
            var trim = Material("Arena Trim", new Color(.17f,.3f,.36f));
            var tile = Material("Floor Inlay", new Color(.09f,.12f,.17f));
            Cube("Arena Floor", new Vector3(0,-.3f,0), new Vector3(32,.5f,22), floor);
            Cube("North Boundary", new Vector3(0,.15f,10.5f), new Vector3(32,.6f,1), trim);
            Cube("South Boundary", new Vector3(0,.15f,-10.5f), new Vector3(32,.6f,1), trim);
            Cube("East Boundary", new Vector3(15.5f,.15f,0), new Vector3(1,.6f,20), trim);
            Cube("West Boundary", new Vector3(-15.5f,.15f,0), new Vector3(1,.6f,20), trim);
            for (int x = -12; x <= 12; x += 3) Cube("Floor Seam", new Vector3(x,-.035f,0), new Vector3(.035f,.015f,20), tile);
            for (int z = -9; z <= 9; z += 3) Cube("Floor Seam", new Vector3(0,-.035f,z), new Vector3(30,.015f,.035f), tile);
            var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0,26,-18);
            cameraObject.transform.LookAt(Vector3.zero);
            var camera = cameraObject.GetComponent<Camera>();
            camera.orthographic = true; camera.orthographicSize = 13.2f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(.025f,.035f,.06f);
            var sun = new GameObject("Arena Light", typeof(Light));
            sun.transform.rotation = Quaternion.Euler(55,-25,0);
            var light = sun.GetComponent<Light>(); light.type = LightType.Directional; light.intensity = 1.4f; light.shadows = LightShadows.Soft;
            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
            RenderSettings.ambientLight = new Color(.4f,.45f,.55f);
            var root = new GameObject("Game");
            root.AddComponent<CombatFeedback>();
            var scope = root.AddComponent<GameBootstrap>();
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameSettings>(SettingsPath);
            serialized.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(scope);
            var driver = new SerializedObject(root.GetComponent<GameDriver>());
            driver.FindProperty("gameCamera").objectReferenceValue = camera;
            driver.ApplyModifiedPropertiesWithoutUndo();
            EditorSceneManager.SaveScene(scene, ScenePath);
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
            Selection.activeObject = root;
        }

        private static void EnsureFolder(string path)
        { if (!AssetDatabase.IsValidFolder(path)) { int slash = path.LastIndexOf('/'); EnsureFolder(path.Substring(0,slash)); AssetDatabase.CreateFolder(path.Substring(0,slash), path.Substring(slash+1)); } }
        private static Material Material(string name, Color color)
        {
            var path = "Assets/Game/Materials/" + name + ".mat";
            var material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material == null) { material = new Material(Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard")) { color = color }; AssetDatabase.CreateAsset(material,path); }
            return material;
        }
        private static void Cube(string name, Vector3 position, Vector3 size, Material material)
        { var cube = GameObject.CreatePrimitive(PrimitiveType.Cube); cube.name = name; cube.transform.position = position; cube.transform.localScale = size; cube.GetComponent<Renderer>().sharedMaterial = material; }
    }
}
