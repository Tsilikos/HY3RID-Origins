using System.Collections;
using UnityEngine;
using HY3RIDOrigins.Characters;
using HY3RIDOrigins.Controllers;

namespace HY3RIDOrigins.Combat
{
    // Handles Leonidas' weighted power-slide dodge.
    // Space: omnidirectional, uses movement direction (or facing if stationary).
    // Grants invulnerability for iFrameDuration seconds via Character.StartTimedInvulnerability.
    // Locks movement control during the slide (Character.ControlsLocked).
    // Spawns fading ghost sprites for visual trail.
    [RequireComponent(typeof(Rigidbody2D))]
    public class DodgeController : MonoBehaviour
    {
        [Header("Dodge (defaults match CharacterData.Leonidas)")]
        [SerializeField] private float dodgeSpeed    = 21.5f; // = dodgeDist / dodgeDuration
        [SerializeField] private float dodgeDuration = 0.14f;
        [SerializeField] private float iFrameDuration = 0.11f;
        [SerializeField] private float cooldown      = 0.85f;

        [Header("Trail")]
        [SerializeField] private float ghostInterval = 0.035f;
        [SerializeField] private float ghostLifetime = 0.15f;

        private Character        character;
        private PlayerController input;
        private Rigidbody2D      rb;
        private SpriteRenderer   characterSR;

        private bool  isDodging;
        private float nextDodgeTime;

        public bool IsDodging    => isDodging;
        public bool CanDodge     => !isDodging && Time.time >= nextDodgeTime;
        public float CooldownLeft => Mathf.Max(0f, nextDodgeTime - Time.time);

        private void Awake()
        {
            character   = GetComponent<Character>();
            input       = GetComponent<PlayerController>();
            rb          = GetComponent<Rigidbody2D>();
            characterSR = GetComponent<SpriteRenderer>();
        }

        private void Update()
        {
            if (!isDodging && input != null && input.DodgeJustDown && CanDodge)
                StartCoroutine(DodgeRoutine());
        }

        private IEnumerator DodgeRoutine()
        {
            isDodging = true;
            character.ControlsLocked = true;
            nextDodgeTime = Time.time + dodgeDuration + cooldown;

            // Direction: movement input takes priority; fall back to facing direction
            Vector2 dir = input != null && input.MoveInput.sqrMagnitude > 0.01f
                ? input.MoveInput.normalized
                : new Vector2(Mathf.Cos(character.FacingAngle), Mathf.Sin(character.FacingAngle));

            character.StartTimedInvulnerability(iFrameDuration);

            float elapsed   = 0f;
            float trailTimer = 0f;

            while (elapsed < dodgeDuration)
            {
                // Ease out: fast start, decelerates to zero
                float t     = elapsed / dodgeDuration;
                float speed = Mathf.Lerp(dodgeSpeed, 0f, t * t);
                rb.linearVelocity = dir * speed;

                trailTimer += Time.deltaTime;
                if (trailTimer >= ghostInterval)
                {
                    trailTimer = 0f;
                    SpawnGhost();
                }

                elapsed += Time.deltaTime;
                yield return null;
            }

            rb.linearVelocity    = Vector2.zero;
            character.ControlsLocked = false;
            isDodging = false;
        }

        private void SpawnGhost()
        {
            if (characterSR == null || characterSR.sprite == null) return;

            var go = new GameObject("DodgeGhost");
            go.transform.position   = transform.position;
            go.transform.rotation   = transform.rotation;
            go.transform.localScale = transform.localScale;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite       = characterSR.sprite;
            sr.color        = new Color(characterSR.color.r, characterSR.color.g,
                                        characterSR.color.b, 0.38f);
            sr.sortingOrder = characterSR.sortingOrder - 1;

            go.AddComponent<GhostFader>().Initialize(sr, ghostLifetime);
        }
    }

    // Fades a SpriteRenderer to transparent over its lifetime, then destroys the GO.
    // Defined alongside DodgeController — no separate file needed for this helper.
    internal class GhostFader : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float lifetime;
        private float elapsed;
        private Color startColor;

        internal void Initialize(SpriteRenderer r, float life)
        {
            sr         = r;
            lifetime   = life;
            startColor = r.color;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            if (t >= 1f) { Destroy(gameObject); return; }
            sr.color = new Color(startColor.r, startColor.g, startColor.b,
                                 startColor.a * (1f - t));
        }
    }
}
