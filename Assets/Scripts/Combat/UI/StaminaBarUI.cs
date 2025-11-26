using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Displays a stamina bar above the character.
    /// Extracted from CharacterInfoDisplay for single responsibility.
    /// </summary>
    public class StaminaBarUI : MonoBehaviour, IScreenSpaceUI
    {
        [Header("Component References")]
        [SerializeField] private StaminaSystem staminaSystem;

        [Header("Display Settings")]
        [SerializeField] private bool showStaminaBar = true;
        [SerializeField] private UIDisplayMode displayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private bool isPlayerCharacter = true;
        [SerializeField] private float heightOffset = 2.8f;
        [SerializeField] private float barWidth = 150f;
        [SerializeField] private float barHeight = 6f;

        [Header("Colors")]
        [SerializeField] private Color staminaColor = new Color(0.2f, 0.6f, 0.2f);
        [SerializeField] private Color staminaBgColor = new Color(0.1f, 0.2f, 0.1f, 0.5f);
        [SerializeField] private Color outlineColor = Color.black;
        [SerializeField] private Color restingColor = new Color(0.4f, 0.8f, 0.4f);

        private Camera mainCamera;
        private int currentStamina;
        private int maxStamina;
        private bool isResting;

        // IScreenSpaceUI implementation
        public Transform Owner => transform;
        public float ElementHeight => barHeight + 4f;
        public int StackOrder => 1; // Stamina is second
        public string CharacterName => gameObject.name;
        public bool IsVisible => showStaminaBar && staminaSystem != null;
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
            if (staminaSystem == null)
                staminaSystem = GetComponent<StaminaSystem>();
        }

        private void OnEnable()
        {
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged += UpdateStaminaData;
                UpdateStaminaData(staminaSystem.CurrentStamina, staminaSystem.MaxStamina);
            }
            // Note: Registration with ScreenSpaceUIManager is handled by CharacterInfoDisplay.ApplyDisplaySettings()
        }

        private void OnDisable()
        {
            if (staminaSystem != null)
            {
                staminaSystem.OnStaminaChanged -= UpdateStaminaData;
            }

            // Unregister from screen-space manager
            ScreenSpaceUIManager.Instance?.Unregister(this);
        }

        private void UpdateStaminaData(int current, int max)
        {
            currentStamina = current;
            maxStamina = max;
            if (staminaSystem != null)
            {
                isResting = staminaSystem.IsResting;
            }
        }

        private void OnGUI()
        {
            if (!showStaminaBar || staminaSystem == null)
                return;

            float screenX, screenY;

            if (displayMode == UIDisplayMode.OnScreenSide)
            {
                // Get position from screen-space manager
                if (ScreenSpaceUIManager.Instance == null)
                    return;

                Vector2 pos = ScreenSpaceUIManager.Instance.GetScreenPosition(this);
                screenX = pos.x + barWidth / 2;
                screenY = pos.y;
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

            DrawStaminaBar(screenX, screenY);
        }

        private void DrawStaminaBar(float centerX, float centerY)
        {
            if (maxStamina <= 0) return;

            float barLeft = centerX - barWidth / 2;
            float barTop = centerY - barHeight / 2;

            // Background bar (dark gray)
            Color originalColor = GUI.color;
            float percentage = Mathf.Clamp01((float)currentStamina / maxStamina);
            Rect bgRect = new Rect(barLeft, barTop, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Stamina bar (green to yellow to red based on percentage)
            if (percentage > 0)
            {
                Color barColor;
                if (percentage > 0.6f)
                    barColor = new Color(0.2f, 0.8f, 0.2f, 0.9f); // Green
                else if (percentage > 0.3f)
                    barColor = new Color(0.9f, 0.9f, 0.2f, 0.9f); // Yellow
                else
                    barColor = new Color(0.9f, 0.2f, 0.2f, 0.9f); // Red

                Rect fillRect = new Rect(barLeft, barTop, barWidth * percentage, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Stamina text
            GUI.color = Color.white;
            string staminaText = isResting ? $"{currentStamina}/{maxStamina} (Resting)" : $"{currentStamina}/{maxStamina}";
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 9;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(barLeft, barTop, barWidth, barHeight), staminaText, style);
            GUI.color = originalColor;
        }
    }
}