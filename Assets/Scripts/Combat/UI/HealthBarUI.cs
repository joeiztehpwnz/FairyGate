using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Displays a health bar above the character.
    /// Extracted from CharacterInfoDisplay for single responsibility.
    /// </summary>
    public class HealthBarUI : MonoBehaviour, IScreenSpaceUI
    {
        [Header("Component References")]
        [SerializeField] private HealthSystem healthSystem;

        [Header("Display Settings")]
        [SerializeField] private bool showHealthBar = true;
        [SerializeField] private UIDisplayMode displayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private bool isPlayerCharacter = true;
        [SerializeField] private float heightOffset = 3.0f;
        [SerializeField] private float barWidth = 150f;
        [SerializeField] private float barHeight = 8f;

        [Header("Colors")]
        [SerializeField] private Color healthColor = new Color(0.8f, 0.4f, 0.4f);
        [SerializeField] private Color healthBgColor = new Color(0.2f, 0.1f, 0.1f, 0.5f);
        [SerializeField] private Color outlineColor = Color.black;

        private Camera mainCamera;
        private int currentHealth;
        private int maxHealth;

        // IScreenSpaceUI implementation
        public Transform Owner => transform;
        public float ElementHeight => barHeight + 4f; // Include text space
        public int StackOrder => 0; // Health is first
        public string CharacterName => gameObject.name;
        public bool IsVisible => showHealthBar && healthSystem != null;
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
            if (healthSystem == null)
                healthSystem = GetComponent<HealthSystem>();
        }

        private void OnEnable()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged += UpdateHealthData;
                UpdateHealthData(healthSystem.CurrentHealth, healthSystem.MaxHealth);
            }
            // Note: Registration with ScreenSpaceUIManager is handled by CharacterInfoDisplay.ApplyDisplaySettings()
        }

        private void OnDisable()
        {
            if (healthSystem != null)
            {
                healthSystem.OnHealthChanged -= UpdateHealthData;
            }

            // Unregister from screen-space manager
            ScreenSpaceUIManager.Instance?.Unregister(this);
        }

        private void UpdateHealthData(int current, int max)
        {
            currentHealth = current;
            maxHealth = max;
        }

        private void OnGUI()
        {
            if (!showHealthBar || healthSystem == null)
                return;

            float screenX, screenY;

            if (displayMode == UIDisplayMode.OnScreenSide)
            {
                // Get position from screen-space manager
                if (ScreenSpaceUIManager.Instance == null)
                    return;

                Vector2 pos = ScreenSpaceUIManager.Instance.GetScreenPosition(this);
                screenX = pos.x + barWidth / 2; // Center the bar at the position
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

            DrawHealthBar(screenX, screenY);
        }

        private void DrawHealthBar(float centerX, float centerY)
        {
            if (maxHealth <= 0) return;

            float barLeft = centerX - barWidth / 2;
            float barTop = centerY - barHeight / 2;

            // Background bar (dark gray)
            Color originalColor = GUI.color;
            float percentage = Mathf.Clamp01((float)currentHealth / maxHealth);
            Rect bgRect = new Rect(barLeft, barTop, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Health bar (red to green gradient based on percentage)
            if (percentage > 0)
            {
                Color barColor = Color.Lerp(
                    new Color(0.9f, 0.1f, 0.1f, 0.9f), // Red (low health)
                    new Color(0.1f, 0.9f, 0.1f, 0.9f), // Green (full health)
                    percentage
                );

                Rect fillRect = new Rect(barLeft, barTop, barWidth * percentage, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            // Health text
            GUI.color = Color.white;
            string healthText = $"{currentHealth}/{maxHealth}";
            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.alignment = TextAnchor.MiddleCenter;
            style.fontSize = 10;
            style.normal.textColor = Color.white;

            GUI.Label(new Rect(barLeft, barTop, barWidth, barHeight), healthText, style);
            GUI.color = originalColor;
        }
    }
}
