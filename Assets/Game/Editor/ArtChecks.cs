using System;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SecondChance.Editor
{
    public static class ArtChecks
    {
        public static string Run()
        {
            int states = 0;
            foreach (var name in new[] { "Punk", "Swat", "Suit" })
            {
                var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Game/Prefabs/"+name+".prefab");
                if (prefab == null || prefab.GetComponent<BodyAnimation>() == null) throw new Exception("Incomplete character: "+name);
                var animator = prefab.GetComponentInChildren<Animator>();
                var controller = animator.runtimeAnimatorController as AnimatorController;
                if (controller == null || animator.applyRootMotion) throw new Exception("Invalid animation setup: "+name);
                foreach (var state in controller.layers[0].stateMachine.states)
                {
                    if (state.state.motion == null || state.state.motion.name.StartsWith("__preview__")) throw new Exception("Non-runtime clip on "+name+" / "+state.state.name);
                    states++;
                }
                foreach (var renderer in prefab.GetComponentsInChildren<SkinnedMeshRenderer>())
                    foreach (var bone in renderer.bones) if (bone == null || !bone.IsChildOf(prefab.transform)) throw new Exception("Broken weapon/character bone: "+name);
            }
            return states+" runtime animation states and 3 complete character rigs validated";
        }
    }
}
