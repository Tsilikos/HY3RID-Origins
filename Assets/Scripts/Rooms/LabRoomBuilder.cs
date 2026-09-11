using UnityEngine;
using HY3RIDOrigins.Camera;

namespace HY3RIDOrigins.Rooms
{
    // Builds the Phase 1 laboratory room at runtime.
    // Creates wall colliders and obstacle colliders from data definitions.
    // Generates placeholder sprites for environment objects.
    // Future phases replace this with proper tilemap/prefab rooms.
    public class LabRoomBuilder : MonoBehaviour
    {
        // Room boundaries in world space. Adjust in Inspector.
        [Header("Room Bounds")]
        [SerializeField] private float roomWidth  = 48f;
        [SerializeField] private float roomHeight = 36f;
        [SerializeField] private float wallThickness = 1.5f;

        [Header("References")]
        [SerializeField] private LabCamera labCamera;

        [Header("Colors")]
        [SerializeField] private Color floorColor     = new Color(0.067f, 0.067f, 0.090f); // #111117
        [SerializeField] private Color wallColor      = new Color(0.050f, 0.055f, 0.080f); // #0D0E14
        [SerializeField] private Color wallEdgeColor  = new Color(0.180f, 0.190f, 0.240f); // #2E3140
        [SerializeField] private Color machineryColor = new Color(0.090f, 0.145f, 0.200f); // #172533
        [SerializeField] private Color crateColor     = new Color(0.145f, 0.145f, 0.155f); // #252527

        // Spawn point returned for external use (Leonidas start position)
        public Vector2 SpawnPoint { get; private set; }

        // Awake runs before any Start(), guaranteeing SpawnPoint is set when
        // LabSceneInitializer.Start() reads it — no script execution order dependency.
        private void Awake()
        {
            BuildRoom();
        }

        private void Start()
        {
            // Fall back to FindObjectOfType when labCamera is not wired via Inspector.
            var cam = labCamera != null
                ? labCamera
                : FindObjectOfType<HY3RIDOrigins.Camera.LabCamera>();

            if (cam != null)
            {
                float hw = roomWidth  / 2f;
                float hh = roomHeight / 2f;
                cam.SetBounds(new Rect(-hw, -hh, roomWidth, roomHeight));
            }
        }

        private void BuildRoom()
        {
            BuildFloor();
            BuildWalls();
            BuildObstacles();

            // Spawn point: lower center of the room (near the cage area)
            SpawnPoint = new Vector2(0f, -roomHeight / 2f + 6f);
        }

        // --- Floor ---

        private void BuildFloor()
        {
            // Single large quad covering the room interior.
            var floor = CreateSprite("Floor", floorColor,
                Vector2.zero, new Vector2(roomWidth - wallThickness * 2f, roomHeight - wallThickness * 2f),
                order: -10);
        }

        // --- Walls ---

        private void BuildWalls()
        {
            float hw = roomWidth  / 2f;
            float hh = roomHeight / 2f;
            float wt = wallThickness;

            // North
            BuildWall("Wall_N", new Vector2(0f, hh - wt / 2f),    new Vector2(roomWidth, wt));
            // South
            BuildWall("Wall_S", new Vector2(0f, -hh + wt / 2f),   new Vector2(roomWidth, wt));
            // West
            BuildWall("Wall_W", new Vector2(-hw + wt / 2f, 0f),   new Vector2(wt, roomHeight));
            // East
            BuildWall("Wall_E", new Vector2(hw - wt / 2f, 0f),    new Vector2(wt, roomHeight));
        }

        private void BuildWall(string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform);
            go.transform.position = pos;
            go.layer = LayerMask.NameToLayer("Default");

            // Sprite (visual)
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite  = CreateSolidSprite(1, 1);
            sr.color   = wallColor;
            sr.size    = size;
            sr.drawMode = SpriteDrawMode.Tiled;

            // Edge accent (thin inner border)
            var edge = new GameObject("Edge");
            edge.transform.SetParent(go.transform);
            edge.transform.localPosition = Vector3.zero;
            var esr = edge.AddComponent<SpriteRenderer>();
            esr.sprite  = CreateBorderSprite(64, wallEdgeColor);
            esr.color   = Color.white;
            esr.size    = size;
            esr.drawMode = SpriteDrawMode.Tiled;
            esr.sortingOrder = 1;

