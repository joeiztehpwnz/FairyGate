using UnityEngine;
using UnityEditor;

namespace FairyGate.Combat
{
    public class EquipmentCreator : EditorWindow
    {
        [MenuItem("Tools/Combat/Equipment/Create All Sample Equipment")]
        public static void CreateAllSampleEquipment()
        {
            if (EditorUtility.DisplayDialog("Create Sample Equipment",
                "This will create:\n" +
                "- 4 Armor pieces\n" +
                "- 4 Accessories\n" +
                "- 4 Equipment Sets\n\n" +
                "in Assets/Data/Equipment/\n\n" +
                "Continue?",
                "Yes", "Cancel"))
            {
                CreateDirectories();
                CreateArmor();
                CreateAccessories();
                CreateSets();

                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();

                EditorUtility.DisplayDialog("Complete",
                    "Sample equipment created successfully!\n\n" +
                    "Check Assets/Data/Equipment/ for:\n" +
                    "- Armor/ (4 pieces)\n" +
                    "- Accessories/ (4 pieces)\n" +
                    "- Sets/ (4 sets)",
                    "OK");
            }
        }

        private static void CreateDirectories()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            if (!AssetDatabase.IsValidFolder("Assets/Data/Equipment"))
                AssetDatabase.CreateFolder("Assets/Data", "Equipment");

            if (!AssetDatabase.IsValidFolder("Assets/Data/Equipment/Armor"))
                AssetDatabase.CreateFolder("Assets/Data/Equipment", "Armor");

            if (!AssetDatabase.IsValidFolder("Assets/Data/Equipment/Accessories"))
                AssetDatabase.CreateFolder("Assets/Data/Equipment", "Accessories");

            if (!AssetDatabase.IsValidFolder("Assets/Data/Equipment/Sets"))
                AssetDatabase.CreateFolder("Assets/Data/Equipment", "Sets");
        }

        private static void CreateArmor()
        {
            // 1. Heavy Platemail (Tank)
            var heavyPlate = ScriptableObject.CreateInstance<EquipmentData>();
            heavyPlate.equipmentName = "Heavy Platemail";
            heavyPlate.slot = EquipmentSlot.Armor;
            heavyPlate.tier = EquipmentTier.Advanced;
            heavyPlate.physicalDefenseBonus = 15;
            heavyPlate.maxHealthBonus = 30;
            heavyPlate.movementSpeedBonus = -1.0f;
            heavyPlate.focusBonus = 3;
            heavyPlate.description = "Heavy iron armor that provides excellent protection at the cost of mobility.";
            AssetDatabase.CreateAsset(heavyPlate, "Assets/Data/Equipment/Armor/HeavyPlatemail.asset");

            // 2. Leather Tunic (Speed)
            var leatherTunic = ScriptableObject.CreateInstance<EquipmentData>();
            leatherTunic.equipmentName = "Leather Tunic";
            leatherTunic.slot = EquipmentSlot.Armor;
            leatherTunic.tier = EquipmentTier.Basic;
            leatherTunic.dexterityBonus = 5;
            leatherTunic.movementSpeedBonus = 2.0f;
            leatherTunic.physicalDefenseBonus = 3;
            leatherTunic.description = "Light leather armor that allows for quick movement and dodging.";
            AssetDatabase.CreateAsset(leatherTunic, "Assets/Data/Equipment/Armor/LeatherTunic.asset");

            // 3. Cloth Robes (Focus)
            var clothRobes = ScriptableObject.CreateInstance<EquipmentData>();
            clothRobes.equipmentName = "Cloth Robes";
            clothRobes.slot = EquipmentSlot.Armor;
            clothRobes.tier = EquipmentTier.Basic;
            clothRobes.focusBonus = 8;
            clothRobes.maxStaminaBonus = 20;
            clothRobes.physicalDefenseBonus = 1;
            clothRobes.description = "Light magical robes that enhance mental focus and stamina recovery.";
            AssetDatabase.CreateAsset(clothRobes, "Assets/Data/Equipment/Armor/ClothRobes.asset");

            // 4. Chain Mail (Balanced)
            var chainMail = ScriptableObject.CreateInstance<EquipmentData>();
            chainMail.equipmentName = "Chain Mail";
            chainMail.slot = EquipmentSlot.Armor;
            chainMail.tier = EquipmentTier.Advanced;
            chainMail.physicalDefenseBonus = 10;
            chainMail.maxHealthBonus = 15;
            chainMail.movementSpeedBonus = -0.5f;
            chainMail.dexterityBonus = 2;
            chainMail.description = "A balanced armor providing decent protection without sacrificing too much mobility.";
            AssetDatabase.CreateAsset(chainMail, "Assets/Data/Equipment/Armor/ChainMail.asset");

            Debug.Log("Created 4 armor pieces");
        }

