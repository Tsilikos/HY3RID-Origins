using UnityEngine;
using HY3RIDOrigins.Combat;

namespace HY3RIDOrigins.Testing
{
    // Development-only stationary target for weapon testing.
    // Color indicates remaining HP: green → yellow → red → white flash on death.
    // Implements IDamageable — hit by Projectile and Spear.
    [RequireComponent(typeof(SpriteRenderer), typeof(BoxCollider2D))]
    public class TestTarget : MonoBehaviour, IDamageable
    {
        [SerializeField] private float maxHp = 80f;
        private float currentHp;
        private SpriteRenderer sr;

        private void Awake()
        {
            currentHp = maxHp;
            sr = GetComponent<SpriteRenderer>();
            UpdateColor();
        }

        public void TakeDamage(float amount, bool fromFront = false)
        {
            currentHp = Mathf.Max(0f, currentHp - amount);
            UpdateColor();
            if (currentHp <= 0f)
                StartCoroutine(DeathFlash());
        }

        private void UpdateColor()
        {
            if (sr == null) return;
            float t = currentHp / maxHp;
            sr.color = t > 0.5f
                ? Color.Lerp(new Color(1f, 0.85f, 0f), Color.green, (t - 0.5f) * 2f) // yellow → green
                : Color.Lerp(Color.red, new Color(1f, 0.85f, 0f), t * 2f);            // red → yellow
        }

        private System.Collections.IEnumerator DeathFlash()
        {
            if (sr != null) sr.color = Color.white;
            yield return new WaitForSeconds(0.25f);
            Destroy(gameObject);
        }
    }
}
