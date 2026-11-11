using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace SecondChance.Editor
{
    public static class TeamValidation
    {
        // Can also run in batch mode if the interactive Editor's asset indexing fails.
        public static void Build()
        {
            try
            {
                Debug.Log(CoreChecks.Run());Debug.Log(WorldChecks.Run());Debug.Log(FeelChecks.Run());
                var atlas=AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Game/ThirdParty/Quaternius/Cyberpunk/Textures/Texture_Sign.png");
                foreach(var id in AssetDatabase.FindAssets("Prop_ t:Material",new[]{"Assets/Game/Materials"}))
                {
                    var material=AssetDatabase.LoadAssetAtPath<Material>(AssetDatabase.GUIDToAssetPath(id));
                    if(!material.name.Contains("Texture_Signs"))continue;
                    material.mainTexture=atlas;material.EnableKeyword("_EMISSION");
                    material.SetTexture("_EmissionMap",atlas);material.SetColor("_EmissionColor",Color.white*.7f);EditorUtility.SetDirty(material);
                }
                AssetDatabase.SaveAssets();
                var report=BuildPipeline.BuildPlayer(new BuildPlayerOptions {
                    scenes=new[]{"Assets/Game/Scenes/Arena.unity"},
                    locationPathName="Builds/Windows/Game.exe",target=BuildTarget.StandaloneWindows64,
                    options=BuildOptions.None });
                var summary=report.summary;
                File.WriteAllText("Logs/team-build-result.json",JsonUtility.ToJson(new Result {
                    result=summary.result.ToString(),errors=summary.totalErrors,warnings=summary.totalWarnings },true));
                EditorApplication.Exit(summary.result==BuildResult.Succeeded ? 0 : 1);
            }
            catch(Exception exception) {Debug.LogException(exception);EditorApplication.Exit(1);}
        }
        [Serializable] private sealed class Result {public string result;public int errors,warnings;}
    }
}
