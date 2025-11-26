using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Displays the knockdown meter bar above the character.
    /// Extracted from CharacterInfoDisplay for single responsibility.
    /// </summary>
    public class KnockdownMeterBarUI : MonoBehaviour, IScreenSpaceUI
    {
        [Header("Component References")]
        [SerializeField] private KnockdownMeterTracker knockdownMeter;

        [Header("Display Settings")]
        [SerializeField] private bool showMeterBar = true;
        [SerializeField] private UIDisplayMode displayMode = UIDisplayMode.OnCharacter;
        [SerializeField] private bool isPlayerCharacter = true;
        [SerializeField] private float heightOffset = 2.6f;
        [SerializeField] private float barWidth = 150f;
        [SerializeField] private float barHeight = 4f;

        [Header("Colors")]
        [SerializeField] private Color meterColor = new Color(0.8f, 0.6f, 0.2f);
        [SerializeField] private Color knockbackColor = new Color(0.9f, 0.7f, 0.3f);
        [SerializeField] private Color knockdownColor = new Color(1.0f, 0.4f, 0.2f);
        [SerializeField] private Color meterBgColor = new Color(0.2f, 0.15f, 0.05f, 0.5f);
        [SerializeField] private Color outlineColor = Color.black;

        private Camera mainCamera;
        private float currentMeter;
        private float maxMeter;
        private bool hasTriggeredKnockback;
        private bool isKnockedDown;

        // IScreenSpaceUI implementation
        public Transform Owner => transform;
        public float ElementHeight => barHeight + 2f;
        public int StackOrder => 2; // Knockdown is third
        public string CharacterName => gameObject.name;
        public bool IsVisible => showMeterBar && knockdownMeter != null && (currentMeter > 0 || isKnockedDown);
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
            if (knockdownMeter == null)
                knockdownMeter = GetComponent<KnockdownMeterTracker>();
        }

        private void OnEnable()
        {
            if (knockdownMeter != null)
            {
                knockdownMeter.OnMeterChanged += UpdateMeterData;
                knockdownMeter.OnKnockbackTriggered += HandleKnockback;
                knockdownMeter.OnMeterKnockdownTriggered += HandleKnockdown;
                UpdateMeterData(knockdownMeter.CurrentMeter, knockdownMeter.MaxMeter);
            }
            // Note: Registration with ScreenSpaceUIManager is handled by CharacterInfoDisplay.ApplyDisplaySettings()
        }

        private void OnDisable()
        {
            if (knockdownMeter != null)
            {
                knockdownMeter.OnMeterChanged -= UpdateMeterData;
                knockdownMeter.OnKnockbackTriggered -= HandleKnockback;
                knockdownMeter.OnMeterKnockdownTriggered -= HandleKnockdown;
            }

            // Unregister from screen-space manager
            ScreenSpaceUIManager.Instance?.Unregister(this);
        }

        private void UpdateMeterData(float current, float max)
        {
            currentMeter = current;
            maxMeter = max;
        }

        private void HandleKnockback()
        {
            hasTriggeredKnockback = true;
        }

        private void HandleKnockdown()
        {
            isKnockedDown = true;
            // Reset after a delay
            Invoke(nameof(ResetKnockdownState), 2f);
        }

        private void ResetKnockdownState()
        {
            isKnockedDown = false;
            hasTriggeredKnockback = false;
        }

        private void OnGUI()
        {
            if (!showMeterBar || knockdownMeter == null)
                return;

            // Only show if meter has some value
            if (currentMeter <= 0 && !isKnockedDown)
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

            DrawKnockdownMeterBar(screenX, screenY);
        }

        private void DrawKnockdownMeterBar(float centerX, float centerY)
        {
            if (maxMeter <= 0) return;

            float barLeft = centerX - barWidth / 2;
            float barTop = centerY - barHeight / 2;

            // Background bar (dark gray)
            Color originalColor = GUI.color;
            float meterPercent = Mathf.Clamp01(currentMeter / maxMeter);
            Rect bgRect = new Rect(barLeft, barTop, barWidth, barHeight);
            GUI.color = new Color(0.2f, 0.2f, 0.2f, 0.8f);
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            // Meter fill with gradient (orange to red based on percentage)
            if (meterPercent > 0)
            {
                Color barColor;
                if (isKnockedDown)
                {
                    // Bright red when knocked down
                    barColor = new Color(1.0f, 0.1f, 0.1f, 0.9f);
                }
                else
                {
                    // Gradient from orange to red as meter fills
                    barColor = Color.Lerp(
                        new Color(0.9f, 0.6f, 0.2f, 0.9f), // Orange (low meter)
                        new Color(0.9f, 0.1f, 0.1f, 0.9f), // Red (high meter)
                        meterPercent
                    );
                }

                float fillWidth = barWidth * meterPercent;
                Rect fillRect = new Rect(barLeft, barTop, fillWidth, barHeight);
                GUI.color = barColor;
                GUI.DrawTexture(fillRect, Texture2D.whiteTexture);
            }

            // Draw threshold markers
            GUI.color = new Color(1f, 1f, 1f, 0.7f);

            // Knockback threshold (50%)
            float knockbackX = barLeft + (barWidth * 0.5f);
            GUI.DrawTexture(new Rect(knockbackX - 0.5f, barTop, 1.5f, barHeight), Texture2D.whiteTexture);

            // Knockdown threshold (100%)
            float knockdownX = barLeft + barWidth - 1;
            GUI.DrawTexture(new Rect(knockdownX, barTop, 1.5f, barHeight), Texture2D.whiteTexture);

            // Border
            GUI.color = Color.black;
            GUI.Box(bgRect, GUIContent.none);

            GUI.color = originalColor;
        }
    }
}