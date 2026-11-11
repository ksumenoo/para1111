using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace SecondChance.Editor
{
    public static class ArtSetup
    {
        private const string Models = "Assets/Game/ThirdParty/Quaternius/ModularMen/";
        private const string Cyber = "Assets/Game/ThirdParty/Quaternius/Cyberpunk/";
        private const string Industrial = "Assets/Game/ThirdParty/Kenney/Industrial/Models/FBX format/";
        private const string Prefabs = "Assets/Game/Prefabs/";

        public static string Apply()
        {
            if (EditorApplication.isPlaying) throw new InvalidOperationException("Stop Play Mode first.");
            if (SceneManager.GetActiveScene().path != JamSetup.ScenePath) throw new InvalidOperationException("Open Arena first.");
            if (GameObject.Find("Neon Rooftop") != null) throw new InvalidOperationException("Art is already installed; refusing duplicate decoration.");
            if (!(GraphicsSettings.currentRenderPipeline is UniversalRenderPipelineAsset)) throw new InvalidOperationException("URP must be active.");
            Ensure(Prefabs.TrimEnd('/')); Ensure("Assets/Game/Animation");
            Backup();
            var settings = AssetDatabase.LoadAssetAtPath<GameSettings>(JamSetup.SettingsPath);
            settings.GetBody(BodyKind.Duelist).VisualPrefab = Character("Punk",BodyKind.Duelist,2.5f);
            settings.GetBody(BodyKind.Brute).VisualPrefab = Character("Swat",BodyKind.Brute,2.875f);
            settings.GetBody(BodyKind.Gunner).VisualPrefab = Character("Suit",BodyKind.Gunner,2.4375f);
            EditorUtility.SetDirty(settings);
            Decorate();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            return "Three animated character prefabs assigned; neon rooftop decorated; original scene/settings backed up.";
        }

        private static void Backup()
        {
            const string backupScene = "Assets/Game/Scenes/Arena_BeforeArt.unity";
            const string backupSettings = "Assets/Game/Settings/GameSettings_BeforeArt.asset";
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(backupScene) != null) return;
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.CopyAsset(JamSetup.SettingsPath,backupSettings);
            AssetDatabase.CopyAsset(JamSetup.ScenePath,backupScene);
            var backup = EditorSceneManager.OpenScene(backupScene,OpenSceneMode.Additive);
            var scope = backup.GetRootGameObjects().SelectMany(g => g.GetComponentsInChildren<GameBootstrap>()).First();
            var serialized = new SerializedObject(scope);
            serialized.FindProperty("settings").objectReferenceValue = AssetDatabase.LoadAssetAtPath<GameSettings>(backupSettings);
            serialized.ApplyModifiedPropertiesWithoutUndo(); EditorUtility.SetDirty(scope);
            EditorSceneManager.SaveScene(backup); EditorSceneManager.CloseScene(backup,true);
        }

        private static GameObject Character(string name, BodyKind kind, float height)
        {
            string path = Models + "Individual Characters/FBX/" + name + ".fbx";
            var importer = (ModelImporter)AssetImporter.GetAtPath(path);
            var clips = importer.defaultClipAnimations;
            foreach (var clip in clips)
            {
                clip.loopTime = clip.name.Contains("Idle") || clip.name.Contains("Run") || clip.name.EndsWith("Walk");
                clip.lockRootHeightY = true; clip.lockRootPositionXZ = true; clip.lockRootRotation = true;
            }
            importer.clipAnimations = clips; importer.SaveAndReimport();
            var root = new GameObject(name + " Visual");
            try
            {
                var model = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path));
                model.transform.SetParent(root.transform,false);
                var meshes = model.GetComponentsInChildren<Renderer>();
                var bounds = meshes[0].bounds;
                foreach (var mesh in meshes) bounds.Encapsulate(mesh.bounds);
                float scale = height / Mathf.Max(.01f,bounds.size.y);
                model.transform.localScale *= scale;
                model.transform.localPosition = Vector3.up * (-bounds.min.y * scale);
                if (kind == BodyKind.Brute) model.transform.localScale = Vector3.Scale(model.transform.localScale,new Vector3(1.23f,1,1.12f));
                AttachWeapon(model,kind == BodyKind.Gunner ? "Pistol" : "Sword");
                foreach (var skin in model.GetComponentsInChildren<SkinnedMeshRenderer>()) skin.updateWhenOffscreen = true;
                var animator = model.GetComponent<Animator>();
                if (animator == null) animator = model.AddComponent<Animator>();
                animator.applyRootMotion = false;
                var imported = AssetDatabase.LoadAllAssetsAtPath(path).OfType<AnimationClip>().Where(c => !c.name.StartsWith("__preview__",StringComparison.Ordinal)).ToArray();
                AnimationClip Clip(string suffix) => imported.First(c => c.name.EndsWith("|" + suffix,StringComparison.Ordinal));
                string controllerPath = "Assets/Game/Animation/" + name + ".controller";
                var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                var machine = controller.layers[0].stateMachine;
                var idle = machine.AddState("Idle"); idle.motion = Clip(kind == BodyKind.Gunner ? "Idle_Gun_Pointing" : "Idle_Sword");
                var move = machine.AddState("Move"); move.motion = Clip(kind == BodyKind.Gunner ? "Run_Shoot" : "Run");
                var attack = machine.AddState("Attack"); attack.motion = Clip(kind == BodyKind.Gunner ? "Gun_Shoot" : "Sword_Slash");
                var death = machine.AddState("Death"); death.motion = Clip("Death"); machine.defaultState = idle;
                animator.runtimeAnimatorController = controller;
                var bridge = root.AddComponent<BodyAnimation>();
                var serialized = new SerializedObject(bridge);
                serialized.FindProperty("animator").objectReferenceValue = animator;
                serialized.FindProperty("attackClipLength").floatValue = attack.motion.averageDuration;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                return PrefabUtility.SaveAsPrefabAsset(root,Prefabs + name + ".prefab");
            }
            finally { Object.DestroyImmediate(root); }
        }

        private static void AttachWeapon(GameObject model, string weaponName)
        {
            var bones = model.GetComponentsInChildren<Transform>().GroupBy(t => t.name).ToDictionary(g=>g.Key,g=>g.First());
            var weapon = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Models + "Separate Skeletal Meshes and Animations/Weapons/" + weaponName + ".fbx"));
            weapon.transform.SetParent(model.transform,false);
            PrefabUtility.UnpackPrefabInstance(weapon,PrefabUnpackMode.Completely,InteractionMode.AutomatedAction);
            foreach (var skin in weapon.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skin.bones = skin.bones.Select(b => bones[b.name]).ToArray();
                if (skin.rootBone != null) skin.rootBone = bones[skin.rootBone.name];
            }
            foreach (var animator in weapon.GetComponentsInChildren<Animator>()) Object.DestroyImmediate(animator);
            var skeleton = weapon.GetComponentsInChildren<Transform>().FirstOrDefault(t => t.name == "CharacterArmature");
            if (skeleton != null) Object.DestroyImmediate(skeleton.gameObject);
        }

        private static void Decorate()
        {
            var parent = new GameObject("Neon Rooftop").transform;
            var floorMaterial = NewMaterial("Noir Floor",new Color(.045f,.055f,.075f));
            floorMaterial.SetFloat("_Metallic",.5f); floorMaterial.SetFloat("_Smoothness",.6f);
            GameObject.Find("Arena Floor").GetComponent<Renderer>().sharedMaterial = floorMaterial;
            var cyan = NewMaterial("Neon Cyan",new Color(.02f,.5f,.6f),new Color(.05f,1,1) * 3);
            var pink = NewMaterial("Neon Magenta",new Color(.5f,.02f,.2f),new Color(1,.04f,.4f) * 3);
            Neon(parent,"North cyan strip",new Vector3(0,.05f,9.9f),new Vector3(29.5f,.06f,.06f),cyan);
            Neon(parent,"South magenta strip",new Vector3(0,.05f,-9.9f),new Vector3(29.5f,.06f,.06f),pink);
            Neon(parent,"West cyan strip",new Vector3(-14.9f,.05f,0),new Vector3(.06f,.06f,19.5f),cyan);
            Neon(parent,"East magenta strip",new Vector3(14.9f,.05f,0),new Vector3(.06f,.06f,19.5f),pink);
            for (int i = 0; i < 5; i++)
            {
                string model = i % 2 == 0 ? "building-a" : "building-f";
                Prop(parent,Industrial+model+".fbx",new Vector3(-14 + i * 7,-1,16),new Vector3(0,180,0),(4 + i % 3) * .55f);
            }
            Prop(parent,Industrial+"building-c.fbx",new Vector3(-19,-1,4),Vector3.zero,4.9f);
            Prop(parent,Industrial+"building-f.fbx",new Vector3(19,-1,4),Vector3.zero,4.9f);
            Prop(parent,Industrial+"chimney-large.fbx",new Vector3(-20,-1,13),Vector3.zero,8);
            Prop(parent,Industrial+"shipping-container-a.fbx",new Vector3(19,0,-4),new Vector3(0,90,0),2);
            Prop(parent,Cyber+"Platforms/AC_Stacked.fbx",new Vector3(-17,0,-3),Vector3.zero,2.4f);
            Prop(parent,Cyber+"Platforms/Pipe_1.fbx",new Vector3(-17.5f,0,3),new Vector3(0,90,0),.4f);
            Prop(parent,Cyber+"Platforms/Computer.fbx",new Vector3(16.8f,0,-1),new Vector3(0,-90,0),1.8f);
            Prop(parent,Cyber+"Platforms/Antenna_1.fbx",new Vector3(17,0,9),Vector3.zero,3.5f);
            Prop(parent,Cyber+"Platforms/Sign_1.fbx",new Vector3(-8,1,11.5f),new Vector3(0,180,0),2.2f);
            Prop(parent,Cyber+"Platforms/Sign_2.fbx",new Vector3(8,1,11.5f),new Vector3(0,180,0),2.2f);
            RenderSettings.ambientLight = new Color(.4f,.45f,.6f);
            var sun = GameObject.Find("Arena Light").GetComponent<Light>(); sun.intensity = 1.35f; sun.color = new Color(.8f,.86f,1);
            Point(parent,"Cyan spill",new Vector3(-10,3,5),Color.cyan);
            Point(parent,"Magenta spill",new Vector3(10,3,-4),new Color(1,.05f,.35f));
            var camera = Camera.main;
            camera.GetUniversalAdditionalCameraData().renderPostProcessing = true;
            var volume = new GameObject("Noir grading").AddComponent<Volume>(); volume.transform.SetParent(parent,false); volume.isGlobal = true;
            var profile = ScriptableObject.CreateInstance<VolumeProfile>();
            AssetDatabase.CreateAsset(profile,"Assets/Game/Settings/NoirVolume.asset");
            var bloom = profile.Add<Bloom>(true); bloom.threshold.value = 1; bloom.intensity.value = .35f;
            var vignette = profile.Add<Vignette>(true); vignette.intensity.value = .2f;
            AssetDatabase.AddObjectToAsset(bloom,profile); AssetDatabase.AddObjectToAsset(vignette,profile);
            volume.sharedProfile = profile;
        }

        private static void Prop(Transform parent,string path,Vector3 position,Vector3 angles,float height)
        {
            var model = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (model == null) throw new InvalidOperationException("Missing prop: " + path);
            var instance = (GameObject)PrefabUtility.InstantiatePrefab(model); instance.transform.SetParent(parent,false);
            var renderers = instance.GetComponentsInChildren<Renderer>();
            var bounds = renderers[0].bounds; foreach (var renderer in renderers) bounds.Encapsulate(renderer.bounds);
            float scale = height / Mathf.Max(.01f,bounds.size.y);
            instance.transform.localScale *= scale;
            instance.transform.position = position - Vector3.up * bounds.min.y * scale;
            instance.transform.rotation = Quaternion.Euler(angles);
        }
        private static Material NewMaterial(string name,Color color,Color? emission = null)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { color = color };
            if (emission.HasValue) { material.EnableKeyword("_EMISSION"); material.SetColor("_EmissionColor",emission.Value); }
            AssetDatabase.CreateAsset(material,"Assets/Game/Materials/"+name+".mat"); return material;
        }
        private static void Neon(Transform parent,string name,Vector3 position,Vector3 size,Material material)
        {
            var item = GameObject.CreatePrimitive(PrimitiveType.Cube); item.name = name; item.transform.SetParent(parent,false);
            item.transform.position = position; item.transform.localScale = size; item.GetComponent<Renderer>().sharedMaterial = material;
            Object.DestroyImmediate(item.GetComponent<Collider>());
        }
        private static void Point(Transform parent,string name,Vector3 position,Color color)
        { var light = new GameObject(name).AddComponent<Light>(); light.transform.SetParent(parent,false); light.transform.position = position; light.type = LightType.Point; light.color = color; light.intensity = 8; light.range = 12; }
        private static void Ensure(string path)
        { if (!AssetDatabase.IsValidFolder(path)) { int slash = path.LastIndexOf('/'); Ensure(path.Substring(0,slash)); AssetDatabase.CreateFolder(path.Substring(0,slash),path.Substring(slash+1)); } }
    }
}
