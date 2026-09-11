using UnityEngine;

namespace HY3RIDOrigins.Combat
{
    // Spawns a brief fading circle at a world position. Used by Projectile and Spear for hit feedback.
    public class HitEffect : MonoBehaviour
    {
        private SpriteRenderer sr;
        private float lifetime;
        private float elapsed;
        private Color startColor;

        public static void Spawn(Vector2 pos, float worldSize, Color color, float duration = 0.08f)
        {
            var go = new GameObject("HitEffect");
            go.transform.position = pos;
            go.transform.localScale = Vector3.one * worldSize;
            var r = go.AddComponent<SpriteRenderer>();
            r.sprite = SpriteUtil.Circle(16, color);
            r.color  = color;
            r.sortingOrder = 30;
            var h = go.AddComponent<HitEffect>();
            h.sr         = r;
            h.startColor = color;
            h.lifetime   = duration;
        }

        private void Update()
        {
            elapsed += Time.deltaTime;
            float t = elapsed / lifetime;
            if (t >= 1f) { Destroy(gameObject); return; }
            sr.color = new Color(startColor.r, startColor.g, startColor.b, 1f - t);
        }
    }
}
