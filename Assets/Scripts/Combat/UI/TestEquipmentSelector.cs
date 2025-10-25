using UnityEngine;

namespace FairyGate.Combat
{
    public class TestEquipmentSelector : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private EquipmentManager targetEquipmentManager;
        [SerializeField] private bool autoFindTargetEnemy = true;

        [Header("Equipment Presets")]
        [SerializeField] private EquipmentSet[] equipmentPresets;
        private int currentPresetIndex = 0;

        [Header("Hotkeys")]
        [SerializeField] private KeyCode nextPresetKey = KeyCode.RightBracket;
        [SerializeField] private KeyCode previousPresetKey = KeyCode.LeftBracket;
        [SerializeField] private KeyCode removeAllKey = KeyCode.Backslash;

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = true;

        [Header("UI Display")]
        [SerializeField] private bool showUIDisplay = true;
        [SerializeField] private Vector2 displayPosition = new Vector2(10, 150);
        [SerializeField] private int fontSize = 12;
        [SerializeField] private bool showDetailedStats = false;
        [SerializeField] private bool showEquipmentPieces = false;
        [SerializeField] private Color textColor = Color.white;

        private void Start()
        {
            if (autoFindTargetEnemy)
            {
                FindTargetEquipmentManager();
            }
        }

        private void Update()
        {
            // Test with additional keys for debugging
            if (Input.GetKeyDown(nextPresetKey) || Input.GetKeyDown(KeyCode.RightBracket) || Input.GetKeyDown(KeyCode.PageDown))
            {
                Debug.Log("[TestEquipment] Next preset key detected!");
                CyclePresetForward();
            }
            else if (Input.GetKeyDown(previousPresetKey) || Input.GetKeyDown(KeyCode.LeftBracket) || Input.GetKeyDown(KeyCode.PageUp))
            {
                Debug.Log("[TestEquipment] Previous preset key detected!");
                CyclePresetBackward();
            }
            else if (Input.GetKeyDown(removeAllKey) || Input.GetKeyDown(KeyCode.Backslash) || Input.GetKeyDown(KeyCode.Home))
            {
                Debug.Log("[TestEquipment] Remove all key detected!");
                RemoveAllEquipment();
            }
        }

        private void CyclePresetForward()
        {
            if (equipmentPresets.Length == 0)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[TestEquipment] No equipment presets configured");
                }
                return;
            }

            currentPresetIndex = (currentPresetIndex + 1) % equipmentPresets.Length;
            ApplyCurrentPreset();
        }

        private void CyclePresetBackward()
        {
            if (equipmentPresets.Length == 0)
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[TestEquipment] No equipment presets configured");
                }
                return;
            }

            currentPresetIndex--;
            if (currentPresetIndex < 0) currentPresetIndex = equipmentPresets.Length - 1;
            ApplyCurrentPreset();
        }

        private void ApplyCurrentPreset()
        {
            if (targetEquipmentManager != null && equipmentPresets.Length > 0)
            {
                var preset = equipmentPresets[currentPresetIndex];
                targetEquipmentManager.EquipSet(preset);

                if (enableDebugLogs)
                {
                    Debug.Log($"[TestEquipment] Applied preset {currentPresetIndex + 1}/{equipmentPresets.Length}: {preset.setName}");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[TestEquipment] No target EquipmentManager found");
                }
            }
        }

        private void RemoveAllEquipment()
        {
            if (targetEquipmentManager != null)
            {
                targetEquipmentManager.UnequipItem(EquipmentSlot.Armor);
                targetEquipmentManager.UnequipItem(EquipmentSlot.Accessory);

                if (enableDebugLogs)
                {
                    Debug.Log("[TestEquipment] Removed all equipment");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning("[TestEquipment] No target EquipmentManager found");
                }
            }
        }

        private void FindTargetEquipmentManager()
        {
            // Find enemy with EquipmentManager
            var enemies = FindObjectsByType<CombatController>(FindObjectsSortMode.None);
            foreach (var enemy in enemies)
            {
                if (enemy.name.Contains("Enemy") || enemy.name.Contains("Knight"))
                {
                    targetEquipmentManager = enemy.GetComponent<EquipmentManager>();
                    if (targetEquipmentManager != null)
                    {
                        if (enableDebugLogs)
                        {
                            Debug.Log($"[TestEquipment] Found target: {enemy.name}");
                        }
                        break;
                    }
                }
            }

            if (targetEquipmentManager == null && enableDebugLogs)
            {
                Debug.LogWarning("[TestEquipment] No enemy with EquipmentManager found");
            }
        }

        // Display current preset info
        private void OnGUI()
        {
            if (!showUIDisplay || targetEquipmentManager == null || equipmentPresets.Length == 0 || !Application.isPlaying)
                return;

            // Setup GUI style
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.fontSize = fontSize;
            labelStyle.normal.textColor = textColor;

            float yOffset = displayPosition.y;
            float lineHeight = fontSize + 4;

            // Main preset info
            string presetInfo = $"Equipment Preset: {equipmentPresets[currentPresetIndex].setName} ({currentPresetIndex + 1}/{equipmentPresets.Length})";
            GUI.Label(new Rect(displayPosition.x, yOffset, 400, lineHeight), presetInfo, labelStyle);
            yOffset += lineHeight;

            // Show equipment pieces if enabled
            if (showEquipmentPieces)
            {
                var preset = equipmentPresets[currentPresetIndex];
                if (preset.armor != null)
                {
                    GUI.Label(new Rect(displayPosition.x + 20, yOffset, 400, lineHeight),
                        $"Armor: {preset.armor.equipmentName}", labelStyle);
                    yOffset += lineHeight;
                }
                if (preset.accessory != null)
                {
                    GUI.Label(new Rect(displayPosition.x + 20, yOffset, 400, lineHeight),
                        $"Accessory: {preset.accessory.equipmentName}", labelStyle);
                    yOffset += lineHeight;
                }
            }

            // Show detailed stats if enabled
            if (showDetailedStats && targetEquipmentManager.ModifiedStats != null)
            {
                var stats = targetEquipmentManager.ModifiedStats;
                string statsText = $"Str: {stats.strength} | Dex: {stats.dexterity} | Def: {stats.physicalDefense} | Focus: {stats.focus}";
                GUI.Label(new Rect(displayPosition.x + 20, yOffset, 500, lineHeight), statsText, labelStyle);
                yOffset += lineHeight;
            }

            // Hotkey instructions
            GUI.Label(new Rect(displayPosition.x, yOffset, 400, lineHeight),
                "PgDn: Next | PgUp: Previous | Home: Remove All", labelStyle);
        }
    }
}
