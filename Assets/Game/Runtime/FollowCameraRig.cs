using Unity.Cinemachine;
using Unity.Cinemachine.TargetTracking;
using UnityEngine;

namespace SecondChance
{
    // A stable target survives death and possession; it is never tied to a disposable BodyView.
    public sealed class FollowCameraRig : MonoBehaviour
    {
        private Camera output;
        private GameSession session;
        private GameSettings settings;
        private Transform target;
        private CinemachineCamera virtualCamera;
        private CameraCombatShake combatShake;
        private int room = -1;
        public Vector3 ShakeOffset { set { if (combatShake != null) combatShake.Offset = value; } }

        public void Initialize(Camera camera, GameSession game, GameSettings configuration)
        {
            output = camera; session = game; settings = configuration;
            output.orthographic = true;
            output.orthographicSize = Mathf.Max(2, settings.CameraSize);
            var brain = output.GetComponent<CinemachineBrain>();
            if (brain == null) brain = output.gameObject.AddComponent<CinemachineBrain>();
            brain.IgnoreTimeScale = true;
            brain.UpdateMethod = CinemachineBrain.UpdateMethods.LateUpdate;
            brain.DefaultBlend = new CinemachineBlendDefinition(CinemachineBlendDefinition.Styles.Cut, 0);
            target = new GameObject("Camera Follow Target").transform;
            target.SetParent(transform, false);
            var rig = new GameObject("Player Cinemachine Camera");
            rig.transform.SetParent(transform, false);
            rig.transform.rotation = Quaternion.LookRotation(new Vector3(0, -16, 11));
            virtualCamera = rig.AddComponent<CinemachineCamera>();
            virtualCamera.Follow = target;
            virtualCamera.Lens = LensSettings.FromCamera(output);
            var follow = rig.AddComponent<CinemachineFollow>();
            follow.FollowOffset = new Vector3(0, 16, -11);
            follow.TrackerSettings = TrackerSettings.Default;
            follow.TrackerSettings.BindingMode = BindingMode.WorldSpace;
            follow.TrackerSettings.PositionDamping = Vector3.one * settings.CameraDamping;
            combatShake = rig.AddComponent<CameraCombatShake>();
            Snap();
        }

        private Vector3 Center()
        {
            Vector3 position = session.Player != null ? session.Player.Position : Vector3.zero;
            // Follow to every edge. Clamping the viewport to the floor pushed the body
            // offscreen, particularly its head at the north wall. Exterior dressing
            // fills the extra view; a small forward offset leaves room above the head.
            return position + Vector3.forward * .8f;
        }

        public void Refresh()
        {
            if (session.Room != room) { Snap(); return; }
            target.position = Center();
        }

        public void Snap()
        {
            if (target == null) return;
            target.position = Center();
            room = session.Room;
            virtualCamera.PreviousStateIsValid = false;
        }
    }
}
