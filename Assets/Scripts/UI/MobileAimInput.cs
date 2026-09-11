using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HY3RIDOrigins.UI
{
    /// Tracks a right-side drag touch to determine aim world position.
    /// Ignores touches already claimed by VirtualJoystick or MobileHUD buttons (via TouchOwnership).
    /// Holds the last known aim position on release — Leonidas never snaps to a default direction.
    [DefaultExecutionOrder(-80)]
    public class MobileAimInput : MonoBehaviour
    {
        /// World-space position that Leonidas should aim toward.
        /// Initialised to the right of the world origin; stays at last valid value on release.
        public Vector2 AimWorldPosition { get; private set; } = Vector2.right;

        private int _trackedTouchId = -1;

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            var touches = Touch.activeTouches;

            if (_trackedTouchId >= 0)
            {
                bool found = false;
                foreach (var t in touches)
                {
                    if (t.touchId != _trackedTouchId) continue;
                    found = true;
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                    {
                        // Hold last position — do not reset AimWorldPosition
                        _trackedTouchId = -1;
                    }
                    else
                    {
                        AimWorldPosition = ToWorld(t.screenPosition);
                        Debug.Log($"[MobileAimInput] AimWorldPos={AimWorldPosition:F2}  screen={t.screenPosition:F0}");
                    }
                    break;
                }
                if (!found) _trackedTouchId = -1;
            }
            else
            {
                // Claim the first unclaimed touch that began on the right half
                foreach (var t in touches)
                {
                    if (t.phase != TouchPhase.Began)               continue;
                    if (TouchOwnership.IsOwned(t.touchId))         continue;
                    if (t.screenPosition.x < Screen.width * 0.5f) continue;

                    _trackedTouchId  = t.touchId;
                    AimWorldPosition = ToWorld(t.screenPosition);
                    Debug.Log($"[MobileAimInput] touch claimed id={t.touchId}  AimWorldPos={AimWorldPosition:F2}");
                    break;
                }
            }
        }

        private static Vector2 ToWorld(Vector2 screenPos)
        {
            var cam = UnityEngine.Camera.main;
            if (cam == null) return screenPos;
            return cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, cam.nearClipPlane));
        }
    }
}
