using Unity.Cinemachine;
using UnityEngine;

namespace SecondChance
{
    public sealed class CameraCombatShake : CinemachineExtension
    {
        public Vector3 Offset;
        protected override void PostPipelineStageCallback(CinemachineVirtualCameraBase camera,
            CinemachineCore.Stage stage, ref CameraState state, float deltaTime)
        {
            if (stage == CinemachineCore.Stage.Noise) state.PositionCorrection += Offset;
        }
    }
}
