using System;
using UnityEngine;

namespace SecondChance
{
    public enum BodyKind { Duelist, Brute, Gunner }
    public enum RunPhase { Ready, Fighting, Possessing, Intermission, Won, Lost }
    // Surge: possession-chain bonus started. BossShift: the boss soul entered Body (Direction = travel from the old host).
    public enum CombatCue { Windup, Attack, Hit, Death, Dash, Possessed, Surge, BossShift }

    [Serializable]
    public sealed class BodyDefinition
    {
        public BodyKind Kind;
        public string Label;
        public float Health = 60, Speed = 5, Damage = 20, Range = 2;
        public float AttackCooldown = .5f, PlayerWindup = .08f, EnemyWindup = .45f;
        public float Arc = 85, Radius = .45f;
        [Tooltip("Knockback speed this body's hits give the target.")]
        public float Push = 6;
        [Tooltip("Dash cooldown multiplier while the player controls this body.")]
        public float DashCooldownScale = 1;
        [Tooltip("Hits never interrupt this body's attack and push it less.")]
        public bool Unstoppable;
        public Color Color = Color.white;
        [Tooltip("Optional visual only. Forward is +Z; origin at feet. No gameplay scripts required.")]
        public GameObject VisualPrefab;
    }

    [Serializable]
    public sealed class WaveDefinition
    {
        public int Duelists, Brutes, Gunners;
        public int Count => Duelists + Brutes + Gunners;
    }

    [CreateAssetMenu(menuName = "Second Chance/Game Settings")]
    public sealed class GameSettings : ScriptableObject
    {
        public Vector2 ArenaHalfSize = new Vector2(15, 10);
        public float RunDuration = 300, MarkDuration = 8, PossessionRadius = 9;
        public float PossessionSeconds = 2, DeathSlowMotion = .06f, PossessionGrace = .8f;
        public float DashDuration = .18f, DashCooldown = 1.05f, DashSpeed = 17;
        public float HitStop = .045f, EnemyDamageMultiplier = .55f;
        public float WaveBreak = 4, ProjectileSpeed = 14;
        public int FirstWaveSize = 4, WaveGrowth = 2, MaxWaveSize = 20;
        public bool UseRooms, UseVision;
        [Min(2)] public float CameraSize = 5.6f;
        [Range(0,1)] public float CameraDamping = .2f;
        [Header("Room dressing")]
        public GameObject VentProp, PipeProp, TerminalProp, StreetLightProp;
        public GameObject CrateProp, TankProp, ScreenProp, ConsoleProp, CableProp, PanelLightProp, MedicalProp;
        [Range(30,180)] public float VisionAngle = 110;
        public float VisionDistance = 24;
        [Min(1)] public int RoomCount = 3, WavesPerRoom = 2;

        [Header("Combat feel")]
        [Tooltip("Seconds an ordinary enemy flinches after a player hit; its attack is cancelled.")]
        public float HitStagger = .22f;
        [Tooltip("Hit stop for kills and heavy (Brute) hits.")]
        public float HeavyHitStop = .09f;
        public float KnockbackDecay = 30;

        [Header("Possession chain")]
        [Tooltip("Simulation seconds after a possession in which a kill starts the surge.")]
        public float ChainWindow = 2;
        public float SurgeDuration = 5, SurgeKillExtension = 1.5f;
        public float SurgeDamage = 1.5f, SurgeSpeed = 1.25f;

        [Header("Breakable props (room mode)")]
        public int CratesPerRoom = 5;
        public int CanistersPerRoom = 3;
        public float ExplosionRadius = 3.2f, ExplosionDamage = 35, ExplosionSelfDamage = 15;

        [Header("Boss (after the last room wave)")]
        public bool UseBoss;
        [Min(1)] public int BossLives = 3;
        public float BossHealthScale = 2.2f, BossDamageScale = 1.25f;
        public int BossEscorts = 3;

        [Header("Authored waves (room mode). Empty = FirstWaveSize/WaveGrowth formula")]
        public WaveDefinition[] Waves =
        {
            new WaveDefinition { Duelists = 3 },
            new WaveDefinition { Duelists = 3, Gunners = 1 },
            new WaveDefinition { Duelists = 2, Brutes = 1, Gunners = 2 },
            new WaveDefinition { Duelists = 3, Brutes = 2, Gunners = 2 },
            new WaveDefinition { Duelists = 3, Brutes = 2, Gunners = 3 },
            new WaveDefinition { Duelists = 4, Brutes = 3, Gunners = 3 }
        };

