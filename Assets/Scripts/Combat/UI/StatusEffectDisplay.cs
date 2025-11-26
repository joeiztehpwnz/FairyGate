using UnityEngine;
using System.Linq;

namespace FairyGate.Combat
{
    /// <summary>
    /// Displays status effects above the character.
    /// Extracted from CharacterInfoDisplay for single responsibility.
    /// </summary>
    public class StatusEffectDisplay : MonoBehaviour, IScreenSpaceUI
    {
        [Header("Component References")]
        [SerializeField] private StatusEffectManager statusEffectManager;

        [Header("Display Settings")]
        [SerializeField] private bool showStatusEffects = true;
        [SerializeField] private UIDisplayMode displayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private bool isPlayerCharacter = true;
        [SerializeField] private float heightOffset = 3.8f;
        [SerializeField] private float panelWidth = 150f;
        [SerializeField] private float lineHeight = 20f;
        [SerializeField] private int fontSize = 11;

        [Header("Colors")]
        [SerializeField] private Color textColor = Color.white;
        [SerializeField] private Color backgroundColor = new Color(0, 0, 0, 0.7f);

        private Camera mainCamera;
        private string currentStatusText = "";

        // IScreenSpaceUI implementation
        public Transform Owner => transform;
        public float ElementHeight => lineHeight * 2 + 4f;
        public int StackOrder => 3; // Status effects are fourth
        public string CharacterName => gameObject.name;
        public bool IsVisible => showStatusEffects && statusEffectManager != null && !string.IsNullOrEmpty(currentStatusText);
        public UIDisplayMode DisplayMode => displayMode;

        /// <summary>
        /// Set the display mode and player character flag from CharacterInfoDisplay.
        /// </summary>
        public void SetDisplayMode(UIDisplayMode mode, bool isPlayer)
        {
            // Always unregister first to handle isPlayer changes
            ScreenSpaceUIManager.Instance?.Unregister(this);

            displayMode = mode;
            isPlayerCharacter = isPlayer;

            // Register with new mode if needed
            if (displayMode == UIDisplayMode.OnScreenSide && enabled)
            {
                ScreenSpaceUIManager.EnsureExists();
                ScreenSpaceUIManager.Instance?.Register(this, isPlayerCharacter);
            }
        }

        private void Awake()
        {
            mainCamera = Camera.main;

            // Auto-find component if not assigned
            if (statusEffectManager == null)
                statusEffectManager = GetComponent<StatusEffectManager>();
        }

        private void OnEnable()
        {
            // Note: Registration with ScreenSpaceUIManager is handled by CharacterInfoDisplay.ApplyDisplaySettings()
        }

        private void OnDisable()
        {
            // Unregister from screen-space manager
            ScreenSpaceUIManager.Instance?.Unregister(this);
        }

        private void Update()
        {
            // Update status text
            if (showStatusEffects && statusEffectManager != null)
            {
                UpdateStatusText();
            }
        }

        private void UpdateStatusText()
        {
            currentStatusText = "";
            var activeEffects = statusEffectManager.ActiveStatusEffects.Where(e => e.isActive).ToList();

            foreach (var effect in activeEffects)
            {
                if (!string.IsNullOrEmpty(currentStatusText))
                    currentStatusText += ", ";
                currentStatusText += $"{effect.type}: {effect.remainingTime:F1}s";
            }
        }

        private void OnGUI()
        {
            if (!showStatusEffects || statusEffectManager == null)
                return;

            // Skip if no active status effects
            if (string.IsNullOrEmpty(currentStatusText))
                return;

            float screenX, screenY;

            if (displayMode == UIDisplayMode.OnScreenSide)
            {
                // Get position from screen-space manager
                if (ScreenSpaceUIManager.Instance == null)
                    return;

                Vector2 pos = ScreenSpaceUIManager.Instance.GetScreenPosition(this);
                screenX = pos.x + panelWidth / 2;
                screenY = pos.y + lineHeight; // Offset for centered drawing
            }
            else
            {
                // World-space positioning (on character)
                if (mainCamera == null)
                    return;

                Vector3 worldPos = transform.position + Vector3.up * heightOffset;
                Vector3 screenPos = mainCamera.WorldToScreenPoint(worldPos);

                // Only draw if in front of camera
                if (screenPos.z <= 0)
                    return;

                screenX = screenPos.x;
                screenY = Screen.height - screenPos.y;
            }

            DrawStatusEffects(screenX, screenY);
        }

        private void DrawStatusEffects(float centerX, float centerY)
        {
            // Set up GUI style
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = fontSize;
            style.normal.textColor = textColor;

            // Calculate background rectangle
            Rect backgroundRect = new Rect(
                centerX - panelWidth / 2,
                centerY - lineHeight,
                panelWidth,
                lineHeight * 2
            );

            // Draw background
            Color originalColor = GUI.color;
            GUI.color = backgroundColor;
            GUI.DrawTexture(backgroundRect, Texture2D.whiteTexture);

            // Draw text
            GUI.color = textColor;
            GUI.Label(backgroundRect, currentStatusText, style);

            // Reset color
            GUI.color = originalColor;
        }
    }
}