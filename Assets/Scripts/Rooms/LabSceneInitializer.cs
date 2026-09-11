using UnityEngine;
using HY3RIDOrigins.Camera;
using HY3RIDOrigins.Characters;
using HY3RIDOrigins.Combat;
using HY3RIDOrigins.Data;
using HY3RIDOrigins.Testing;

namespace HY3RIDOrigins.Rooms
{
    // Phase 1/2 scene entry point.
    // Spawns Leonidas (with weapons + dodge), wires the camera, places test targets.
    // Attach to an empty "SceneInitializer" GameObject in Lab_Floor_01.unity.
    public class LabSceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LabRoomBuilder roomBuilder;
        [SerializeField] private LabCamera labCamera;

        private void Start()
        {
            // LabRoomBuilder.Awake() already ran — SpawnPoint is guaranteed set.
            SpawnLeonidas();
            SpawnTestTargets();
        }

        private void SpawnLeonidas()
        {
            var stats = CharacterData.Leonidas;

            var go = new GameObject("Leonidas");
            // Tag BEFORE AddComponent so Awake() methods read the correct tag
            go.tag = "Player";
            go.transform.position = roomBuilder != null
                ? (Vector3)(Vector2)roomBuilder.SpawnPoint
                : Vector3.zero;

            // ── Sprite ───────────────────────────────────────────────────────────
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CharacterSpriteFactory.CreateTopDownSprite(stats);
            sr.sortingOrder = 10;

            // ── Physics ──────────────────────────────────────────────────────────
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = stats.Radius;

            // ── Core scripts ─────────────────────────────────────────────────────
            go.AddComponent<HY3RIDOrigins.Controllers.PlayerController>();
            var leonidas = go.AddComponent<Leonidas>();

            // ── Combat ───────────────────────────────────────────────────────────
            var pistol  = go.AddComponent<KineticPistol>();
            var spear   = go.AddComponent<Spear>();
            var holder  = go.AddComponent<WeaponHolder>();
            var dodger  = go.AddComponent<DodgeController>();
            holder.SetWeapons(pistol, spear);

            // ── Camera ───────────────────────────────────────────────────────────
            if (labCamera != null)
                labCamera.SetTarget(go.transform);

            // ── Debug console ────────────────────────────────────────────────────
            HY3RIDOrigins.DevTools.DebugConsole.LeonidassRef = leonidas;
        }

        // Five stationary dummies placed at open spots in the room for weapon testing.
        private void SpawnTestTargets()
        {
            SpawnTarget(new Vector2(  0f,  -6f));  // just north of spawn, open lane
            SpawnTarget(new Vector2(-10f,   6f));  // left mid-area
            SpawnTarget(new Vector2( 10f,   6f));  // right mid-area
            SpawnTarget(new Vector2(  0f,  10f));  // upper center
            SpawnTarget(new Vector2(-14f, -10f));  // lower-left open area
        }

        private static void SpawnTarget(Vector2 pos)
        {
            var go = new GameObject("TestTarget");
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CreateTargetSprite();
            sr.color  = Color.green;
            sr.sortingOrder = 5;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = Vector2.one * 1.4f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;

            go.AddComponent<TestTarget>();
        }

        private static Sprite CreateTargetSprite()
        {
            // Cross / target icon: 32×32 solid with center cross
            int size = 32;
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            var body = new Color32(80, 80, 90, 255);
            var cross = new Color32(200, 210, 220, 255);
            int thick = 4, margin = 6;
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    bool inBody  = x >= margin && x < size - margin &&
                                   y >= margin && y < size - margin;
                    bool inCross = (x >= size/2 - thick/2 && x < size/2 + thick/2) ||
                                   (y >= size/2 - thick/2 && y < size/2 + thick/2);
                    if (inBody && inCross) pixels[y * size + x] = cross;
                    else if (inBody)       pixels[y * size + x] = body;
                    else                   pixels[y * size + x] = new Color32(0, 0, 0, 0);
                }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, size);
        }
    }
}
