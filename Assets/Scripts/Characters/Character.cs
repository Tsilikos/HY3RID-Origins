using UnityEngine;
using HY3RIDOrigins.Combat;
using HY3RIDOrigins.Data;

namespace HY3RIDOrigins.Characters
{
    // Base for Leonidas and all four AI companions.
    // Handles: movement application, health/shield, facing direction.
    // Does NOT handle input — that is the responsibility of PlayerController or AIController.
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(CircleCollider2D))]
    public class Character : MonoBehaviour, IDamageable
    {
        [Header("Definition")]
        [SerializeField] protected Stats stats;

        // Runtime state
        protected float currentHp;
        protected float currentShield;
        protected bool isInvulnerable;
        protected float facingAngle; // radians, 0 = right, PI/2 = up

        // Set by DodgeController (or any ability) to suppress normal movement input.
        public bool ControlsLocked { get; set; }

        protected Rigidbody2D rb;
        protected CircleCollider2D col;

        // Direction indicator (small triangle child object)
        protected Transform directionIndicator;

        // Called by subclasses or factory to inject stats when not set via Inspector
        public virtual void Initialize(Stats characterStats)
        {
            stats = characterStats;
            ApplyStatsToComponents();
        }

        protected virtual void Awake()
        {
            rb = GetComponent<Rigidbody2D>();
            col = GetComponent<CircleCollider2D>();

            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            directionIndicator = transform.Find("DirectionIndicator");

            if (stats != null)
                ApplyStatsToComponents();
        }

        protected virtual void Start()
        {
            if (stats == null)
            {
                UnityEngine.Debug.LogError($"[Character] {name} has no stats set. Call Initialize() or assign via Inspector.");
                return;
            }
            currentHp = stats.MaxHp;
            currentShield = stats.MaxShield;
        }

        private void ApplyStatsToComponents()
        {
            if (col != null)
                col.radius = stats.Radius;
        }

        // --- Movement API ---

        // Called by PlayerController or AIController each FixedUpdate.
        // dir: normalized movement direction in world space.
        public virtual void Move(Vector2 dir)
        {
            rb.linearVelocity = dir * stats.MoveSpeed;
        }

        public virtual void Stop()
        {
            rb.linearVelocity = Vector2.zero;
        }

        // --- Facing / Aim ---

        // angle: atan2(worldTarget - position), in radians.
        public virtual void FaceAngle(float angle)
        {
            facingAngle = angle;
            // Unity 2D: Z rotation. Sprite default faces +Y (up), so subtract 90deg.
            float degrees = angle * Mathf.Rad2Deg - 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, degrees);
        }

        public virtual void FaceToward(Vector2 worldTarget)
        {
            Vector2 dir = worldTarget - (Vector2)transform.position;
            if (dir.sqrMagnitude > 0.001f)
                FaceAngle(Mathf.Atan2(dir.y, dir.x));
        }

        // --- Health / Shield ---

        public virtual void TakeDamage(float amount, bool fromFront = false)
        {
            if (isInvulnerable) return;

            float reduced = amount;
            if (fromFront && stats.DamageReductionFront > 0f)
                reduced *= (1f - stats.DamageReductionFront);

            if (currentShield > 0f)
            {
                float shieldAbsorb = Mathf.Min(currentShield, reduced);
                currentShield -= shieldAbsorb;
                reduced -= shieldAbsorb;
            }

            currentHp = Mathf.Max(0f, currentHp - reduced);

            if (currentHp <= 0f)
                OnDeath();
        }

        public virtual void SetInvulnerable(bool value)
        {
            isInvulnerable = value;
        }

        // Grants invulnerability for exactly `duration` seconds, then clears it.
        // Used by DodgeController and any future ability with i-frames.
        public void StartTimedInvulnerability(float duration)
        {
            StartCoroutine(InvulnTimer(duration));
        }

        private System.Collections.IEnumerator InvulnTimer(float duration)
        {
            isInvulnerable = true;
            yield return new WaitForSeconds(duration);
            // Only clear if DebugConsole god-mode hasn't overridden it
            if (!HY3RIDOrigins.DevTools.DebugConsole.InvulnerabilityOn)
                isInvulnerable = false;
        }

        protected virtual void OnDeath()
        {
            // Subclasses override for death behavior.
            // Phase 1: no death system yet.
            gameObject.SetActive(false);
        }

        // --- Geometry helpers ---

        // True when attackerWorldPos is within ±60° of this character's facing direction.
        // Used by Projectile to determine whether the hit activates front-facing passives
        // (e.g. Leonidas' Resilience — 15% damage reduction from frontal attacks).
        public bool IsFacingToward(Vector2 attackerWorldPos)
        {
            Vector2 toAttacker = (attackerWorldPos - (Vector2)transform.position).normalized;
            Vector2 facing     = new Vector2(Mathf.Cos(facingAngle), Mathf.Sin(facingAngle));
            return Vector2.Dot(facing, toAttacker) > 0.5f; // true inside the ±60° front cone
        }

        // --- Accessors ---

        public float Hp => currentHp;
        public float Shield => currentShield;
        public float MaxHp => stats?.MaxHp ?? 0f;
        public float MaxShield => stats?.MaxShield ?? 0f;
        public Stats Stats => stats;
        public float FacingAngle => facingAngle;
    }
}