            // Collider
            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            // Static rigidbody to work with composite colliders
            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }

        // --- Obstacles ---
        // Each entry: [localX, localY, width, height, type]
        // type: 0 = large machinery, 1 = workstation, 2 = crate/pillar

        private static readonly float[,] ObstacleData = {
            // Left wall machinery cluster
            { -16f,  10f,  5f, 2.5f,  0 },
            { -16f,  7f,   5f, 1.5f,  0 },
            { -18f,  4f,   3f, 4.0f,  0 },

            // Right wall machinery cluster
            {  16f,  10f,  5f, 2.5f,  0 },
            {  16f,  7f,   5f, 1.5f,  0 },
            {  18f,  4f,   3f, 4.0f,  0 },

            // Center column pair (creates two lanes)
            { -5f,   3f,  1.2f, 6f,   2 },
            {  5f,   3f,  1.2f, 6f,   2 },

            // Mid-left workstation cluster
            { -10f,  0f,  4f, 2f,     1 },
            { -10f, -3f,  4f, 1f,     1 },

            // Mid-right workstation cluster
            {  10f,  0f,  4f, 2f,     1 },
            {  10f, -3f,  4f, 1f,     1 },

            // Lower crates (near spawn, feel of storage area)
            { -8f,  -10f, 2f, 2f,     2 },
            { -5f,  -10f, 2f, 2f,     2 },
            {  5f,  -10f, 2f, 2f,     2 },
            {  8f,  -10f, 2f, 2f,     2 },

            // Upper center large machinery (boss-room direction blocker)
            { -3f,  14f,  6f, 3f,     0 },
            {  3f,  14f,  6f, 3f,     0 },
        };

        private void BuildObstacles()
        {
            for (int i = 0; i < ObstacleData.GetLength(0); i++)
            {
                float x = ObstacleData[i, 0];
                float y = ObstacleData[i, 1];
                float w = ObstacleData[i, 2];
                float h = ObstacleData[i, 3];
                int   t = (int)ObstacleData[i, 4];

                Color c = t switch {
                    0 => machineryColor,
                    1 => new Color(0.10f, 0.16f, 0.22f),
                    _ => crateColor,
                };

                BuildObstacle($"Obstacle_{i}", new Vector2(x, y), new Vector2(w, h), c);
            }
        }

        private void BuildObstacle(string label, Vector2 pos, Vector2 size, Color color)
        {
            var go = new GameObject(label);
            go.transform.SetParent(transform);
            go.transform.position = pos;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite  = CreateSolidSprite(1, 1);
            sr.color   = color;
            sr.size    = size;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.sortingOrder = 2;

            var col = go.AddComponent<BoxCollider2D>();
            col.size = size;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Static;
        }

        // --- Sprite Factories ---

        private static Sprite CreateSolidSprite(int w, int h)
        {
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var pixels = new Color32[w * h];
            for (int i = 0; i < pixels.Length; i++)
                pixels[i] = Color.white;
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.one * 0.5f, 1f);
        }

        private static Sprite CreateBorderSprite(int size, Color edgeColor)
        {
            var tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            var pixels = new Color32[size * size];
            int border = 2;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool onBorder = x < border || x >= size - border ||
                                   y < border || y >= size - border;
                    pixels[y * size + x] = onBorder
                        ? (Color32)edgeColor
                        : new Color32(0, 0, 0, 0);
                }
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            tex.filterMode = FilterMode.Point;
            return Sprite.Create(tex, new Rect(0, 0, size, size), Vector2.one * 0.5f, (float)size);
        }

        private static GameObject CreateSprite(string name, Color color,
            Vector2 pos, Vector2 size, int order = 0)
        {
            var go = new GameObject(name);
            go.transform.position = pos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite  = CreateSolidSprite(1, 1);
            sr.color   = color;
            sr.size    = size;
            sr.drawMode = SpriteDrawMode.Tiled;
            sr.sortingOrder = order;
            return go;
        }
    }
}
