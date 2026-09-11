using UnityEngine;
using UnityEngine.InputSystem;

namespace HY3RIDOrigins.DevTools
{
    // In-game debug panel. Press ` (backtick) to toggle.
    // Phase 1: shows Leonidas HP, shield, position, facing angle.
    // Later phases add: restart floor, skip floor, grant Evolution, spawn enemy, etc.
    public class DebugConsole : MonoBehaviour
    {
        [Header("Dev Controls")]
        [SerializeField] private bool enableInBuild; // Off in release builds — toggled from Inspector

        private bool visible = false;
        private GUIStyle boxStyle;
        private GUIStyle labelStyle;

        // Populated by LabSceneInitializer after Leonidas spawns.
        public static Characters.Leonidas LeonidassRef { get; set; }
        public static bool InvulnerabilityOn { get; private set; }

        private void Update()
        {
#if !UNITY_EDITOR
            if (!enableInBuild) return;
#endif
            var kb = Keyboard.current;
            if (kb != null && kb.backquoteKey.wasPressedThisFrame)
                visible = !visible;

            if (!visible) return;

            // G = toggle god mode (permanent invulnerability override)
            if (kb != null && kb.gKey.wasPressedThisFrame)
            {
                InvulnerabilityOn = !InvulnerabilityOn;
                LeonidassRef?.SetInvulnerable(InvulnerabilityOn);
                UnityEngine.Debug.Log($"[Debug] God mode: {InvulnerabilityOn}");
            }

            // I = apply 15 test damage (respects invulnerability — use to verify dodge i-frames)
            if (kb != null && kb.iKey.wasPressedThisFrame)
            {
                LeonidassRef?.TakeDamage(15f);
                UnityEngine.Debug.Log("[Debug] Applied 15 test damage");
            }
        }

        private void OnGUI()
        {
#if !UNITY_EDITOR
            if (!enableInBuild) return;
#endif
            if (!visible) return;

            if (boxStyle == null)
            {
                boxStyle = new GUIStyle(GUI.skin.box)
                {
                    fontSize = 13,
                    alignment = TextAnchor.UpperLeft,
                };
                labelStyle = new GUIStyle(GUI.skin.label)
                {
                    fontSize = 13,
                    normal = { textColor = Color.green },
                };
            }

            var leo = LeonidassRef;
            string info = leo == null
                ? "Leonidas: not spawned"
                : $"Leonidas\n" +
                  $"  HP:     {leo.Hp:F0} / {leo.MaxHp:F0}\n" +
                  $"  Shield: {leo.Shield:F0} / {leo.MaxShield:F0}\n" +
                  $"  Pos:    {leo.transform.position.x:F1}, {leo.transform.position.y:F1}\n" +
                  $"  Facing: {leo.FacingAngle * Mathf.Rad2Deg:F0}°\n" +
                  $"  Invuln: {InvulnerabilityOn}\n\n" +
                  $"Keys\n" +
                  $"  ` = toggle this panel\n" +
                  $"  G = toggle god mode (invuln)\n" +
                  $"  I = apply 15 test damage";

            GUI.Box(new Rect(10, 10, 280, 180), "");
            GUI.Label(new Rect(16, 16, 270, 170), info, labelStyle);
        }
    }
}
