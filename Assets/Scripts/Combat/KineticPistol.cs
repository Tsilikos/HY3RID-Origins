using System.Collections;
using UnityEngine;

namespace HY3RIDOrigins.Combat
{
    // Leonidas' ranged weapon. Standalone component — can be equipped on any character.
    // WeaponHolder routes player input to TryFire/TryReload.
    // AI controllers call the same methods directly.
    public class KineticPistol : MonoBehaviour
    {
        [Header("Stats")]
        [SerializeField] private float damage          = 18f;
        [SerializeField] private float fireRate        = 7f;    // shots/second
        [SerializeField] private float projectileSpeed = 22f;
        [SerializeField] private int   magazineSize    = 12;
        [SerializeField] private float reloadDuration  = 1.4f;

        private int   currentAmmo;
        private float nextFireTime;
        private bool  isReloading;

        private SpriteRenderer muzzleFlashSR;

        public int   CurrentAmmo  => currentAmmo;
        public int   MagazineSize => magazineSize;
        public bool  IsReloading  => isReloading;

        private void Awake()
        {
            currentAmmo = magazineSize;

            // Muzzle flash — brief bright circle parented to this GO
            var mfGO = new GameObject("MuzzleFlash");
            mfGO.transform.SetParent(transform);
            muzzleFlashSR = mfGO.AddComponent<SpriteRenderer>();
            muzzleFlashSR.sprite = SpriteUtil.Circle(16, new Color(1f, 0.9f, 0.45f));
            muzzleFlashSR.color  = new Color(1f, 0.9f, 0.45f, 0.9f);
            muzzleFlashSR.sortingOrder = 20;
            mfGO.transform.localScale = Vector3.one * 0.3f;
            mfGO.SetActive(false);
        }

        // Override SerializeField defaults at runtime — used by enemy factory code.
        public void Configure(float newDamage = 0f, float newFireRate = 0f,
            float newProjectileSpeed = 0f, int newMagazineSize = 0, float newReloadDuration = 0f)
        {
            if (newDamage         > 0f) damage          = newDamage;
            if (newFireRate       > 0f) fireRate         = newFireRate;
            if (newProjectileSpeed > 0f) projectileSpeed = newProjectileSpeed;
            if (newMagazineSize   > 0)  { magazineSize  = newMagazineSize; currentAmmo = newMagazineSize; }
            if (newReloadDuration > 0f) reloadDuration   = newReloadDuration;
        }

        // Returns true if a shot was fired. Called by WeaponHolder (player) or AIController.
        public bool TryFire(Vector2 origin, Vector2 direction)
        {
            if (isReloading || currentAmmo <= 0 || Time.time < nextFireTime)
                return false;

            direction = direction.normalized;
            SpawnBullet(origin + direction * 0.55f, direction);
            StartCoroutine(ShowMuzzleFlash(origin + direction * 0.45f));

            currentAmmo--;
            nextFireTime = Time.time + 1f / fireRate;

            if (currentAmmo <= 0)
                TryReload();

            return true;
        }

        public void TryReload()
        {
            if (!isReloading && currentAmmo < magazineSize)
                StartCoroutine(ReloadRoutine());
        }

        private void SpawnBullet(Vector2 pos, Vector2 dir)
        {
            var go = new GameObject("Bullet");
            go.transform.position = pos;
            go.tag = gameObject.tag; // inherit so Projectile can ignore owner

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = SpriteUtil.Circle(6, new Color(0.92f, 0.88f, 1f));
            sr.sortingOrder = 15;
            go.transform.localScale = Vector3.one * 0.22f;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.gravityScale = 0f;
            rb.linearVelocity = dir * projectileSpeed;

            var col = go.AddComponent<CircleCollider2D>();
            col.isTrigger = true;
            col.radius = 0.5f;

            var proj = go.AddComponent<Projectile>();
            proj.Initialize(damage, 28f, gameObject.tag);
        }

        private IEnumerator ShowMuzzleFlash(Vector2 worldPos)
        {
            muzzleFlashSR.gameObject.SetActive(true);
            muzzleFlashSR.transform.position = worldPos;
            yield return new WaitForSeconds(0.05f);
            muzzleFlashSR.gameObject.SetActive(false);
        }

        private IEnumerator ReloadRoutine()
        {
            isReloading = true;
            yield return new WaitForSeconds(reloadDuration);
            currentAmmo = magazineSize;
            isReloading = false;
        }
    }
}
