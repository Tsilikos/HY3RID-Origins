using UnityEngine;
using HY3RIDOrigins.Data;
using HY3RIDOrigins.Controllers;

namespace HY3RIDOrigins.Characters
{
    // Leonidas — the player-controlled Spartan.
    // Architecture note: this class is the ONLY player-driven character in the
    // vertical slice. Atomix/Diana/Jester/Hex reuse Character but drive via
    // AIController. When those become selectable, swap PlayerController in/out
    // without rewriting Character or weapon logic.
    public class Leonidas : Character
    {
        private PlayerController input;

        // Passive: 15% damage reduction from front-facing attacks (applied in Character.TakeDamage).
        // Already wired via CharacterData.Leonidas.DamageReductionFront = 0.15f.

        // Visual: direction arrow child. Created at runtime if not assigned.
        [SerializeField] private SpriteRenderer directionArrow;

        protected override void Awake()
        {
            base.Awake();
            input = GetComponent<PlayerController>();
            if (input == null)
                input = gameObject.AddComponent<PlayerController>();
        }

        protected override void Start()
        {
            // Initialize with locked stats from the authoritative data file.
            Initialize(CharacterData.Leonidas);
            base.Start();

            // Apply team color to sprite renderer.
            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.color = stats.TeamColor;

            if (directionArrow != null)
                directionArrow.color = new Color(stats.TeamColor.r * 0.6f, stats.TeamColor.g * 0.6f, stats.TeamColor.b * 0.6f);
        }

        private void FixedUpdate()
        {
            Move(input.MoveInput);
        }

        private void Update()
        {
            FaceToward(input.AimWorldPosition);

            // Log pending actions (implementations added in later phases).
            if (input.FireDown)
                UnityEngine.Debug.Log("[Leonidas] FIRE — pistol (Phase 2)");
            if (input.SpearDown)
                UnityEngine.Debug.Log("[Leonidas] SPEAR (Phase 2)");
            if (input.DodgeJustDown)
                UnityEngine.Debug.Log("[Leonidas] DODGE (Phase 2)");
            if (input.ActiveJustDown)
                UnityEngine.Debug.Log("[Leonidas] PHALANX ANCHOR — active ability (Phase 6)");
            if (input.PhalanxJustDown)
                UnityEngine.Debug.Log("[Leonidas] PHALANX — team ability (Phase 6)");
        }

        // Called by Phase 2 when a weapon fires to check front-facing damage reduction.
        public bool IsFacingToward(Vector2 attackerWorldPos)
        {
            Vector2 toAttacker = (attackerWorldPos - (Vector2)transform.position).normalized;
            Vector2 facing = new Vector2(
                Mathf.Cos(facingAngle),
                Mathf.Sin(facingAngle)
            );
            // Front-facing if the attacker is within ±60° of our facing direction.
            return Vector2.Dot(facing, toAttacker) > 0.5f;
        }
    }
}
