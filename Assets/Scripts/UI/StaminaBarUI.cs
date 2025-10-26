using UnityEngine;

namespace FairyGate.Combat.UI
{
    public class StaminaBarUI : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private Vector2 barPosition = new Vector2(10, 40);
        [SerializeField] private Vector2 barSize = new Vector2(200, 20);
        [SerializeField] private Color fullStaminaColor = Color.blue;
        [SerializeField] private Color lowStaminaColor = Color.red;
        [SerializeField] private Color restingColor = Color.cyan;

        [Header("Configuration")]
        [SerializeField] private bool showStaminaNumbers = true;
        [SerializeField] private bool animateChanges = true;
        [SerializeField] private float animationSpeed = 10f;
        [SerializeField] private float lowStaminaThreshold = 0.25f;

        private StaminaSystem targetStaminaSystem;
        private float currentDisplayedStamina;
        private float targetStamina;
        private int maxStamina;
        private bool isResting = false;

        private void Awake()
        {
            // No UI components needed - using OnGUI for display
        }

        private void Update()
        {
            if (targetStaminaSystem != null && animateChanges)
            {
                AnimateStaminaBar();
            }
        }

        public void SetTargetStaminaSystem(StaminaSystem staminaSystem)
        {
            // Unsubscribe from previous target
            if (targetStaminaSystem != null)
            {
                targetStaminaSystem.OnStaminaChanged -= OnStaminaChanged;
                targetStaminaSystem.OnRestStarted -= OnRestStarted;
                targetStaminaSystem.OnRestStopped -= OnRestStopped;
            }

            // Subscribe to new target
            targetStaminaSystem = staminaSystem;
            if (targetStaminaSystem != null)
            {
                targetStaminaSystem.OnStaminaChanged += OnStaminaChanged;
                targetStaminaSystem.OnRestStarted += OnRestStarted;
                targetStaminaSystem.OnRestStopped += OnRestStopped;

                // Initialize display
                maxStamina = targetStaminaSystem.MaxStamina;
                targetStamina = targetStaminaSystem.CurrentStamina;
                currentDisplayedStamina = targetStamina;
                isResting = targetStaminaSystem.IsResting;
                UpdateStaminaDisplay();
            }
        }

        private void OnStaminaChanged(int currentStamina, int maxStaminaValue)
        {
            maxStamina = maxStaminaValue;
            targetStamina = currentStamina;

            if (!animateChanges)
            {
                currentDisplayedStamina = targetStamina;
                UpdateStaminaDisplay();
            }
        }

        private void OnRestStarted()
        {
            isResting = true;
            UpdateStaminaDisplay();
        }

        private void OnRestStopped()
        {
            isResting = false;
            UpdateStaminaDisplay();
        }

        private void AnimateStaminaBar()
        {
            if (Mathf.Abs(currentDisplayedStamina - targetStamina) > 0.1f)
            {
                currentDisplayedStamina = Mathf.Lerp(currentDisplayedStamina, targetStamina, animationSpeed * Time.deltaTime);
                UpdateStaminaDisplay();
            }
            else
            {
                currentDisplayedStamina = targetStamina;
                UpdateStaminaDisplay();
            }
        }

        private void UpdateStaminaDisplay()
        {
            // Display handled in OnGUI
        }

        private void OnGUI()
        {
            if (targetStaminaSystem == null) return;

            float staminaPercentage = currentDisplayedStamina / maxStamina;

            // Draw background
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(barPosition.x - 2, barPosition.y - 2, barSize.x + 4, barSize.y + 4), Texture2D.whiteTexture);

            // Update color based on state
            Color barColor;
            if (isResting)
            {
                barColor = restingColor;
            }
            else if (staminaPercentage <= lowStaminaThreshold)
            {
                barColor = lowStaminaColor;
            }
            else
            {
                barColor = Color.Lerp(lowStaminaColor, fullStaminaColor,
                    (staminaPercentage - lowStaminaThreshold) / (1f - lowStaminaThreshold));
            }

            // Draw stamina bar
            GUI.color = barColor;
            GUI.DrawTexture(new Rect(barPosition.x, barPosition.y, barSize.x * staminaPercentage, barSize.y), Texture2D.whiteTexture);

            // Draw stamina text
            if (showStaminaNumbers)
            {
                GUI.color = Color.white;
                string restIndicator = isResting ? " (Resting)" : "";
                GUI.Label(new Rect(barPosition.x, barPosition.y + barSize.y + 5, barSize.x, 20),
                    $"Stamina: {Mathf.RoundToInt(currentDisplayedStamina)}/{maxStamina}{restIndicator}");
            }

            GUI.color = Color.white; // Reset GUI color
        }

        public void SetShowNumbers(bool show)
        {
            showStaminaNumbers = show;
        }

        public void SetAnimateChanges(bool animate)
        {
            animateChanges = animate;
        }

        private void OnDestroy()
        {
            if (targetStaminaSystem != null)
            {
                targetStaminaSystem.OnStaminaChanged -= OnStaminaChanged;
                targetStaminaSystem.OnRestStarted -= OnRestStarted;
                targetStaminaSystem.OnRestStopped -= OnRestStopped;
            }
        }
    }
}