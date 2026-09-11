using System;
using UnityEngine;
using UnityEngine.InputSystem.EnhancedTouch;
using UnityEngine.UI;
using HY3RIDOrigins.Squad;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HY3RIDOrigins.UI
{
    /// Radial command wheel activated by MobileHUD's command button.
    /// Selection is direction-based: the angle from the wheel centre to the current touch
    /// position determines the highlighted segment. Release fires OnCommandSelected.
    /// Release at the centre (within dead-zone) fires nothing.
    /// Phase 5 wires SquadCommandSystem as a subscriber to OnCommandSelected.
    [DefaultExecutionOrder(-85)]
    public class CommandWheelUI : MonoBehaviour
    {
        /// Fires with the selected command on release outside the dead-zone.
        /// Phase 5: SquadCommandSystem subscribes here.
        public event Action<SquadCommandType> OnCommandSelected;

        private const float DeadZoneRadius = 40f; // canvas pixels — centre release fires nothing

        private bool  _active;
        private int   _trackedTouchId = -1;
        private Vector2 _centerCanvas;  // wheel centre in canvas (screen) pixels

        // Command arrangement clockwise from top: Follow(top) → Aggressive → Defensive → Hold → Focus
        private static readonly SquadCommandType[] Commands =
        {
            SquadCommandType.Follow,
            SquadCommandType.Aggressive,
            SquadCommandType.Defensive,
            SquadCommandType.Hold,
            SquadCommandType.Focus,
        };

        // 5 segments × 72° each. Segment i spans [(90 - i*72 - 36), (90 - i*72 + 36)] degrees.
        private const int SegmentCount = 5;
        private const float SegmentAngle = 360f / SegmentCount;

        // Visuals
        private GameObject _wheelRoot;
        private Image[]    _segmentImages;
        private int        _highlightedIndex = -1;

        private static readonly Color ColNormal    = new Color(0.1f, 0.1f, 0.15f, 0.85f);
        private static readonly Color ColHighlight = new Color(0.9f, 0.75f, 0.2f,  0.95f);

        // ── Public API ───────────────────────────────────────────────────────────

        /// MobileHUD calls this when the command button is pressed.
        public void Activate(int touchId, Vector2 centerScreenPos)
        {
            _trackedTouchId = touchId;
            _centerCanvas   = centerScreenPos;
            _highlightedIndex = -1;
            _active = true;
            BuildOrShowWheel(centerScreenPos);
        }

        // ── Lifecycle ────────────────────────────────────────────────────────────

        private void Update()
        {
            if (!_active) return;

            var touches = Touch.activeTouches;
            bool found  = false;

            foreach (var t in touches)
            {
                if (t.touchId != _trackedTouchId) continue;
                found = true;

                if (t.phase == TouchPhase.Ended || t.phase == TouchPhase.Canceled)
                {
                    FireAndDeactivate(t.screenPosition);
                }
                else
                {
                    UpdateHighlight(t.screenPosition);
                }
                break;
            }

            if (!found) Deactivate();
        }

        // ── Core logic ───────────────────────────────────────────────────────────

        private void UpdateHighlight(Vector2 screenPos)
        {
            Vector2 delta = screenPos - _centerCanvas;
            if (delta.magnitude < DeadZoneRadius)
            {
                SetHighlight(-1);
                return;
            }

            float angle = Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg; // [-180,180]
            // Segment 0 (Follow) is at 90° (top). We rotate so 90° → 0 index.
            float adjusted = (90f - angle + 360f) % 360f; // clockwise from top
            int index = Mathf.FloorToInt(adjusted / SegmentAngle) % SegmentCount;
            SetHighlight(index);
        }

        private void FireAndDeactivate(Vector2 screenPos)
        {
            Vector2 delta = screenPos - _centerCanvas;
            if (delta.magnitude >= DeadZoneRadius && _highlightedIndex >= 0)
            {
                var cmd = Commands[_highlightedIndex];
                Debug.Log($"[CommandWheelUI] Selected: {cmd}");
                OnCommandSelected?.Invoke(cmd);
            }
            Deactivate();
        }

        private void Deactivate()
        {
            _active = false;
            _trackedTouchId   = -1;
            _highlightedIndex = -1;
            if (_wheelRoot != null) _wheelRoot.SetActive(false);
        }

        private void SetHighlight(int index)
        {
            if (index == _highlightedIndex) return;
            _highlightedIndex = index;
            if (_segmentImages == null) return;
            for (int i = 0; i < _segmentImages.Length; i++)
                _segmentImages[i].color = (i == index) ? ColHighlight : ColNormal;
        }

        // ── Visuals ──────────────────────────────────────────────────────────────

        private Canvas _canvas;

        private void Awake()
        {
            _canvas = GetComponentInParent<Canvas>();
        }

        public void SetCanvas(Canvas canvas)
        {
            _canvas = canvas;
        }

        private void BuildOrShowWheel(Vector2 centerPos)
        {
            if (_wheelRoot != null)
            {
                // Reposition existing wheel
                var rt = _wheelRoot.GetComponent<RectTransform>();
                if (rt != null) rt.anchoredPosition = centerPos;
                _wheelRoot.SetActive(true);
                SetHighlight(-1);
                return;
            }

            _wheelRoot = new GameObject("CommandWheel");
            _wheelRoot.transform.SetParent(transform, false);
            var rootRT = _wheelRoot.AddComponent<RectTransform>();
            rootRT.anchorMin = rootRT.anchorMax = Vector2.zero;
            rootRT.pivot     = Vector2.one * 0.5f;
            rootRT.sizeDelta = Vector2.zero;
            rootRT.anchoredPosition = centerPos;

            // Dark backdrop disc
            var bgGO = new GameObject("WheelBG");
            bgGO.transform.SetParent(_wheelRoot.transform, false);
            var bgRT = bgGO.AddComponent<RectTransform>();
            bgRT.anchorMin = bgRT.anchorMax = new Vector2(0.5f, 0.5f);
            bgRT.pivot     = Vector2.one * 0.5f;
            bgRT.sizeDelta = Vector2.one * 240f;
            bgRT.anchoredPosition = Vector2.zero;
            var bgImg = bgGO.AddComponent<Image>();
            bgImg.sprite        = MakeCircleSprite(128, new Color(0f, 0f, 0f, 0.65f));
            bgImg.raycastTarget = false;

            // 5 segment buttons arranged in a ring
            _segmentImages = new Image[SegmentCount];
            float ringRadius = 90f;
            float segSize    = 70f;

            for (int i = 0; i < SegmentCount; i++)
            {
                // Angle: 90° for index 0, stepping 72° clockwise (subtract)
                float angleDeg = 90f - i * SegmentAngle;
                float rad      = angleDeg * Mathf.Deg2Rad;
                Vector2 pos    = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad)) * ringRadius;

                var segGO = new GameObject($"Segment_{Commands[i]}");
                segGO.transform.SetParent(_wheelRoot.transform, false);
                var segRT = segGO.AddComponent<RectTransform>();
                segRT.anchorMin = segRT.anchorMax = new Vector2(0.5f, 0.5f);
                segRT.pivot     = Vector2.one * 0.5f;
                segRT.sizeDelta = Vector2.one * segSize;
                segRT.anchoredPosition = pos;

                var segImg = segGO.AddComponent<Image>();
                segImg.sprite        = MakeCircleSprite(32, Color.white);
                segImg.color         = ColNormal;
                segImg.raycastTarget = false;
                _segmentImages[i]    = segImg;

                // Label
                var labelGO = new GameObject("Label");
                labelGO.transform.SetParent(segGO.transform, false);
                var labelRT = labelGO.AddComponent<RectTransform>();
                labelRT.anchorMin = Vector2.zero;
                labelRT.anchorMax = Vector2.one;
                labelRT.offsetMin = labelRT.offsetMax = Vector2.zero;
                var labelText = labelGO.AddComponent<UnityEngine.UI.Text>();
                labelText.text      = Commands[i].ToString().ToUpper();
                labelText.fontSize  = 11;
                labelText.alignment = TextAnchor.MiddleCenter;
                labelText.color     = Color.white;
                labelText.raycastTarget = false;
            }
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
