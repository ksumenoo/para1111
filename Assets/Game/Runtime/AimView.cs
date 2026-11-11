using UnityEngine;
using UnityEngine.InputSystem;

namespace SecondChance
{
    public sealed class AimView : MonoBehaviour
    {
        private LineRenderer cursor, reach;
        private bool initialCursorVisible;

        public void Initialize(Material material)
        {
            initialCursorVisible = Cursor.visible;
            cursor = CreateLine("Aim reticle", material, .04f);
            cursor.positionCount = 5;
            reach = CreateLine("Weapon reach", material, .025f);
        }

        public void Refresh(GameSession session, Vector3 aim, bool paused)
        {
            bool active = !paused && Mouse.current != null && session.Player != null && session.Player.Alive && session.Phase == RunPhase.Fighting;
            cursor.enabled = reach.enabled = active;
            Cursor.visible = !active || !Application.isFocused;
            if (!active) return;
            var player = session.Player;
            Color reticleColor = player.AttackCooldown <= 0 ? Color.white : new Color(.3f,.75f,.8f);
            cursor.startColor = cursor.endColor = reticleColor;
            const float radius = .2f;
            for (int i = 0; i < 5; i++)
            {
                float angle = i * Mathf.PI * .5f;
                cursor.SetPosition(i, aim + new Vector3(Mathf.Sin(angle) * radius,.1f,Mathf.Cos(angle) * radius));
            }
            var direction = player.Windup > 0 ? player.AttackDirection : player.Facing;
            Color rangeColor = new Color(.2f,.9f,1,player.Windup > 0 ? .7f : .25f);
            reach.startColor = reach.endColor = rangeColor;
            var origin = player.Position + Vector3.up * .09f;
            if (player.Definition.Kind == BodyKind.Gunner)
            {
                reach.positionCount = 2;
                reach.SetPosition(0,origin + direction * .8f);
                var end=origin + direction * player.Definition.Range;
                reach.SetPosition(1,Vector3.Lerp(origin,end,session.Layout.CastFraction(origin,end)));
            }
            else
            {
                reach.positionCount = 25;
                reach.SetPosition(0,origin);
                for (int i = 1; i < 24; i++)
                {
                    float angle = Mathf.Lerp(-player.Definition.Arc * .5f,player.Definition.Arc * .5f,(i - 1) / 22f);
                    reach.SetPosition(i,origin + Quaternion.AngleAxis(angle,Vector3.up) * direction * player.Definition.Range);
                }
                reach.SetPosition(24,origin);
            }
        }

        private LineRenderer CreateLine(string name, Material material, float width)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform,false);
            line.sharedMaterial = material; line.useWorldSpace = true; line.widthMultiplier = width;
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
            return line;
        }
        private void OnDestroy() { Cursor.visible = initialCursorVisible; }
    }
}
