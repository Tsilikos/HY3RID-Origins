using UnityEngine;
using HY3RIDOrigins.UI;

namespace HY3RIDOrigins.Controllers
{
    /// Reads mobile touch input from VirtualJoystick, MobileAimInput, and MobileHUD
    /// and exposes clean per-frame query properties.
    /// Public API (names, types, semantics) is identical to the Phase 1–4 keyboard/mouse version.
    /// Placed on the same GameObject as Leonidas.
    public class PlayerController : MonoBehaviour
    {
        // ── Public API — unchanged from Phase 1–4 ────────────────────────────────

        public Vector2 MoveInput        { get; private set; }
        public Vector2 AimWorldPosition { get; private set; }

        public bool FireDown        { get; private set; }
        public bool FireHeld        { get; private set; }
        public bool SpearDown       { get; private set; }
        public bool SpearHeld       { get; private set; }
        public bool SpearJustUp     { get; private set; }
        public bool DodgeJustDown   { get; private set; }
        public bool ReloadJustDown  { get; private set; } // always false — reload is automatic
        public bool ActiveJustDown  { get; private set; } // always false — Phase 6+
        public bool PhalanxJustDown { get; private set; } // always false — Phase 6+

        // ── Private references ────────────────────────────────────────────────────

        private MobileHUD      _hud;
        private VirtualJoystick _joystick;
        private MobileAimInput  _aimInput;

        // ── Awake ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            // MobileHUD bootstraps itself (and creates VirtualJoystick + MobileAimInput)
            // when PlayerController is constructed. If the HUD already exists in the scene,
            // we reuse it; otherwise we create it here.
            _hud = FindAnyObjectByType<MobileHUD>();
            if (_hud == null)
            {
                var hudGO = new GameObject("MobileHUD");
                _hud = hudGO.AddComponent<MobileHUD>(); // MobileHUD.Awake() runs immediately
            }

            _joystick = _hud.Joystick;
            _aimInput = _hud.AimInput;

            // Initialise aim to the right so Leonidas faces a sensible direction at spawn
            AimWorldPosition = (Vector2)transform.position + Vector2.right;
        }

        // ── Update ────────────────────────────────────────────────────────────────

        private void Update()
        {
            if (_joystick != null)
                MoveInput = _joystick.Value;
            else
                MoveInput = Vector2.zero;

            if (_aimInput != null)
                AimWorldPosition = _aimInput.AimWorldPosition;

            if (_hud != null)
            {
                FireDown      = _hud.FireDown;
                FireHeld      = _hud.FireHeld;
                SpearDown     = _hud.SpearDown;
                SpearHeld     = _hud.SpearHeld;
                SpearJustUp   = _hud.SpearJustUp;
                DodgeJustDown = _hud.DodgeJustDown;
            }
            else
            {
                FireDown = FireHeld = SpearDown = SpearHeld = SpearJustUp = DodgeJustDown = false;
            }

            // These remain permanently false in Phase 4.5.
            // ReloadJustDown: automatic reload wired in KineticPistol.TryFire — no button needed.
            // ActiveJustDown / PhalanxJustDown: Phase 6+ abilities, no button yet.
            ReloadJustDown  = false;
            ActiveJustDown  = false;
            PhalanxJustDown = false;
        }

        // ── Utility ───────────────────────────────────────────────────────────────

        /// Angle (radians) from player toward aim position. Unchanged from Phase 1–4.
        public float AimAngle(Vector2 fromWorldPos)
        {
            Vector2 dir = AimWorldPosition - fromWorldPos;
            return Mathf.Atan2(dir.y, dir.x);
        }
    }
}
