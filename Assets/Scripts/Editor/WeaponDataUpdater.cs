using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Editor utility to update all WeaponData assets with correct N+1 system values.
    /// Fixes assets that were saved before comboLength and knockdownRate fields were added.
    /// </summary>
    public class WeaponDataUpdater : EditorWindow
    {
        [MenuItem("Combat/Update Weapon Assets/Fix N+1 Values")]
        public static void UpdateAllWeaponAssets()
        {
            if (!EditorUtility.DisplayDialog("Update Weapon Assets",
                "This will update all WeaponData assets with correct N+1 combo system values:\n\n" +
                "• comboLength (varies by weapon type)\n" +
                "• knockdownRate (varies by weapon type)\n" +
                "• stunDuration (correct values)\n\n" +
                "Continue?",
                "Yes, Update Assets",
                "Cancel"))
            {
                return;
            }

            // Find all WeaponData assets in the project
            string[] guids = AssetDatabase.FindAssets("t:WeaponData");
            int updatedCount = 0;

            foreach (string guid in guids)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guid);
                WeaponData weapon = AssetDatabase.LoadAssetAtPath<WeaponData>(assetPath);

                if (weapon != null)
                {
                    bool wasUpdated = UpdateWeaponValues(weapon);
                    if (wasUpdated)
                    {
                        EditorUtility.SetDirty(weapon);
                        updatedCount++;
                        Debug.Log($"[WeaponDataUpdater] Updated: {weapon.name}");
                    }
                }
            }

            if (updatedCount > 0)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                Debug.Log($"[WeaponDataUpdater] Successfully updated {updatedCount} weapon asset(s)");
                EditorUtility.DisplayDialog("Update Complete",
                    $"Successfully updated {updatedCount} weapon asset(s) with correct N+1 values.",
                    "OK");
            }
            else
            {
                Debug.Log("[WeaponDataUpdater] No weapons needed updating");
                EditorUtility.DisplayDialog("No Updates Needed",
                    "All weapon assets already have correct values.",
                    "OK");
            }
        }

        /// <summary>
        /// Updates a weapon's N+1 system values based on its name/type.
        /// Returns true if any values were changed.
        /// </summary>
        private static bool UpdateWeaponValues(WeaponData weapon)
        {
            bool wasUpdated = false;
            string weaponName = weapon.name.ToLower();

            // Determine weapon type from name and apply correct values
            if (weaponName.Contains("mace"))
            {
                // Mace: Slow, heavy, single powerful hit
                if (weapon.comboLength != 1)
                {
                    weapon.comboLength = 1;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.knockdownRate, 1.3f))
                {
                    weapon.knockdownRate = 1.3f;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.stunDuration, 1.7f))
                {
                    weapon.stunDuration = 1.7f;
                    wasUpdated = true;
                }
            }
            else if (weaponName.Contains("bow"))
            {
                // Bow: Ranged, single shot
                if (weapon.comboLength != 1)
                {
                    weapon.comboLength = 1;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.knockdownRate, 0.7f))
                {
                    weapon.knockdownRate = 0.7f;
                    wasUpdated = true;
                }
                // CRITICAL FIX: Bow should have 0.3s stun, not 2.0s
                if (!Mathf.Approximately(weapon.stunDuration, 0.3f))
                {
                    weapon.stunDuration = 0.3f;
                    wasUpdated = true;
                }
            }
            else if (weaponName.Contains("dagger") || weaponName.Contains("knife"))
            {
                // Dagger: Fast, weak, long combo chain
                if (weapon.comboLength != 5)
                {
                    weapon.comboLength = 5;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.knockdownRate, 0.8f))
                {
                    weapon.knockdownRate = 0.8f;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.stunDuration, 0.4f))
                {
                    weapon.stunDuration = 0.4f;
                    wasUpdated = true;
                }
                weapon.isFastWeapon = true;
            }
            else if (weaponName.Contains("spear") || weaponName.Contains("lance"))
            {
                // Spear: Balanced medium weapon
                if (weapon.comboLength != 3)
                {
                    weapon.comboLength = 3;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.knockdownRate, 1.1f))
                {
                    weapon.knockdownRate = 1.1f;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.stunDuration, 0.8f))
                {
                    weapon.stunDuration = 0.8f;
                    wasUpdated = true;
                }
            }
            else if (weaponName.Contains("sword"))
            {
                // Sword: Baseline standard weapon
                if (weapon.comboLength != 3)
                {
                    weapon.comboLength = 3;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.knockdownRate, 1.0f))
                {
                    weapon.knockdownRate = 1.0f;
                    wasUpdated = true;
                }
                if (!Mathf.Approximately(weapon.stunDuration, 1.0f))
                {
                    weapon.stunDuration = 1.0f;
                    wasUpdated = true;
                }
            }

            return wasUpdated;
        }
    }
}
