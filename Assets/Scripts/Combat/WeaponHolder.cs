using UnityEngine;
using HY3RIDOrigins.Controllers;

namespace HY3RIDOrigins.Combat
{
    // Routes player combat input to Leonidas' weapons.
    // Tap RMB (< 0.15 s)  → Spear.TryThrust
    // Hold RMB (≥ 0.15 s) → Spear.StartCharge → Spear.ReleaseCharge on release
    // AI characters bypass this and call weapon methods directly from AIController.
    public class WeaponHolder : MonoBehaviour
    {
        private const float ThrustTapThreshold = 0.15f; // seconds

        private PlayerController input;
        private KineticPistol    pistol;
        private Spear            spear;

        private float spearDownTime = -1f; // < 0 = RMB not held

        public KineticPistol Pistol     => pistol;
        public Spear         SpearWeapon => spear;

        private void Awake()
        {
            input = GetComponent<PlayerController>();
        }

        // Called by LabSceneInitializer after components are added.
        public void SetWeapons(KineticPistol p, Spear s)
        {
            pistol = p;
            spear  = s;
        }

        private void Update()
        {
            if (input == null || pistol == null || spear == null) return;

            var origin = (Vector2)transform.position;
            var aimDir = (input.AimWorldPosition - origin).normalized;

            // ── Pistol ───────────────────────────────────────────────────────────
            if (input.FireDown || input.FireHeld)
                pistol.TryFire(origin, aimDir);

            if (input.ReloadJustDown)
                pistol.TryReload();

            // ── Spear ────────────────────────────────────────────────────────────
            if (input.SpearDown)
                spearDownTime = Time.time;

            if (input.SpearHeld && spearDownTime >= 0f)
            {
                float held = Time.time - spearDownTime;
                if (held >= ThrustTapThreshold && !spear.IsCharging && !spear.IsAttacking)
                    spear.StartCharge();
            }

            if (input.SpearJustUp && spearDownTime >= 0f)
            {
                float held = Time.time - spearDownTime;
                spearDownTime = -1f;

                if (held < ThrustTapThreshold)
                    spear.TryThrust(origin, aimDir);
                else
                    spear.ReleaseCharge(origin, aimDir);
            }
        }
    }
}
