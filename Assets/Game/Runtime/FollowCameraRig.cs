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
            float halfWidth = output.orthographicSize * output.aspect;
            float halfDepth = output.orthographicSize / (16f / Mathf.Sqrt(16 * 16 + 11 * 11));
            float x = Mathf.Max(0, settings.ArenaHalfSize.x - halfWidth);
            float z = Mathf.Max(0, settings.ArenaHalfSize.y - halfDepth);
            return new Vector3(Mathf.Clamp(position.x, -x, x), 0, Mathf.Clamp(position.z, -z, z));
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
