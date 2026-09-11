using UnityEngine;
using UnityEngine.Rendering.Universal;
using HY3RIDOrigins.Characters;

namespace HY3RIDOrigins.Visibility
{
    // Reusable per-character flashlight powered by URP 2D lighting.
    // Drop onto any Character GO. Creates a child Light2D on Awake.
    // Reads Character.FacingAngle each LateUpdate — zero coupling to weapons or movement.
    // Toggle SetEnabled() for blackout state (future Evolutions, Phase 7+).
    [RequireComponent(typeof(Character))]
    public class VisibilityLight : MonoBehaviour
    {
        [Header("Cone shape")]
        [SerializeField] private float outerAngle  = 70f;   // total cone width, degrees
        [SerializeField] private float innerAngle  = 45f;   // inner bright region, degrees
        [SerializeField] private float outerRadius = 16f;   // max reach in world units
        [SerializeField] private float innerRadius = 4f;    // full-brightness falloff start

        [Header("Appearance")]
        [SerializeField] private Color lightColor = new Color(0.85f, 0.90f, 1.00f); // cool white
        [SerializeField] private float intensity = 1.5f;
        [SerializeField] [Range(0f, 1f)] private float falloff = 0.7f;

        [Header("Shadows")]
        [SerializeField] private bool  enableShadows  = true;
        [SerializeField] [Range(0f, 1f)] private float shadowStrength = 0.90f;

        private Character owner;
        private Light2D   cone;

        private void Awake()
        {
            owner = GetComponent<Character>();

            var coneGO = new GameObject("FlashlightCone");
            coneGO.transform.SetParent(transform, false);
            coneGO.transform.localPosition = Vector3.zero;

            cone = coneGO.AddComponent<Light2D>();
            cone.lightType              = Light2D.LightType.Point;
            cone.pointLightOuterAngle   = outerAngle;
            cone.pointLightInnerAngle   = innerAngle;
            cone.pointLightOuterRadius  = outerRadius;
            cone.pointLightInnerRadius  = innerRadius;
            cone.color                  = lightColor;
            cone.intensity              = intensity;
            cone.falloffIntensity       = falloff;
            cone.shadowsEnabled         = enableShadows;
            cone.shadowIntensity        = shadowStrength;
        }

        private void LateUpdate()
        {
            if (owner == null) return;
            // URP 2D Point-light cone bisector points along the child's local +Y.
            // Character.FacingAngle: 0=right, π/2=up.
            // eulerZ = FacingAngle_deg - 90 → local +Y == facing direction (matches FaceAngle() logic).
            cone.transform.rotation = Quaternion.Euler(0f, 0f,
                owner.FacingAngle * Mathf.Rad2Deg - 90f);
        }

        // Override the color after Awake() — called by factory code that can't set SerializeField.
        public void SetColor(Color color)
        {
            lightColor   = color;
            if (cone != null) cone.color = color;
        }

        // Toggle the cone light without destroying the component (blackout, stealth, etc.).
        public void SetEnabled(bool on) => cone.enabled = on;

        public Light2D Light => cone;
    }
}
