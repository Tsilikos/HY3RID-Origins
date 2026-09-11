using UnityEngine;

namespace HY3RIDOrigins.Data
{
    // Locked stats per character. ScriptableObject assets mirror these values —
    // this file is the authoritative constant source so code can reference stats
    // without requiring an asset reference in every caller.
    public static class CharacterData
    {
        public static readonly Stats Leonidas = new Stats(
            id:          "leonidas",
            displayName: "Leonidas",
            role:        CharacterRole.Commander,
            teamColor:   new Color(0.788f, 0.635f, 0.153f), // Gold #C9A227
            maxHp:       150f,
            maxShield:   50f,
            moveSpeed:   5.5f,  // Unity units/sec (lab scale)
            radius:      0.40f,
            dodgeDist:   3.0f,
            dodgeDuration: 0.14f,
            dodgeCooldown: 0.85f,
            dodgeIFrames: 0.11f,
            damageReductionFront: 0.15f  // Passive: Resilience
        );

        public static readonly Stats Atomix = new Stats(
            id:          "atomix",
            displayName: "Atomix",
            role:        CharacterRole.DualWielder,
            teamColor:   new Color(0.310f, 0.765f, 0.973f), // Electric Blue #4FC3F7
            maxHp:       120f,
            maxShield:   30f,
            moveSpeed:   7.0f,
            radius:      0.36f,
            dodgeDist:   3.8f,
            dodgeDuration: 0.11f,
            dodgeCooldown: 0.70f,
            dodgeIFrames: 0.09f,
            damageReductionFront: 0f
        );

        public static readonly Stats Diana = new Stats(
            id:          "diana",
            displayName: "Diana",
            role:        CharacterRole.Hunter,
            teamColor:   new Color(0.702f, 0.898f, 0.988f), // Ice Blue #B3E5FC
            maxHp:       110f,
            maxShield:   40f,
            moveSpeed:   6.0f,
            radius:      0.34f,
            dodgeDist:   3.5f,
            dodgeDuration: 0.12f,
            dodgeCooldown: 0.75f,
            dodgeIFrames: 0.10f,
            damageReductionFront: 0f
        );

        public static readonly Stats Jester = new Stats(
            id:          "jester",
            displayName: "Jester",
            role:        CharacterRole.Demolitions,
            teamColor:   new Color(1.0f, 0.839f, 0.0f),    // Yellow #FFD600
            maxHp:       100f,
            maxShield:   35f,
            moveSpeed:   5.0f,
            radius:      0.34f,
            dodgeDist:   2.8f,
            dodgeDuration: 0.13f,
            dodgeCooldown: 0.90f,
            dodgeIFrames: 0.10f,
            damageReductionFront: 0f
        );

        public static readonly Stats Hex = new Stats(
            id:          "hex",
            displayName: "Hex",
            role:        CharacterRole.LoneWolf,
            teamColor:   new Color(0.267f, 0.267f, 0.267f), // Dark #444444
            maxHp:       130f,
            maxShield:   20f,
            moveSpeed:   6.5f,
            radius:      0.38f,
            dodgeDist:   3.2f,
            dodgeDuration: 0.13f,
            dodgeCooldown: 0.80f,
            dodgeIFrames: 0.10f,
            damageReductionFront: 0f
        );

        // Phase 4 enemy: basic ranged attacker.
        // dodge params all 0 — Getter Basic cannot dodge.
        public static readonly Stats GetterBasic = new Stats(
            id:          "getter_basic",
            displayName: "Getter Basic",
            role:        CharacterRole.Enemy,
            teamColor:   new Color(0.82f, 0.14f, 0.14f), // threat red
            maxHp:       80f,
            maxShield:   0f,
            moveSpeed:   3.5f,
            radius:      0.45f,
            dodgeDist:   0f,
            dodgeDuration: 0f,
            dodgeCooldown: 0f,
            dodgeIFrames: 0f,
            damageReductionFront: 0f
        );
    }

    public enum CharacterRole
    {
        Commander,
        DualWielder,
        Hunter,
        Demolitions,
        LoneWolf,
        Enemy,
    }

    [System.Serializable]
    public sealed class Stats
    {
        public readonly string Id;
        public readonly string DisplayName;
        public readonly CharacterRole Role;
        public readonly Color TeamColor;
        public readonly float MaxHp;
        public readonly float MaxShield;
        public readonly float MoveSpeed;
        public readonly float Radius;
        public readonly float DodgeDistance;
        public readonly float DodgeDuration;
        public readonly float DodgeCooldown;
        public readonly float DodgeInvulnerabilitySeconds;
        public readonly float DamageReductionFront;

        public Stats(
            string id, string displayName, CharacterRole role, Color teamColor,
            float maxHp, float maxShield, float moveSpeed, float radius,
            float dodgeDist, float dodgeDuration, float dodgeCooldown,
            float dodgeIFrames, float damageReductionFront)
        {
            Id                          = id;
            DisplayName                 = displayName;
            Role                        = role;
            TeamColor                   = teamColor;
            MaxHp                       = maxHp;
            MaxShield                   = maxShield;
            MoveSpeed                   = moveSpeed;
            Radius                      = radius;
            DodgeDistance               = dodgeDist;
            DodgeDuration               = dodgeDuration;
            DodgeCooldown               = dodgeCooldown;
            DodgeInvulnerabilitySeconds = dodgeIFrames;
            DamageReductionFront        = damageReductionFront;
        }
    }
}
