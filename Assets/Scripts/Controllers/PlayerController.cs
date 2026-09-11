using UnityEngine;
using UnityEngine.InputSystem;

namespace HY3RIDOrigins.Controllers
{
    // Reads WASD + mouse input and exposes clean query methods.
    // Placed on the same GameObject as Leonidas.
    // Uses the New Input System (UnityEngine.InputSystem) directly via
    // Keyboard.current / Mouse.current — no Action Map asset required.
    // Controller support: wire a Gamepad in Phase 8+ by reading Gamepad.current here.
    public class PlayerController : MonoBehaviour
    {
        // Exposed so Leonidas can query per-frame.
        public Vector2 MoveInput { get; private set; }
        public Vector2 AimWorldPosition { get; private set; }

        public bool FireDown        { get; private set; } // LMB pressed this frame
        public bool FireHeld        { get; private set; } // LMB held
        public bool SpearDown       { get; private set; } // RMB pressed this frame
        public bool SpearHeld       { get; private set; } // RMB held
        public bool SpearJustUp     { get; private set; } // RMB released this frame
        public bool DodgeJustDown   { get; private set; } // Space
        public bool ReloadJustDown  { get; private set; } // R
        public bool ActiveJustDown  { get; private set; } // Q
        public bool PhalanxJustDown { get; private set; } // F

        private UnityEngine.Camera mainCam;

        private void Awake()
        {
            mainCam = UnityEngine.Camera.main;
        }

        private void Update()
        {
            ReadMoveInput();
            ReadAimInput();
            ReadActionInput();
        }

        private void ReadMoveInput()
        {
            var kb = Keyboard.current;
            if (kb == null) { MoveInput = Vector2.zero; return; }

            float x = 0f, y = 0f;
            if (kb.aKey.isPressed) x -= 1f;
            if (kb.dKey.isPressed) x += 1f;
            if (kb.sKey.isPressed) y -= 1f;
            if (kb.wKey.isPressed) y += 1f;

            MoveInput = new Vector2(x, y).normalized;
        }

        private void ReadAimInput()
        {
            if (mainCam == null) return;

            var mouse = Mouse.current;
            if (mouse == null) return;

            Vector2 screenPos = mouse.position.ReadValue();
            AimWorldPosition = mainCam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, mainCam.nearClipPlane));
        }

        private void ReadActionInput()
        {
            var kb = Keyboard.current;
            var mouse = Mouse.current;

            if (mouse != null)
            {
                FireDown    = mouse.leftButton.wasPressedThisFrame;
                FireHeld    = mouse.leftButton.isPressed;
                SpearDown   = mouse.rightButton.wasPressedThisFrame;
                SpearHeld   = mouse.rightButton.isPressed;
                SpearJustUp = mouse.rightButton.wasReleasedThisFrame;
            }
            else
            {
                FireDown = FireHeld = SpearDown = SpearHeld = SpearJustUp = false;
            }

            if (kb != null)
            {
                DodgeJustDown   = kb.spaceKey.wasPressedThisFrame;
                ReloadJustDown  = kb.rKey.wasPressedThisFrame;
                ActiveJustDown  = kb.qKey.wasPressedThisFrame;
                PhalanxJustDown = kb.fKey.wasPressedThisFrame;
            }
            else
            {
                DodgeJustDown = ReloadJustDown = ActiveJustDown = PhalanxJustDown = false;
            }
        }

        // Angle (radians) from player toward mouse position.
        public float AimAngle(Vector2 fromWorldPos)
        {
            Vector2 dir = AimWorldPosition - fromWorldPos;
            return Mathf.Atan2(dir.y, dir.x);
        }
    }
}
