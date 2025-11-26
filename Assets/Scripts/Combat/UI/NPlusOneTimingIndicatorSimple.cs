using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Simple visual indicator for N+1 combo timing window using OnGUI.
    /// Zero setup required - just attach to player GameObject.
    /// Shows stun progress bar with color-coded timing window.
    /// </summary>
    public class NPlusOneTimingIndicatorSimple : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool enableIndicator = true;
        [SerializeField] private Vector2 screenPosition = new Vector2(0.5f, 0.15f); // Normalized screen position (0-1)
        [SerializeField] private Vector2 barSize = new Vector2(300f, 30f);

        [Header("Colors")]
        [SerializeField] private Color buildupColor = new Color(1f, 0.8f, 0f, 0.9f);      // Yellow - building up
        [SerializeField] private Color windowColor = new Color(0f, 1f, 0.3f, 1f);         // Green - execute now!
        [SerializeField] private Color closingColor = new Color(1f, 0.2f, 0f, 0.9f);      // Red - window closing
        [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.8f);

        private WeaponController weaponController;
        private GUIStyle barStyle;
        private GUIStyle textStyle;
        private GUIStyle windowMarkerStyle;
        private bool stylesInitialized = false;

        private void Awake()
        {
            weaponController = GetComponent<WeaponController>();
            if (weaponController == null)
            {
                CombatLogger.LogCombat($"NPlusOneTimingIndicatorSimple on {gameObject.name} has no WeaponController", CombatLogger.LogLevel.Warning);
            }
        }

        private void InitializeStyles()
        {
            if (stylesInitialized) return;

            // Bar background/fill style
            barStyle = new GUIStyle();
            barStyle.normal.background = MakeTex(2, 2, Color.white);

            // Text style
            textStyle = new GUIStyle();
            textStyle.alignment = TextAnchor.MiddleCenter;
            textStyle.fontSize = 16;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.normal.textColor = Color.white;

            // Window marker style
            windowMarkerStyle = new GUIStyle();
            windowMarkerStyle.normal.background = MakeTex(2, 2, Color.white);

            stylesInitialized = true;
        }

        private void OnGUI()
        {
            if (!enableIndicator || weaponController == null) return;

            InitializeStyles();

            float stunProgress = weaponController.CurrentStunProgress;
            bool isTracking = stunProgress > 0f && stunProgress < 1f;

            if (!isTracking) return;

            // Calculate screen position
            float posX = Screen.width * screenPosition.x - barSize.x * 0.5f;
            float posY = Screen.height * screenPosition.y;

            Rect barRect = new Rect(posX, posY, barSize.x, barSize.y);
            Rect fillRect = new Rect(posX, posY, barSize.x * stunProgress, barSize.y);

            // Determine current color based on progress
            Color currentColor;
            string statusText;

            if (stunProgress < CombatConstants.N_PLUS_ONE_WINDOW_START)
            {
                // Building up (0-70%)
                currentColor = buildupColor;
                statusText = $"{Mathf.RoundToInt(stunProgress * 100)}%";
            }
            else if (stunProgress <= CombatConstants.N_PLUS_ONE_WINDOW_END)
            {
                // In execution window (70-95%)
                currentColor = windowColor;
                statusText = ">>> EXECUTE! <<<";
            }
            else
            {
                // Window closing (95-100%)
                currentColor = closingColor;
                statusText = "MISSED";
            }

            // Draw background
            GUI.color = backgroundColor;
            GUI.Box(barRect, "", barStyle);

            // Draw window markers (70% and 95%)
            float windowStartX = posX + (barSize.x * CombatConstants.N_PLUS_ONE_WINDOW_START);
            float windowEndX = posX + (barSize.x * CombatConstants.N_PLUS_ONE_WINDOW_END);

            GUI.color = new Color(0f, 1f, 0.3f, 0.3f);
            Rect windowZone = new Rect(windowStartX, posY, windowEndX - windowStartX, barSize.y);
            GUI.Box(windowZone, "", windowMarkerStyle);

            // Draw fill bar
            GUI.color = currentColor;
            GUI.Box(fillRect, "", barStyle);

            // Draw border
            GUI.color = Color.white;
            DrawBorder(barRect, 2);

            // Draw marker lines at 70% and 95%
            GUI.color = windowColor;
            Rect startMarker = new Rect(windowStartX - 1, posY, 2, barSize.y);
            Rect endMarker = new Rect(windowEndX - 1, posY, 2, barSize.y);
            GUI.Box(startMarker, "", barStyle);
            GUI.Box(endMarker, "", barStyle);

            // Draw text
            GUI.color = Color.white;
            textStyle.normal.textColor = currentColor;
            GUI.Label(barRect, statusText, textStyle);

            // Draw percentage below bar
            Rect percentRect = new Rect(posX, posY + barSize.y + 5, barSize.x, 20);
            textStyle.fontSize = 12;
            textStyle.normal.textColor = Color.white;
            GUI.Label(percentRect, $"Stun Progress: {Mathf.RoundToInt(stunProgress * 100)}% | Window: 70-95%", textStyle);
            textStyle.fontSize = 16; // Reset

            // Reset color
            GUI.color = Color.white;
        }

        private void DrawBorder(Rect rect, float thickness)
        {
            // Top
            GUI.Box(new Rect(rect.x, rect.y, rect.width, thickness), "", barStyle);
            // Bottom
            GUI.Box(new Rect(rect.x, rect.y + rect.height - thickness, rect.width, thickness), "", barStyle);
            // Left
            GUI.Box(new Rect(rect.x, rect.y, thickness, rect.height), "", barStyle);
            // Right
            GUI.Box(new Rect(rect.x + rect.width - thickness, rect.y, thickness, rect.height), "", barStyle);
        }

        private Texture2D MakeTex(int width, int height, Color color)
        {
            Color[] pixels = new Color[width * height];
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = color;
            }

            Texture2D texture = new Texture2D(width, height);
            texture.SetPixels(pixels);
            texture.Apply();
            return texture;
        }

        private void OnValidate()
        {
            // Clamp screen position to valid range
            screenPosition.x = Mathf.Clamp01(screenPosition.x);
            screenPosition.y = Mathf.Clamp01(screenPosition.y);

            // Ensure minimum bar size
            barSize.x = Mathf.Max(100f, barSize.x);
            barSize.y = Mathf.Max(20f, barSize.y);
        }
    }
}
