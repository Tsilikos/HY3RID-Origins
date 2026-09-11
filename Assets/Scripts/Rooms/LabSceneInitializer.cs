using UnityEngine;
using HY3RIDOrigins.Camera;
using HY3RIDOrigins.Characters;
using HY3RIDOrigins.Data;
using HY3RIDOrigins.Rooms;

namespace HY3RIDOrigins.Rooms
{
    // Phase 1 scene entry point.
    // Spawns Leonidas, sets up the camera, wires everything together.
    // Attach to an empty "SceneInitializer" GameObject in Lab_Floor_01.unity.
    public class LabSceneInitializer : MonoBehaviour
    {
        [Header("References")]
        [SerializeField] private LabRoomBuilder roomBuilder;
        [SerializeField] private LabCamera labCamera;

        private void Start()
        {
            // Room is built by LabRoomBuilder.Start() which runs before this
            // because Start() order depends on script execution order.
            // If order matters, use Awake/Start split or set Script Execution Order in Project Settings.

            SpawnLeonidas();
        }

        private void SpawnLeonidas()
        {
            var stats = CharacterData.Leonidas;

            // Create GameObject
            var go = new GameObject("Leonidas");
            go.transform.position = roomBuilder != null
                ? (Vector3)(Vector2)roomBuilder.SpawnPoint
                : Vector3.zero;

            // Sprite
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = CharacterSpriteFactory.CreateTopDownSprite(stats);
            sr.sortingOrder = 10;

            // Direction indicator (child)
            // The direction indicator is baked into the sprite texture.
            // Rotation of the parent GO handles aiming direction.

            // Physics
            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.freezeRotation = true;
            rb.interpolation = RigidbodyInterpolation2D.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode2D.Continuous;

            var col = go.AddComponent<CircleCollider2D>();
            col.radius = stats.Radius;

            // Scripts
            go.AddComponent<HY3RIDOrigins.Controllers.PlayerController>();
            var leonidas = go.AddComponent<Leonidas>();

            // Camera
            if (labCamera != null)
                labCamera.SetTarget(go.transform);
        }
    }
}
