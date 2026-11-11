using System;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace SecondChance.Editor
{
    public static class ReleasePreparation
    {
        public static void Build()
        {
            try
            {
                EditorSceneManager.OpenScene("Assets/Game/Scenes/Arena.unity");
                PlayerSettings.resizableWindow=true;PlayerSettings.defaultScreenWidth=1280;PlayerSettings.defaultScreenHeight=720;
                PlayerSettings.fullScreenMode=FullScreenMode.FullScreenWindow;PlayerSettings.allowFullscreenSwitch=true;
                PlayerSettings.runInBackground=false;PlayerSettings.bundleVersion="0.3.0";
                var settings=AssetDatabase.LoadAssetAtPath<GameSettings>("Assets/Game/Settings/GameSettings.asset");
                settings.MaxWaveSize=24;
                settings.Waves=new[] {
                    new WaveDefinition{Duelists=5,Gunners=1},new WaveDefinition{Duelists=6,Brutes=1,Gunners=2},
                    new WaveDefinition{Duelists=7,Brutes=2,Gunners=3},new WaveDefinition{Duelists=8,Brutes=3,Gunners=4},
                    new WaveDefinition{Duelists=10,Brutes=4,Gunners=4},new WaveDefinition{Duelists=12,Brutes=5,Gunners=5}};
                EditorUtility.SetDirty(settings);AssetDatabase.SaveAssets();
                ValidateHud(settings);ValidateWorld(settings);
                TeamValidation.Build();
            }
            catch(Exception exception){Debug.LogException(exception);EditorApplication.Exit(1);}
        }

        private static void ValidateHud(GameSettings settings)
        {
            var root=new GameObject("Release HUD check");var copy=UnityEngine.Object.Instantiate(settings);
            try
            {
                copy.WavesPerRoom=1;var game=new GameSession(copy);game.StartRun();
                var hud=root.AddComponent<DefaultHud>();hud.Initialize(game);
                typeof(GameSession).GetMethod("StartBoss",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,null);
                var canvas=root.GetComponentInChildren<Canvas>();canvas.GetComponent<UnityEngine.UI.CanvasScaler>().enabled=false;canvas.renderMode=RenderMode.WorldSpace;
                int checks=0;
                foreach(var size in new[]{new Vector2Int(800,600),new Vector2Int(1280,720),new Vector2Int(1920,1080),new Vector2Int(2560,1080),new Vector2Int(720,1280)})
                foreach(var phase in new[]{RunPhase.Ready,RunPhase.Possessing,RunPhase.Won,RunPhase.Lost,RunPhase.Fighting})
                {
                    typeof(GameSession).GetMethod("SetPhase",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{phase});
                    hud.Refresh(false);((RectTransform)canvas.transform).sizeDelta=size;hud.ApplyViewport(size.x,size.y,new Rect(0,0,size.x,size.y));
                    Canvas.ForceUpdateCanvases();
                    foreach(var text in root.GetComponentsInChildren<UnityEngine.UI.Text>())
                    {
                        if(string.IsNullOrWhiteSpace(text.text))continue;
                        var generation=text.GetGenerationSettings(text.rectTransform.rect.size);
                        text.cachedTextGenerator.Populate(text.text,generation);
                        int letters=text.text.Count(c=>!char.IsWhiteSpace(c));
                        if(text.cachedTextGenerator.characterCountVisible<letters)throw new Exception("Clipped HUD: "+text.name+" at "+size+" in "+phase);
                    }
                    checks++;
                }
                typeof(GameSession).GetMethod("SetPhase",System.Reflection.BindingFlags.Instance|System.Reflection.BindingFlags.NonPublic).Invoke(game,new object[]{RunPhase.Intermission});
                typeof(GameSession).GetProperty("AwaitingExit").SetValue(game,true);
                hud.Refresh(false);
                var arrow=root.GetComponentInChildren<ExitArrowGraphic>();
                if(arrow==null || !arrow.gameObject.activeInHierarchy)throw new Exception("Missing exit direction after clear");
                hud.Refresh(true);if(arrow.gameObject.activeSelf)throw new Exception("Exit guide must hide on pause");
                Debug.Log("Release HUD passed "+checks+" resolution/state combinations, exit guide and pause");
            }
            finally {UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(copy);}
        }

        private static void ValidateWorld(GameSettings settings)
        {
            var copy=UnityEngine.Object.Instantiate(settings);var root=new GameObject("Release room check");
            try
            {
                copy.EnemyDamageMultiplier=0;copy.RunDuration=10000;
                var game=new GameSession(copy);game.StartRun();
                if(game.Bodies.Count!=7 || settings.Waves[5].Count!=22)throw new Exception("Wrong release wave density");
                for(int i=game.Bodies.Count-1;i<22;i++)game.Spawn((BodyKind)(i%3),game.Layout.Spawns[i%game.Layout.Spawns.Count]);
                var watch=System.Diagnostics.Stopwatch.StartNew();
                for(int i=0;i<600;i++)game.Tick(1f/60,new PlayerCommand{Move=i%240<120 ? Vector3.right : Vector3.left,Aim=Vector3.forward*10});
                watch.Stop();
                root.AddComponent<RoomDressing>().Build(game.Layout,0,settings);
                int batched=root.GetComponentsInChildren<MeshRenderer>().Count(r=>r.isPartOfStaticBatch);
                Debug.Log("Release CPU simulation: 22 enemies, "+(watch.Elapsed.TotalMilliseconds/600).ToString("0.000")+" ms/tick; static batched renderers="+batched+" (GPU not measured)");
            }
            finally{UnityEngine.Object.DestroyImmediate(root);UnityEngine.Object.DestroyImmediate(copy);}
        }
    }
}
