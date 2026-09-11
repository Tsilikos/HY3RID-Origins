using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HY3RIDOrigins.UI
{
    /// On-screen analog joystick for the left half of the screen.
    /// Outputs normalized Value in [-1,1] per axis. Returns Vector2.zero when idle.
    /// Claims its active touch via TouchOwnership so MobileAimInput does not double-consume it.
    [DefaultExecutionOrder(-90)]
    public class VirtualJoystick : MonoBehaviour
    {
        [SerializeField] private float radius = 80f; // canvas pixels

        /// Normalized movement direction in [-1,1] per axis. Zero when not touched.
        public Vector2 Value         { get; private set; }
        /// Touch ID currently claimed by this joystick, or -1 if idle.
        public int     ActiveTouchId { get; private set; } = -1;

        private RectTransform _knobRect;
        private Vector2       _anchorCanvas; // canvas-space position of the outer ring center
        private Canvas        _canvas;

        // ── Initialisation ───────────────────────────────────────────────────────

        /// Called by MobileHUD after it creates this component.
        /// anchorScreenPos is in screen pixels (origin bottom-left).
        public void Init(Canvas canvas, Vector2 anchorScreenPos)
        {
            _canvas       = canvas;
            _anchorCanvas = anchorScreenPos; // Constant Pixel Size canvas: screen px == canvas px
            BuildVisuals();
        }

        private void BuildVisuals()
        {
            // Outer ring
            var outerGO = new GameObject("JoystickOuter");
            outerGO.transform.SetParent(transform, false);
            var outerRT = outerGO.AddComponent<RectTransform>();
            outerRT.anchorMin = outerRT.anchorMax = Vector2.zero;
            outerRT.pivot     = Vector2.one * 0.5f;
            outerRT.sizeDelta = Vector2.one * radius * 2f;
            outerRT.anchoredPosition = _anchorCanvas;
            var outerImg = outerGO.AddComponent<Image>();
            outerImg.sprite        = MakeCircleSprite(64, new Color(1f, 1f, 1f, 0.12f));
            outerImg.raycastTarget = false;

            // Ring border
            var borderGO = new GameObject("JoystickBorder");
            borderGO.transform.SetParent(outerGO.transform, false);
            var borderRT = borderGO.AddComponent<RectTransform>();
            borderRT.anchorMin = borderRT.anchorMax = new Vector2(0.5f, 0.5f);
            borderRT.pivot     = Vector2.one * 0.5f;
            borderRT.sizeDelta = Vector2.one * radius * 2f;
            borderRT.anchoredPosition = Vector2.zero;
            var borderImg = borderGO.AddComponent<Image>();
            borderImg.sprite        = MakeRingSprite(64, 5, new Color(1f, 1f, 1f, 0.45f));
            borderImg.raycastTarget = false;

            // Inner knob
            var knobGO = new GameObject("JoystickKnob");
            knobGO.transform.SetParent(outerGO.transform, false);
            _knobRect = knobGO.AddComponent<RectTransform>();
            _knobRect.anchorMin = _knobRect.anchorMax = new Vector2(0.5f, 0.5f);
            _knobRect.pivot     = Vector2.one * 0.5f;
            _knobRect.sizeDelta = Vector2.one * radius * 0.55f * 2f;
            _knobRect.anchoredPosition = Vector2.zero;
            var knobImg = knobGO.AddComponent<Image>();
            knobImg.sprite        = MakeCircleSprite(32, new Color(1f, 1f, 1f, 0.65f));
            knobImg.raycastTarget = false;
        }

        // ── Update ───────────────────────────────────────────────────────────────

        private void Update()
        {
            var touches = Touch.activeTouches;

            if (ActiveTouchId >= 0)
            {
                // Track existing touch
                bool stillAlive = false;
                foreach (var t in touches)
                {
                    if (t.touchId != ActiveTouchId) continue;
                    stillAlive = true;
                    if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                        Release();
                    else
                        UpdateKnob(t.screenPosition);
                    break;
                }
                if (!stillAlive) Release();
            }
            else
            {
                // Try to claim a new touch that began on the left half
                foreach (var t in touches)
                {
                    if (t.phase != TouchPhase.Began)            continue;
                    if (TouchOwnership.IsOwned(t.touchId))      continue;
                    if (t.screenPosition.x >= Screen.width * 0.5f) continue;

                    ActiveTouchId = t.touchId;
                    TouchOwnership.Claim(t.touchId);
                    UpdateKnob(t.screenPosition);
                    break;
                }
            }

            if (ActiveTouchId < 0)
            {
                Value = Vector2.zero;
                if (_knobRect != null) _knobRect.anchoredPosition = Vector2.zero;
                Debug.Log($"[VirtualJoystick] idle — Value={Value}");
            }
        }

        private void UpdateKnob(Vector2 screenPos)
        {
            // Constant Pixel Size canvas: screen pixels == canvas pixels
            Vector2 offset = screenPos - _anchorCanvas;
            float dist = offset.magnitude;
            if (dist > radius) offset = offset.normalized * radius;
            if (_knobRect != null) _knobRect.anchoredPosition = offset;
            Value = offset / radius; // [-1,1]
            Debug.Log($"[VirtualJoystick] Value={Value:F2}  raw-offset={offset:F0}");
        }

        private void Release()
        {
            TouchOwnership.Release(ActiveTouchId);
            ActiveTouchId = -1;
            Value = Vector2.zero;
            if (_knobRect != null) _knobRect.anchoredPosition = Vector2.zero;
        }

        // ── Sprite helpers ────────────────────────────────────────────────────────

        private static Sprite MakeCircleSprite(int radius, Color color)
        {
            int size = radius * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pix = new Color32[size * size];
            var c32 = (Color32)color;
            float r2 = (radius - 0.5f) * (radius - 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f, dy = y - radius + 0.5f;
                    pix[y * size + x] = dx * dx + dy * dy <= r2
                        ? c32 : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }

        private static Sprite MakeRingSprite(int outerRadius, int thickness, Color color)
        {
            int size = outerRadius * 2;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pix = new Color32[size * size];
            var c32 = (Color32)color;
            float rOuter2 = (outerRadius - 0.5f) * (outerRadius - 0.5f);
            float inner   = outerRadius - thickness;
            float rInner2 = (inner - 0.5f) * (inner - 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - outerRadius + 0.5f, dy = y - outerRadius + 0.5f;
                    float d2 = dx * dx + dy * dy;
                    pix[y * size + x] = d2 <= rOuter2 && d2 > rInner2
                        ? c32 : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
