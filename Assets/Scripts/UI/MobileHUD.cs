using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HY3RIDOrigins.UI
{
    /// Assembles the on-screen control layout on a Screen Space Overlay canvas.
    /// Creates and owns: VirtualJoystick, MobileAimInput, CommandWheelUI.
    /// Exposes per-frame combat input states to PlayerController.
    ///
    /// Edge-state contract (checked each Update before exposing to callers):
    ///   FireDown      — true for exactly one frame on press
    ///   SpearDown     — true for exactly one frame on press
    ///   SpearJustUp   — true for exactly one frame on release
    ///   DodgeJustDown — true for exactly one frame on press
    [DefaultExecutionOrder(-100)]
    public class MobileHUD : MonoBehaviour
    {
        // ── Public state (read each frame by PlayerController) ────────────────────

        public bool FireDown      { get; private set; }
        public bool FireHeld      { get; private set; }
        public bool SpearDown     { get; private set; }
        public bool SpearHeld     { get; private set; }
        public bool SpearJustUp   { get; private set; }
        public bool DodgeJustDown { get; private set; }

        // ── Child components ─────────────────────────────────────────────────────

        public VirtualJoystick Joystick    { get; private set; }
        public MobileAimInput  AimInput    { get; private set; }
        public CommandWheelUI  CommandWheel { get; private set; }

        // ── Button tracking ───────────────────────────────────────────────────────

        private struct ButtonTracker
        {
            public RectTransform rect;
            public int   touchId;       // -1 when idle
            public bool  held;
            public bool  justPressed;   // set in Update, cleared next Update
            public bool  justReleased;  // set in Update, cleared next Update
        }

        private ButtonTracker _fire;
        private ButtonTracker _spear;
        private ButtonTracker _dodge;
        private ButtonTracker _command;

        // Screen positions (screen pixels, origin bottom-left)
        private Vector2 _cmdButtonScreenPos;

        private Canvas _canvas;

        // ── Awake ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            EnhancedTouchSupport.Enable();
            BuildCanvas();
            BuildHUDElements();
        }

        private void OnDestroy()
        {
            EnhancedTouchSupport.Disable();
        }

        // ── Canvas + HUD construction ─────────────────────────────────────────────

        private void BuildCanvas()
        {
            var go = new GameObject("HUD_Canvas");
            go.transform.SetParent(transform, false);
            _canvas = go.AddComponent<Canvas>();
            _canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            _canvas.sortingOrder = 100;

            var scaler = go.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ConstantPixelSize;
            scaler.scaleFactor = 1f;

            go.AddComponent<GraphicRaycaster>();
        }

        private void BuildHUDElements()
        {
            // ── VirtualJoystick ──────────────────────────────────────────────────
            var joyGO = new GameObject("VirtualJoystick");
            joyGO.transform.SetParent(_canvas.transform, false);
            var joyRT = joyGO.AddComponent<RectTransform>();
            joyRT.anchorMin = joyRT.anchorMax = Vector2.zero;
            joyRT.anchoredPosition = Vector2.zero;
            joyRT.sizeDelta = Vector2.zero;
            Joystick = joyGO.AddComponent<VirtualJoystick>();

            // Anchor: lower-left thumb zone
            var joyAnchor = new Vector2(130f, 130f);
            Joystick.Init(_canvas, joyAnchor);

            // ── MobileAimInput ───────────────────────────────────────────────────
            var aimGO = new GameObject("MobileAimInput");
            aimGO.transform.SetParent(_canvas.transform, false);
            AimInput = aimGO.AddComponent<MobileAimInput>();

            // ── CommandWheelUI ───────────────────────────────────────────────────
            var wheelGO = new GameObject("CommandWheelUI");
            wheelGO.transform.SetParent(_canvas.transform, false);
            var wheelRT = wheelGO.AddComponent<RectTransform>();
            wheelRT.anchorMin = wheelRT.anchorMax = Vector2.zero;
            wheelRT.anchoredPosition = Vector2.zero;
            wheelRT.sizeDelta = Vector2.zero;
            CommandWheel = wheelGO.AddComponent<CommandWheelUI>();
            CommandWheel.SetCanvas(_canvas);

            // ── Buttons ──────────────────────────────────────────────────────────
            // Layout (screen-space px, origin bottom-left — typical ~1080×1920 portrait)
            // These are fixed pixel coords; they read correctly under Constant Pixel Size.
            CreateButton(ref _fire,    "BTN_Fire",    new Vector2(Screen.width - 110f, 140f), 100f,
                new Color(0.8f, 0.2f, 0.2f, 0.75f), "FIRE");
            CreateButton(ref _spear,   "BTN_Spear",   new Vector2(Screen.width - 230f, 200f), 100f,
                new Color(0.2f, 0.5f, 0.9f, 0.75f), "SPEAR");
            CreateButton(ref _dodge,   "BTN_Dodge",   new Vector2(Screen.width - 160f, 310f), 90f,
                new Color(0.3f, 0.7f, 0.3f, 0.75f), "DODGE");

            _cmdButtonScreenPos = new Vector2(Screen.width - 100f, Screen.height - 160f);
            CreateButton(ref _command, "BTN_Command", _cmdButtonScreenPos, 80f,
                new Color(0.6f, 0.3f, 0.7f, 0.75f), "CMD");
        }

        private void CreateButton(ref ButtonTracker btn, string name, Vector2 screenPos,
                                   float size, Color color, string label)
        {
            var go = new GameObject(name);
            go.transform.SetParent(_canvas.transform, false);

            var rt = go.AddComponent<RectTransform>();
            rt.anchorMin = rt.anchorMax = Vector2.zero;
            rt.pivot     = Vector2.one * 0.5f;
            rt.sizeDelta = Vector2.one * size;
            rt.anchoredPosition = screenPos;

            var img = go.AddComponent<Image>();
            img.sprite = MakeCircleSprite(64, color);

            // Label
            var labelGO = new GameObject("Label");
            labelGO.transform.SetParent(go.transform, false);
            var labelRT = labelGO.AddComponent<RectTransform>();
            labelRT.anchorMin = Vector2.zero;
            labelRT.anchorMax = Vector2.one;
            labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
            var text = labelGO.AddComponent<Text>();
            text.text      = label;
            text.fontSize  = 14;
            text.alignment = TextAnchor.MiddleCenter;
            text.color     = Color.white;
            text.raycastTarget = false;

            btn.rect    = rt;
            btn.touchId = -1;
        }

        // ── Update ────────────────────────────────────────────────────────────────

        private void Update()
        {
            // Clear one-frame edge states from previous frame
            _fire.justPressed    = false;
            _spear.justPressed   = false;
            _spear.justReleased  = false;
            _dodge.justPressed   = false;
            _command.justPressed = false;

            var touches = Touch.activeTouches;

            // ── Step 1: release tracking — detect lifted touches ─────────────────
            CheckRelease(ref _fire);
            CheckRelease(ref _spear);
            CheckRelease(ref _dodge);
            CheckRelease(ref _command);

            // ── Step 2: new-touch claiming ────────────────────────────────────────
            foreach (var t in touches)
            {
                if (t.phase != TouchPhase.Began) continue;
                if (TouchOwnership.IsOwned(t.touchId)) continue;

                // Check each button for a hit (ordered by priority)
                if      (HitTest(ref _command, t)) ClaimButton(ref _command, t.touchId, isCommand: true);
                else if (HitTest(ref _dodge,   t)) ClaimButton(ref _dodge,   t.touchId);
                else if (HitTest(ref _spear,   t)) ClaimButton(ref _spear,   t.touchId);
                else if (HitTest(ref _fire,    t)) ClaimButton(ref _fire,    t.touchId);
            }

            // ── Step 3: expose per-frame states ──────────────────────────────────
            FireDown      = _fire.justPressed;
            FireHeld      = _fire.held;
            SpearDown     = _spear.justPressed;
            SpearHeld     = _spear.held;
            SpearJustUp   = _spear.justReleased;
            DodgeJustDown = _dodge.justPressed;

            DebugLogEdges();
        }

        private void CheckRelease(ref ButtonTracker btn)
        {
            if (btn.touchId < 0) return;

            bool stillActive = false;
            foreach (var t in Touch.activeTouches)
            {
                if (t.touchId != btn.touchId) continue;
                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled) break;
                stillActive = true;
                break;
            }

            if (!stillActive)
            {
                // Touch lifted
                btn.justReleased = true;
                btn.held         = false;
                TouchOwnership.Release(btn.touchId);
                btn.touchId = -1;
            }
        }

        private bool HitTest(ref ButtonTracker btn, Touch t)
        {
            if (btn.rect == null) return false;
            Vector2 sp = t.screenPosition;
            Vector2 rPos  = btn.rect.anchoredPosition;
            Vector2 rSize = btn.rect.sizeDelta;
            float hw = rSize.x * 0.5f, hh = rSize.y * 0.5f;
            return sp.x >= rPos.x - hw && sp.x <= rPos.x + hw &&
                   sp.y >= rPos.y - hh && sp.y <= rPos.y + hh;
        }

        private void ClaimButton(ref ButtonTracker btn, int touchId, bool isCommand = false)
        {
            btn.touchId      = touchId;
            btn.held         = true;
            btn.justPressed  = true;
            TouchOwnership.Claim(touchId);

            if (isCommand)
            {
                CommandWheel.Activate(touchId, _cmdButtonScreenPos);
                // CommandWheelUI now tracks the same touchId for wheel direction.
                // The touch stays claimed in TouchOwnership so VirtualJoystick and
                // MobileAimInput ignore it.
            }
        }

        // ── Debug logging for engine validation ───────────────────────────────────

        private void DebugLogEdges()
        {
            if (FireDown)      Debug.Log("[MobileHUD] FireDown");
            if (SpearDown)     Debug.Log("[MobileHUD] SpearDown");
            if (SpearJustUp)   Debug.Log("[MobileHUD] SpearJustUp");
            if (DodgeJustDown) Debug.Log("[MobileHUD] DodgeJustDown");
        }

        // ── Sprite helper ─────────────────────────────────────────────────────────

        private static Sprite MakeCircleSprite(int radius, Color color)
        {
            int size = radius * 2;
            var tex  = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pix  = new Color32[size * size];
            var c32  = (Color32)color;
            float r2 = (radius - 0.5f) * (radius - 0.5f);
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    float dx = x - radius + 0.5f, dy = y - radius + 0.5f;
                    pix[y * size + x] = dx * dx + dy * dy <= r2 ? c32 : new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(pix);
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