        public BodyDefinition[] Bodies =
        {
            new BodyDefinition { Kind = BodyKind.Duelist, Label = "DUELIST", Health = 65, Speed = 6, Damage = 24, Range = 2.25f, AttackCooldown = .36f, EnemyWindup = .4f, Arc = 80, DashCooldownScale = .7f, Color = new Color(.15f,.8f,.85f) },
            new BodyDefinition { Kind = BodyKind.Brute, Label = "BRUTE", Health = 135, Speed = 3.5f, Damage = 46, Range = 2.8f, AttackCooldown = .95f, PlayerWindup = .18f, EnemyWindup = .85f, Arc = 145, Radius = .65f, Push = 12, Unstoppable = true, Color = new Color(.95f,.45f,.15f) },
            new BodyDefinition { Kind = BodyKind.Gunner, Label = "GUNNER", Health = 40, Speed = 4.8f, Damage = 20, Range = 12, AttackCooldown = .42f, EnemyWindup = .8f, Push = 4, Color = new Color(.7f,.35f,.95f) }
        };

        public BodyDefinition GetBody(BodyKind kind)
        {
            foreach (var body in Bodies) if (body != null && body.Kind == kind) return body;
            throw new InvalidOperationException("Missing body definition: " + kind);
        }

        private void OnValidate()
        {
            RunDuration = Mathf.Max(1, RunDuration);
            PossessionSeconds = Mathf.Max(.1f, PossessionSeconds);
            PossessionRadius = Mathf.Max(1, PossessionRadius);
            DeathSlowMotion = Mathf.Clamp(DeathSlowMotion, .01f, .5f);
            DashDuration = Mathf.Max(.05f, DashDuration);
            DashCooldown = Mathf.Max(DashDuration, DashCooldown);
            KnockbackDecay = Mathf.Max(1, KnockbackDecay);
            BossLives = Mathf.Max(1, BossLives);
            BossHealthScale = Mathf.Max(.1f, BossHealthScale);
        }
    }

    public sealed class BodyState
    {
        public int Id { get; internal set; }
        public BodyDefinition Definition { get; internal set; }
        public Vector3 Position { get; internal set; }
        public Vector3 Facing { get; internal set; } = Vector3.forward;
        public float Health { get; internal set; }
        // Equals Definition.Health except for a boss host.
        public float MaxHealth { get; internal set; }
        public bool IsPlayer { get; internal set; }
        public bool IsBoss { get; internal set; }
        public bool Alive => Health > 0;
        public float MarkUntil { get; internal set; }
        public float Windup { get; internal set; }
        public float Stagger { get; internal set; }
        internal float AttackCooldown, Invulnerable, DashTime, DashCooldown;
        internal Vector3 AttackDirection, DashDirection, Knockback;
        internal Vector3 LastKnownPlayer;
        internal float AlertUntil;
    }

    // Breakable crate or explosive canister. Soft obstacle: pushes bodies out, stops shots, never blocks sight or routing.
    public sealed class PropState
    {
        public int Id { get; internal set; }
        public Vector3 Position { get; internal set; }
        public float Radius { get; internal set; }
        public float Health { get; internal set; }
        public bool Explosive { get; internal set; }
        public bool Alive => Health > 0;
    }

    public sealed class ShotState
    {
        public Vector3 Position;
        internal Vector3 Direction;
        internal float Remaining = 2;
        internal BodyState Source;
        internal bool PlayerShot;
        internal float Damage;
    }

    public readonly struct CombatEvent
    {
        public readonly CombatCue Cue;
        public readonly BodyState Body, Other;
        public readonly Vector3 Position, Direction;
        public readonly float Amount;
        public CombatEvent(CombatCue cue, BodyState body, Vector3 direction, BodyState other = null, float amount = 0)
        { Cue = cue; Body = body; Other = other; Position = body.Position; Direction = direction; Amount = amount; }
    }

    public struct PlayerCommand
    {
        public Vector3 Move, Aim;
        public bool Attack, Dash, Possess;
    }
}
