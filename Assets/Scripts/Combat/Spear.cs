using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace HY3RIDOrigins.Combat
{
    // Leonidas' signature melee weapon.
    // INPUT CONTRACT (driven by WeaponHolder):
    //   TryThrust()      — quick RMB tap
    //   StartCharge()    — RMB held past the tap threshold
    //   ReleaseCharge()  — RMB released after charge started
    //
    // Thrust = fast narrow cone forward.
    // Sweep  = slower full-circle — basic on quick release, charged on long hold.
    public class Spear : MonoBehaviour
    {
        [Header("Thrust")]
        [SerializeField] private float thrustDamage   = 35f;
        [SerializeField] private float thrustRange    = 2.4f;
        [SerializeField] private float thrustHalfAngle = 55f; // degrees
        [SerializeField] private float thrustCooldown = 0.32f;
        [SerializeField] private float thrustDuration = 0.12f;

        [Header("Sweep")]
        [SerializeField] private float sweepDamage   = 25f;
        [SerializeField] private float sweepRadius   = 2.8f;
        [SerializeField] private float sweepCooldown = 0.70f;
        [SerializeField] private float sweepDuration = 0.25f;

        [Header("Charged Sweep")]
        [SerializeField] private float minChargeSecs     = 0.40f;
        [SerializeField] private float maxChargeSecs     = 1.80f;
        [SerializeField] private float maxDamageMult     = 2.5f;
        [SerializeField] private float maxRadiusMult     = 1.8f;

        private float nextThrustTime;
        private float nextSweepTime;
        private float chargeStartTime = -1f;
        private string ownerTag;

        // Visual children (built in Awake, toggled during attacks)
        private GameObject thrustIndicator;
        private GameObject sweepIndicator;
        private SpriteRenderer sweepIndicatorSR;
        private GameObject chargeRing;
        private SpriteRenderer chargeRingSR;

        public bool  IsAttacking { get; private set; }
        public bool  IsCharging  => chargeStartTime >= 0f;

        // 0..1 progress through the charge window (above minChargeSecs)
        public float ChargeRatio => chargeStartTime < 0f ? 0f :
            Mathf.Clamp01((Time.time - chargeStartTime - minChargeSecs) /
                          Mathf.Max(maxChargeSecs - minChargeSecs, 0.01f));

        private void Awake()
        {
            ownerTag = gameObject.tag;
            BuildVisuals();
        }

        private void BuildVisuals()
        {
            // Thrust indicator: narrow oval in local +Y direction (= world aim direction)
            // localScale (0.18, thrustRange, 1) makes it thrustRange units long, 0.18 wide
            thrustIndicator = new GameObject("ThrustIndicator");
            thrustIndicator.transform.SetParent(transform);
            var tsr = thrustIndicator.AddComponent<SpriteRenderer>();
            tsr.sprite = SpriteUtil.Circle(16, new Color(0.75f, 0.9f, 1f, 0.75f));
            tsr.sortingOrder = 18;
            thrustIndicator.transform.localScale    = new Vector3(0.18f, thrustRange, 1f);
            thrustIndicator.transform.localPosition = new Vector3(0f, thrustRange * 0.5f, 0f);
            thrustIndicator.SetActive(false);

            // Sweep indicator: filled circle scaled to sweep radius
            sweepIndicator = new GameObject("SweepIndicator");
            sweepIndicator.transform.SetParent(transform);
            sweepIndicatorSR = sweepIndicator.AddComponent<SpriteRenderer>();
            sweepIndicatorSR.sprite = SpriteUtil.Circle(32, new Color(0.6f, 0.82f, 1f, 0.4f));
            sweepIndicatorSR.sortingOrder = 17;
            sweepIndicator.transform.localPosition = Vector3.zero;
            sweepIndicator.SetActive(false);

            // Charge ring: grows and brightens as charge builds
            chargeRing = new GameObject("ChargeRing");
            chargeRing.transform.SetParent(transform);
            chargeRingSR = chargeRing.AddComponent<SpriteRenderer>();
            chargeRingSR.sprite = SpriteUtil.Circle(32, new Color(0.5f, 0.78f, 1f, 0.22f));
            chargeRingSR.sortingOrder = 12;
            chargeRing.transform.localPosition = Vector3.zero;
            chargeRing.SetActive(false);
        }

        private void Update()
        {
            if (IsCharging)
            {
                chargeRing.SetActive(true);
                float ratio = ChargeRatio;
                float diameter = sweepRadius * Mathf.Lerp(0.6f, maxRadiusMult, ratio) * 2f;
                chargeRing.transform.localScale = Vector3.one * diameter;
                float alpha = Mathf.Lerp(0.18f, 0.65f, ratio);
                chargeRingSR.color = new Color(
                    Mathf.Lerp(0.5f, 1.0f, ratio),
                    Mathf.Lerp(0.78f, 0.5f, ratio),
                    1f, alpha);
            }
            else
            {
                chargeRing.SetActive(false);
            }
        }

        // ── Public API ──────────────────────────────────────────────────────────

        public bool TryThrust(Vector2 origin, Vector2 aimDir)
        {
            if (IsAttacking || Time.time < nextThrustTime) return false;
            StartCoroutine(ThrustRoutine(origin, aimDir));
            return true;
        }

        public void StartCharge()
        {
            if (IsAttacking || IsCharging) return;
            chargeStartTime = Time.time;
        }

        public bool ReleaseCharge(Vector2 origin, Vector2 aimDir)
        {
            if (!IsCharging) return false;
            float held  = Time.time - chargeStartTime;
            chargeStartTime = -1f;
            float ratio = held < minChargeSecs ? 0f :
                Mathf.Clamp01((held - minChargeSecs) / Mathf.Max(maxChargeSecs - minChargeSecs, 0.01f));
            return TrySweep(origin, aimDir, ratio);
        }

        public bool TrySweep(Vector2 origin, Vector2 aimDir, float chargeRatio = 0f)
        {
            if (IsAttacking || Time.time < nextSweepTime) return false;
            StartCoroutine(SweepRoutine(origin, aimDir, chargeRatio));
            return true;
        }

        // ── Attack coroutines ───────────────────────────────────────────────────

        private IEnumerator ThrustRoutine(Vector2 origin, Vector2 aimDir)
        {
            IsAttacking   = true;
            nextThrustTime = Time.time + thrustCooldown;

            thrustIndicator.SetActive(true);

            ApplyHits(HitCone(origin, aimDir, thrustRange, thrustHalfAngle), thrustDamage);

            yield return new WaitForSeconds(thrustDuration);

            thrustIndicator.SetActive(false);
            IsAttacking = false;
        }

        private IEnumerator SweepRoutine(Vector2 origin, Vector2 aimDir, float chargeRatio)
        {
            IsAttacking  = true;
            nextSweepTime = Time.time + sweepCooldown;

            float radius   = sweepRadius * Mathf.Lerp(1f, maxRadiusMult, chargeRatio);
            float damage   = sweepDamage * Mathf.Lerp(1f, maxDamageMult, chargeRatio);
            float duration = sweepDuration * (1f + chargeRatio * 0.5f);

            sweepIndicator.SetActive(true);
            sweepIndicator.transform.localScale = Vector3.one * radius * 2f;
            sweepIndicatorSR.color = new Color(
                Mathf.Lerp(0.6f, 1.0f, chargeRatio),
                Mathf.Lerp(0.82f, 0.5f, chargeRatio),
                1f,
                Mathf.Lerp(0.40f, 0.65f, chargeRatio));

            ApplyHits(HitCircle(origin, radius), damage);

            yield return new WaitForSeconds(duration);

            sweepIndicator.SetActive(false);
            IsAttacking = false;
        }

        // ── Hit detection ───────────────────────────────────────────────────────

        private void ApplyHits(Collider2D[] hits, float damage)
        {
            var seen = new HashSet<GameObject>();
            foreach (var c in hits)
            {
                var root = c.transform.root.gameObject;
                if (!seen.Add(root)) continue; // don't hit the same entity twice (multi-collider)

                var target = c.GetComponentInParent<IDamageable>();
                if (target == null) continue;

                target.TakeDamage(damage);
                HitEffect.Spawn(c.transform.position, 0.38f, new Color(0.75f, 0.9f, 1f), 0.13f);
            }
        }

        private Collider2D[] HitCone(Vector2 origin, Vector2 aimDir, float range, float halfAngleDeg)
        {
            var all    = Physics2D.OverlapCircleAll(origin, range);
            var result = new List<Collider2D>();
            aimDir = aimDir.normalized;
            foreach (var c in all)
            {
                if (c.gameObject == gameObject) continue;
                if (c.gameObject.CompareTag(ownerTag)) continue;
                Vector2 toTarget = (Vector2)c.transform.position - origin;
                if (toTarget.sqrMagnitude < 0.01f) continue;
                if (Vector2.Angle(aimDir, toTarget.normalized) <= halfAngleDeg)
                    result.Add(c);
            }
            return result.ToArray();
        }

        private Collider2D[] HitCircle(Vector2 origin, float radius)
        {
            var all    = Physics2D.OverlapCircleAll(origin, radius);
            var result = new List<Collider2D>();
            foreach (var c in all)
            {
                if (c.gameObject == gameObject) continue;
                if (c.gameObject.CompareTag(ownerTag)) continue;
                result.Add(c);
            }
            return result.ToArray();
        }
    }
}
