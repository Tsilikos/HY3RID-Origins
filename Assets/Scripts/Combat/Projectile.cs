using UnityEngine;

namespace HY3RIDOrigins.Combat
{
    // Kinetic pistol bullet. Moves via Rigidbody2D velocity set by KineticPistol.
    // Deals damage to any IDamageable it enters. Destroys itself on hit or when out of range.
    [RequireComponent(typeof(Rigidbody2D), typeof(CircleCollider2D))]
    public class Projectile : MonoBehaviour
    {
        private float  damage;
        private float  maxRange;
        private string ownerTag;
        private Vector2 spawnPos;

        public void Initialize(float dmg, float range, string tag)
        {
            damage   = dmg;
            maxRange = range;
            ownerTag = tag;
            spawnPos = transform.position;
        }

        private void Update()
        {
            if (Vector2.SqrMagnitude((Vector2)transform.position - spawnPos) > maxRange * maxRange)
                Destroy(gameObject);
        }

        private void OnTriggerEnter2D(Collider2D other)
        {
            if (other.isTrigger) return;
            if (other.gameObject.CompareTag(ownerTag)) return;

            var target = other.GetComponentInParent<IDamageable>();

            if (target != null)
            {
                // Ask the hit character whether the bullet came from their front.
                // This activates front-facing passives (e.g. Leonidas' Resilience).
                // Characters with DamageReductionFront = 0 are unaffected regardless.
                var character  = other.GetComponentInParent<HY3RIDOrigins.Characters.Character>();
                bool fromFront = character != null && character.IsFacingToward(transform.position);
                target.TakeDamage(damage, fromFront);
                HitEffect.Spawn(transform.position, 0.30f, Color.white, 0.09f);
            }
            else
            {
                // Wall or obstacle — no damage, spark only
                HitEffect.Spawn(transform.position, 0.12f, new Color(0.9f, 0.75f, 0.4f), 0.06f);
            }

            Destroy(gameObject);
        }
    }
}
