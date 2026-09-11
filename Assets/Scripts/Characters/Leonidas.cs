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
            // DodgeController sets ControlsLocked during a slide
            if (!ControlsLocked)
                Move(input.MoveInput);
        }

        private void Update()
        {
            // WeaponHolder and DodgeController read their own input each Update.
            // Leonidas only needs to keep facing toward the mouse.
            FaceToward(input.AimWorldPosition);

            // Phase 6 stubs — Phalanx logs remain until those systems are implemented
            if (input.ActiveJustDown)
                UnityEngine.Debug.Log("[Leonidas] PHALANX ANCHOR — active ability (Phase 6)");
            if (input.PhalanxJustDown)
                UnityEngine.Debug.Log("[Leonidas] PHALANX — team ability (Phase 6)");
        }

    }
}
