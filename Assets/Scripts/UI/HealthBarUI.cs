using UnityEngine;

namespace FairyGate.Combat.UI
{
    public class HealthBarUI : MonoBehaviour
    {
        [Header("Display Settings")]
        [SerializeField] private Vector2 barPosition = new Vector2(10, 10);
        [SerializeField] private Vector2 barSize = new Vector2(200, 20);
        [SerializeField] private Color fullHealthColor = Color.green;
        [SerializeField] private Color lowHealthColor = Color.red;

        [Header("Configuration")]
        [SerializeField] private bool showHealthNumbers = true;
        [SerializeField] private bool animateChanges = true;
        [SerializeField] private float animationSpeed = 5f;

        private HealthSystem targetHealthSystem;
        private float currentDisplayedHealth;
        private float targetHealth;
        private int maxHealth;

        private void Awake()
        {
            // No UI components needed - using OnGUI for display
        }

        private void Update()
        {
            if (targetHealthSystem != null && animateChanges)
            {
                AnimateHealthBar();
            }
        }

        public void SetTargetHealthSystem(HealthSystem healthSystem)
        {
            // Unsubscribe from previous target
            if (targetHealthSystem != null)
            {
                targetHealthSystem.OnHealthChanged -= OnHealthChanged;
            }

            // Subscribe to new target
            targetHealthSystem = healthSystem;
            if (targetHealthSystem != null)
            {
                targetHealthSystem.OnHealthChanged += OnHealthChanged;

                // Initialize display
                maxHealth = targetHealthSystem.MaxHealth;
                targetHealth = targetHealthSystem.CurrentHealth;
                currentDisplayedHealth = targetHealth;
                UpdateHealthDisplay();
            }
        }

        private void OnHealthChanged(int currentHealth, int maxHealthValue)
        {
            maxHealth = maxHealthValue;
            targetHealth = currentHealth;

            if (!animateChanges)
            {
                currentDisplayedHealth = targetHealth;
                UpdateHealthDisplay();
            }
        }

        private void AnimateHealthBar()
        {
            if (Mathf.Abs(currentDisplayedHealth - targetHealth) > 0.1f)
            {
                currentDisplayedHealth = Mathf.Lerp(currentDisplayedHealth, targetHealth, animationSpeed * Time.deltaTime);
                UpdateHealthDisplay();
            }
            else
            {
                currentDisplayedHealth = targetHealth;
                UpdateHealthDisplay();
            }
        }

        private void UpdateHealthDisplay()
        {
            // Display handled in OnGUI
        }

        private void OnGUI()
        {
            if (targetHealthSystem == null) return;

            float healthPercentage = currentDisplayedHealth / maxHealth;

            // Draw background
            GUI.color = Color.black;
            GUI.DrawTexture(new Rect(barPosition.x - 2, barPosition.y - 2, barSize.x + 4, barSize.y + 4), Texture2D.whiteTexture);

            // Draw health bar
            GUI.color = Color.Lerp(lowHealthColor, fullHealthColor, healthPercentage);
            GUI.DrawTexture(new Rect(barPosition.x, barPosition.y, barSize.x * healthPercentage, barSize.y), Texture2D.whiteTexture);

            // Draw health text
            if (showHealthNumbers)
            {
                GUI.color = Color.white;
                GUI.Label(new Rect(barPosition.x, barPosition.y + barSize.y + 5, barSize.x, 20),
                    $"Health: {Mathf.RoundToInt(currentDisplayedHealth)}/{maxHealth}");
            }

            GUI.color = Color.white; // Reset GUI color
        }

        public void SetShowNumbers(bool show)
        {
            showHealthNumbers = show;
        }

        public void SetAnimateChanges(bool animate)
        {
            animateChanges = animate;
        }

        private void OnDestroy()
        {
            if (targetHealthSystem != null)
            {
                targetHealthSystem.OnHealthChanged -= OnHealthChanged;
            }
        }
    }
}