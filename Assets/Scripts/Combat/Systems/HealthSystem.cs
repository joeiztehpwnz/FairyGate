using UnityEngine;
using UnityEngine.Events;

namespace FairyGate.Combat
{
    public class HealthSystem : MonoBehaviour, IDamageable
    {
        [Header("Health Configuration")]
        [SerializeField] private CharacterStats characterStats;
        [SerializeField] private int currentHealth;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;
        [SerializeField] private bool showHealthGUI = true;

        [Header("Events")]
        public UnityEvent<int, Transform> OnDamageReceived = new UnityEvent<int, Transform>();
        public UnityEvent<Transform> OnDied = new UnityEvent<Transform>();
        public UnityEvent<int, int> OnHealthChanged = new UnityEvent<int, int>(); // current, max

        private StaminaSystem staminaSystem;
        private StatusEffectManager statusEffectManager;
        private CombatController combatController;
        private EquipmentManager equipmentManager;

        public int CurrentHealth => currentHealth;
        public int MaxHealth
        {
            get
            {
                int baseHealth = characterStats?.MaxHealth ?? CombatConstants.BASE_HEALTH;
                int bonus = 0;

                if (equipmentManager != null)
                {
                    if (equipmentManager.CurrentArmor != null)
                        bonus += equipmentManager.CurrentArmor.maxHealthBonus;
                    if (equipmentManager.CurrentAccessory != null)
                        bonus += equipmentManager.CurrentAccessory.maxHealthBonus;
                }

                return baseHealth + bonus;
            }
        }
        public bool IsAlive => currentHealth > 0;
        public float HealthPercentage => (float)currentHealth / MaxHealth;

        private void Awake()
        {
            staminaSystem = GetComponent<StaminaSystem>();
            statusEffectManager = GetComponent<StatusEffectManager>();
            combatController = GetComponent<CombatController>();
            equipmentManager = GetComponent<EquipmentManager>();

            if (characterStats == null)
            {
                Debug.LogWarning($"HealthSystem on {gameObject.name} has no CharacterStats assigned. Using default values.");
                characterStats = CharacterStats.CreateDefaultStats();
            }

            currentHealth = MaxHealth;
        }

        private void Start()
        {
            OnHealthChanged.Invoke(currentHealth, MaxHealth);
        }

        public void TakeDamage(int damage, Transform source)
        {
            if (!IsAlive) return;

            // Ensure minimum damage
            damage = Mathf.Max(damage, CombatConstants.MINIMUM_DAMAGE);

            int oldHealth = currentHealth;
            currentHealth = Mathf.Max(0, currentHealth - damage);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} took {damage} damage from {(source ? source.name : "unknown")} ({currentHealth}/{MaxHealth})");
            }

            OnDamageReceived.Invoke(damage, source);
            OnHealthChanged.Invoke(currentHealth, MaxHealth);

            // Interrupt rest if taking damage
            if (staminaSystem != null)
            {
                staminaSystem.InterruptRest();
            }

            // Check for death
            if (currentHealth <= 0 && oldHealth > 0)
            {
                Die();
            }
        }

        public void Die()
        {
            if (!IsAlive) return;

            currentHealth = 0;

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} has died!");
            }

            // Clear all status effects
            if (statusEffectManager != null)
            {
                statusEffectManager.ClearAllStatusEffects();
            }

            // Exit combat
            if (combatController != null && combatController.IsInCombat)
            {
                combatController.ExitCombat();
            }

            OnDied.Invoke(transform);
            OnHealthChanged.Invoke(currentHealth, MaxHealth);

            // Trigger game state changes (reset prompt, etc.)
            GameManager.Instance?.OnCharacterDied(this);
        }

        public void Heal(int healAmount)
        {
            if (!IsAlive || healAmount <= 0) return;

            int oldHealth = currentHealth;
            currentHealth = Mathf.Min(MaxHealth, currentHealth + healAmount);

            if (currentHealth != oldHealth)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"{gameObject.name} healed for {currentHealth - oldHealth} ({currentHealth}/{MaxHealth})");
                }

                OnHealthChanged.Invoke(currentHealth, MaxHealth);
            }
        }

        public void SetHealth(int health)
        {
            currentHealth = Mathf.Clamp(health, 0, MaxHealth);
            OnHealthChanged.Invoke(currentHealth, MaxHealth);

            if (currentHealth <= 0)
            {
                Die();
            }
        }

        public void RestoreToFull()
        {
            SetHealth(MaxHealth);
        }

        public void ProcessCounterReflection(int reflectedDamage, Transform originalAttacker)
        {
            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} taking {reflectedDamage} reflected damage");
            }

            TakeDamage(reflectedDamage, originalAttacker);
        }

        public int CalculateDamageReduction(int incomingDamage, float reductionPercent)
        {
            return DamageCalculator.ApplyDamageReduction(incomingDamage, reductionPercent, characterStats);
        }

        public void ResetForTesting()
        {
            currentHealth = MaxHealth;
            OnHealthChanged.Invoke(currentHealth, MaxHealth);

            if (enableDebugLogs)
            {
                Debug.Log($"{gameObject.name} health reset for testing");
            }
        }

        // GUI Debug Display
        private void OnGUI()
        {
            if (showHealthGUI && Application.isPlaying)
            {
                Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2f);
                screenPos.y = Screen.height - screenPos.y;

                // Health bar background
                GUI.color = Color.black;
                GUI.DrawTexture(new Rect(screenPos.x - 52, screenPos.y - 12, 104, 14), Texture2D.whiteTexture);

                // Health bar fill
                float healthPercent = HealthPercentage;
                GUI.color = Color.Lerp(Color.red, Color.green, healthPercent);
                GUI.DrawTexture(new Rect(screenPos.x - 50, screenPos.y - 10, 100 * healthPercent, 10), Texture2D.whiteTexture);

                // Health text
                GUI.color = Color.white;
                GUI.Label(new Rect(screenPos.x - 30, screenPos.y + 5, 60, 20), $"{currentHealth}/{MaxHealth}");

                GUI.color = Color.white; // Reset GUI color
            }
        }

        private void OnValidate()
        {
            if (characterStats != null)
            {
                currentHealth = Mathf.Clamp(currentHealth, 0, MaxHealth);
            }
        }
    }
}