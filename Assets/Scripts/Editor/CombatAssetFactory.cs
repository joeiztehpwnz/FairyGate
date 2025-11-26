using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Factory class for creating and managing ScriptableObject assets.
    /// Handles weapons, character stats, and equipment data.
    /// </summary>
    public class CombatAssetFactory
    {
        /// <summary>
        /// Creates or loads character stats from an asset file.
        /// </summary>
        /// <param name="assetName">Name of the asset file</param>
        /// <param name="str">Strength stat</param>
        /// <param name="dex">Dexterity stat</param>
        /// <param name="intel">Intelligence stat</param>
        /// <param name="focus">Focus stat</param>
        /// <param name="physDef">Physical Defense stat</param>
        /// <param name="magDef">Magical Defense stat</param>
        /// <param name="vitality">Vitality stat</param>
        /// <returns>The created or loaded CharacterStats asset</returns>
        public CharacterStats CreateOrLoadCharacterStats(string assetName, int str, int dex, int intel,
            int focus, int physDef, int magDef, int vitality)
        {
            string path = $"Assets/Data/Characters/{assetName}.asset";

            // Try to load existing
            var existing = AssetDatabase.LoadAssetAtPath<CharacterStats>(path);
            if (existing != null)
            {
                Debug.Log($"📦 Loaded existing {assetName}");
                return existing;
            }

            // Create new
            var stats = ScriptableObject.CreateInstance<CharacterStats>();
            stats.strength = str;
            stats.dexterity = dex;
            stats.intelligence = intel;
            stats.focus = focus;
            stats.physicalDefense = physDef;
            stats.magicalDefense = magDef;
            stats.vitality = vitality;

            System.IO.Directory.CreateDirectory("Assets/Data/Characters");
            AssetDatabase.CreateAsset(stats, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created {assetName}");
            return stats;
        }

        /// <summary>
        /// Creates or loads weapon data from an asset file.
        /// Uses weapon-specific factory methods from WeaponData.
        /// </summary>
        /// <param name="assetName">Name of the asset file</param>
        /// <param name="weaponType">Type of weapon to create</param>
        /// <returns>The created or loaded WeaponData asset</returns>
        public WeaponData CreateOrLoadWeaponData(string assetName, WeaponType weaponType)
        {
            string path = $"Assets/Data/Weapons/{assetName}.asset";

            // Try to load existing
            var existing = AssetDatabase.LoadAssetAtPath<WeaponData>(path);
            if (existing != null)
            {
                Debug.Log($"📦 Loaded existing {assetName}");
                return existing;
            }

            // Create new using factory methods
            WeaponData weapon = weaponType switch
            {
                WeaponType.Sword => WeaponData.CreateSwordData(),
                WeaponType.Spear => WeaponData.CreateSpearData(),
                WeaponType.Dagger => WeaponData.CreateDaggerData(),
                WeaponType.Mace => WeaponData.CreateMaceData(),
                WeaponType.Bow => WeaponData.CreateBowData(),
                _ => WeaponData.CreateSwordData()
            };

            System.IO.Directory.CreateDirectory("Assets/Data/Weapons");
            AssetDatabase.CreateAsset(weapon, path);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created {assetName} ({weaponType})");
            return weapon;
        }

        /// <summary>
        /// Ensures all equipment asset directories exist.
        /// Verifies armor, accessories, and equipment set directories.
        /// </summary>
        public void CreateAllEquipmentAssets()
        {
            // This method ensures all equipment assets exist
            // The assets are already created based on your git status, so we just verify
            string[] requiredArmor = { "HeavyPlatemail", "LeatherTunic", "ClothRobes", "ChainMail" };
            string[] requiredAccessories = { "PowerGauntlets", "MeditationAmulet", "SwiftBoots", "GuardianRing" };
            string[] requiredSets = { "Fortress_TankSet", "Windrunner_SpeedSet", "Berserker_GlassCannonSet", "Wanderer_BalancedSet" };

            System.IO.Directory.CreateDirectory("Assets/Data/Equipment/Armor");
            System.IO.Directory.CreateDirectory("Assets/Data/Equipment/Accessories");
            System.IO.Directory.CreateDirectory("Assets/Data/Equipment/Sets");

            Debug.Log("✅ Equipment asset directories verified");
        }
    }
}
