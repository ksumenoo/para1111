using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SecondChance.Editor
{
    public static class RoomSetup
    {
        public static string Apply()
        {
            foreach(var name in new[] { "Punk","Swat","Suit" })
            {
                var clips=AssetDatabase.LoadAllAssetsAtPath("Assets/Game/ThirdParty/Quaternius/ModularMen/Individual Characters/FBX/"+name+".fbx")
                    .OfType<AnimationClip>().Where(c=>!c.name.StartsWith("__preview__")).ToArray();
                var controller=AssetDatabase.LoadAssetAtPath<AnimatorController>("Assets/Game/Animation/"+name+".controller");
                var machine=controller.layers[0].stateMachine;
                foreach(var direction in new[] { "Back","Left","Right" })
                {
                    string stateName="Move"+direction;
                    var state=machine.states.FirstOrDefault(s=>s.state.name==stateName).state;
                    if(state==null) state=machine.AddState(stateName);
                    state.motion=clips.First(c=>c.name.EndsWith("|Run_"+direction,StringComparison.Ordinal));
                    EditorUtility.SetDirty(state);
                }
                EditorUtility.SetDirty(controller);
            }
            var settings=AssetDatabase.LoadAssetAtPath<GameSettings>(JamSetup.SettingsPath);
            settings.UseRooms=true; settings.UseVision=true; settings.RoomCount=3; settings.WavesPerRoom=2;
            settings.VisionAngle=110; settings.VisionDistance=24;
            EditorUtility.SetDirty(settings); AssetDatabase.SaveAssets();
            return "Directional locomotion configured; three rooms and cursor-facing vision enabled";
        }
    }
}
