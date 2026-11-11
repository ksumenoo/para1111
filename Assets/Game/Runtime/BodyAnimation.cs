using UnityEngine;

namespace SecondChance
{
    // Animation follows combat events; animation events never apply gameplay damage.
    public sealed class BodyAnimation : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private float attackClipLength = 1;
        private Vector3 lastPosition;
        private float windupDuration, recovery;
        private string current = "";

        public void Initialize(BodyState state)
        {
            lastPosition = state.Position;
            animator.applyRootMotion = false;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            animator.Rebind(); animator.Update(0);
            Play("Idle");
        }

        public void Refresh(BodyState state, float worldScale, bool paused)
        {
            bool moving = (state.Position - lastPosition).sqrMagnitude > .000001f;
            lastPosition = state.Position;
            if (paused || worldScale <= 0) { animator.speed = 0; return; }
            if (state.Windup > 0 && windupDuration > 0)
            {
                animator.speed = 0;
                animator.Play("Attack",0,Mathf.Clamp01(1 - state.Windup / windupDuration) * .35f);
                current = "Attack";
                return;
            }
            if (recovery > 0)
            {
                recovery -= Time.unscaledDeltaTime * worldScale;
                animator.speed = attackClipLength * .65f / .2f * worldScale;
                return;
            }
            animator.speed = worldScale;
            Play(moving ? "Move" : "Idle");
        }

        public void React(CombatEvent message)
        {
            if (message.Cue == CombatCue.Windup)
            {
                windupDuration = message.Body.Windup; recovery = 0;
                animator.Play("Attack",0,0); animator.speed = 0; current = "Attack";
            }
            if (message.Cue == CombatCue.Attack)
            {
                recovery = .2f;
                animator.Play("Attack",0,.35f); current = "Attack";
            }
            if (message.Cue == CombatCue.Possessed) { recovery = 0; windupDuration = 0; current = ""; Play("Idle"); }
        }

        public void PlayDeath()
        { animator.speed = 1; animator.Play("Death",0,0); }

        private void Play(string state)
        {
            if (current == state) return;
            animator.CrossFadeInFixedTime(state,.08f); current = state;
        }
    }
}
