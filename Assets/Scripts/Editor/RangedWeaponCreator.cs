using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Editor
{
    public class RangedWeaponCreator
    {
        private const string WeaponPath = "Assets/Data/Weapons/";

        [MenuItem("Tools/Combat/Create Ranged Weapons/Bow")]
        public static void CreateBow()
        {
            CreateWeapon(WeaponData.CreateBowData(), "Bow");
        }

        [MenuItem("Tools/Combat/Create Ranged Weapons/Javelin")]
        public static void CreateJavelin()
        {
            CreateWeapon(WeaponData.CreateJavelinData(), "Javelin");
        }

        [MenuItem("Tools/Combat/Create Ranged Weapons/Throwing Knife")]
        public static void CreateThrowingKnife()
        {
            CreateWeapon(WeaponData.CreateThrowingKnifeData(), "ThrowingKnife");
        }

        [MenuItem("Tools/Combat/Create Ranged Weapons/Sling")]
        public static void CreateSling()
        {
            CreateWeapon(WeaponData.CreateSlingData(), "Sling");
        }

        [MenuItem("Tools/Combat/Create Ranged Weapons/Throwing Axe")]
        public static void CreateThrowingAxe()
        {
            CreateWeapon(WeaponData.CreateThrowingAxeData(), "ThrowingAxe");
        }

        [MenuItem("Tools/Combat/Create Ranged Weapons/All Ranged Weapons")]
        public static void CreateAllRangedWeapons()
        {
            CreateBow();
            CreateJavelin();
            CreateThrowingKnife();
            CreateSling();
            CreateThrowingAxe();

            Debug.Log("Created all 5 ranged weapons in " + WeaponPath);
            AssetDatabase.Refresh();
        }

        private static void CreateWeapon(WeaponData weaponData, string fileName)
        {
            // Ensure directory exists
            if (!AssetDatabase.IsValidFolder("Assets/Data"))
                AssetDatabase.CreateFolder("Assets", "Data");

            if (!AssetDatabase.IsValidFolder("Assets/Data/Weapons"))
                AssetDatabase.CreateFolder("Assets/Data", "Weapons");

            string fullPath = WeaponPath + fileName + ".asset";

            // Check if already exists
            if (AssetDatabase.LoadAssetAtPath<WeaponData>(fullPath) != null)
            {
                Debug.LogWarning($"Weapon {fileName} already exists at {fullPath}. Skipping.");
                return;
            }

            AssetDatabase.CreateAsset(weaponData, fullPath);
            AssetDatabase.SaveAssets();

            Debug.Log($"✅ Created {weaponData.weaponName} at {fullPath}");

            // Ping the asset in Project window
            EditorGUIUtility.PingObject(weaponData);
        }
    }
}
