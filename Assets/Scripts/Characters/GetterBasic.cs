using System.Collections;
using UnityEngine;
using HY3RIDOrigins.Combat;
using HY3RIDOrigins.Data;
using HY3RIDOrigins.Rooms;

namespace HY3RIDOrigins.Characters
{
    // Phase 4 first enemy. Pure ranged attacker — no melee, no dash, no special ability.
    //
    // AI state machine (3 states):
    //   Idle    — target outside detectionRange; stands still
    //   Approach — target inside detectionRange but outside shootRange; walks toward target
    //   Shoot   — target inside shootRange; stops, aims, fires KineticPistol
    //
    // Reuses existing systems: IDamageable (Character base), KineticPistol, VisibilityLight.
    // AI calls pistol.TryFire() directly — no WeaponHolder needed (that's a player input router).
    [RequireComponent(typeof(KineticPistol))]
    public class GetterBasic : Character
    {
        [Header("AI")]
        [SerializeField] private float detectionRange = 15f;
        [SerializeField] private float shootRange     = 8f;

        private KineticPistol pistol;
        private Transform     target;
        private Vector2       pendingMove; // Set in Update, applied in FixedUpdate

        protected override void Start()
        {
            Initialize(CharacterData.GetterBasic);
            base.Start();

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null)
                sr.sprite = CharacterSpriteFactory.CreateEnemySprite(stats.TeamColor);

            pistol = GetComponent<KineticPistol>();

            // Auto-locate the player — decoupled from the spawner.
            target = FindAnyObjectByType<Leonidas>()?.transform;
        }

        private void Update()
        {
            if (target == null)
            {
                pendingMove = Vector2.zero;
                return;
            }

            var  toTarget = (Vector2)target.position - (Vector2)transform.position;
            float dist    = toTarget.magnitude;
            var  dir      = dist > 0.001f ? toTarget / dist : Vector2.up;

            if (dist > detectionRange)
            {
                pendingMove = Vector2.zero;
                return;
            }

            // Face toward target every frame while detected.
            FaceToward(target.position);

            if (dist > shootRange)
            {
                pendingMove = dir;
            }
            else
            {
                pendingMove = Vector2.zero;
                // TryFire is internally gated by fire-rate and reload — safe to call every frame.
                pistol?.TryFire(transform.position, dir);
            }
        }

        private void FixedUpdate()
        {
            if (pendingMove.sqrMagnitude > 0.01f)
                Move(pendingMove);
            else
                Stop();
        }

        protected override void OnDeath()
        {
            StartCoroutine(DeathFlash());
        }

        private IEnumerator DeathFlash()
        {
            enabled = false; // disable AI update
            pistol.enabled = false;

            var sr = GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = Color.white;

            yield return new WaitForSeconds(0.25f);
            Destroy(gameObject);
        }
    }
}