        private static void CreateAccessories()
        {
            // 1. Power Gauntlets (Damage)
            var powerGauntlets = ScriptableObject.CreateInstance<EquipmentData>();
            powerGauntlets.equipmentName = "Power Gauntlets";
            powerGauntlets.slot = EquipmentSlot.Accessory;
            powerGauntlets.tier = EquipmentTier.Advanced;
            powerGauntlets.strengthBonus = 8;
            powerGauntlets.maxStaminaBonus = -10;
            powerGauntlets.description = "Enchanted gauntlets that enhance striking power but drain stamina faster.";
            AssetDatabase.CreateAsset(powerGauntlets, "Assets/Data/Equipment/Accessories/PowerGauntlets.asset");

            // 2. Meditation Amulet (Focus)
            var meditationAmulet = ScriptableObject.CreateInstance<EquipmentData>();
            meditationAmulet.equipmentName = "Meditation Amulet";
            meditationAmulet.slot = EquipmentSlot.Accessory;
            meditationAmulet.tier = EquipmentTier.Advanced;
            meditationAmulet.focusBonus = 10;
            meditationAmulet.maxStaminaBonus = 15;
            meditationAmulet.description = "An amulet that enhances mental fortitude and stamina recovery.";
            AssetDatabase.CreateAsset(meditationAmulet, "Assets/Data/Equipment/Accessories/MeditationAmulet.asset");

            // 3. Swift Boots (Speed)
            var swiftBoots = ScriptableObject.CreateInstance<EquipmentData>();
            swiftBoots.equipmentName = "Swift Boots";
            swiftBoots.slot = EquipmentSlot.Accessory;
            swiftBoots.tier = EquipmentTier.Basic;
            swiftBoots.dexterityBonus = 5;
            swiftBoots.movementSpeedBonus = 1.5f;
            swiftBoots.description = "Light boots that greatly enhance movement speed and agility.";
            AssetDatabase.CreateAsset(swiftBoots, "Assets/Data/Equipment/Accessories/SwiftBoots.asset");

            // 4. Guardian Ring (Tank)
            var guardianRing = ScriptableObject.CreateInstance<EquipmentData>();
            guardianRing.equipmentName = "Guardian Ring";
            guardianRing.slot = EquipmentSlot.Accessory;
            guardianRing.tier = EquipmentTier.Advanced;
            guardianRing.physicalDefenseBonus = 5;
            guardianRing.maxHealthBonus = 20;
            guardianRing.focusBonus = 3;
            guardianRing.description = "A protective ring blessed with defensive magic.";
            AssetDatabase.CreateAsset(guardianRing, "Assets/Data/Equipment/Accessories/GuardianRing.asset");

            Debug.Log("Created 4 accessories");
        }

        private static void CreateSets()
        {
            // Load equipment pieces
            var heavyPlate = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Armor/HeavyPlatemail.asset");
            var leatherTunic = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Armor/LeatherTunic.asset");
            var clothRobes = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Armor/ClothRobes.asset");
            var chainMail = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Armor/ChainMail.asset");

            var powerGauntlets = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Accessories/PowerGauntlets.asset");
            var meditationAmulet = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Accessories/MeditationAmulet.asset");
            var swiftBoots = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Accessories/SwiftBoots.asset");
            var guardianRing = AssetDatabase.LoadAssetAtPath<EquipmentData>("Assets/Data/Equipment/Accessories/GuardianRing.asset");

            // 1. Tank Set (Fortress)
            var tankSet = ScriptableObject.CreateInstance<EquipmentSet>();
            tankSet.setName = "Fortress";
            tankSet.armor = heavyPlate;
            tankSet.accessory = guardianRing;
            tankSet.description = "Maximum survivability build focused on absorbing damage and outlasting opponents.";
            AssetDatabase.CreateAsset(tankSet, "Assets/Data/Equipment/Sets/Fortress_TankSet.asset");

            // 2. Speed Set (Windrunner)
            var speedSet = ScriptableObject.CreateInstance<EquipmentSet>();
            speedSet.setName = "Windrunner";
            speedSet.armor = leatherTunic;
            speedSet.accessory = swiftBoots;
            speedSet.description = "High mobility build for hit-and-run tactics and kiting.";
            AssetDatabase.CreateAsset(speedSet, "Assets/Data/Equipment/Sets/Windrunner_SpeedSet.asset");

            // 3. Glass Cannon Set (Berserker)
            var glassCannonSet = ScriptableObject.CreateInstance<EquipmentSet>();
            glassCannonSet.setName = "Berserker";
            glassCannonSet.armor = clothRobes;
            glassCannonSet.accessory = powerGauntlets;
            glassCannonSet.description = "High damage output at the cost of stamina management and defense.";
            AssetDatabase.CreateAsset(glassCannonSet, "Assets/Data/Equipment/Sets/Berserker_GlassCannonSet.asset");

            // 4. Balanced Set (Wanderer)
            var balancedSet = ScriptableObject.CreateInstance<EquipmentSet>();
            balancedSet.setName = "Wanderer";
            balancedSet.armor = chainMail;
            balancedSet.accessory = meditationAmulet;
            balancedSet.description = "Well-rounded build suitable for most combat scenarios.";
            AssetDatabase.CreateAsset(balancedSet, "Assets/Data/Equipment/Sets/Wanderer_BalancedSet.asset");

            Debug.Log("Created 4 equipment sets");
        }
    }
}
