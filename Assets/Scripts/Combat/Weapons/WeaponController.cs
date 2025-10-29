using UnityEngine;

namespace FairyGate.Combat
{
    public class WeaponController : MonoBehaviour
    {
        [Header("Weapon Configuration")]
        [SerializeField] private WeaponData weaponData;
        [SerializeField] private Transform weaponModel;
        [SerializeField] private LayerMask targetLayerMask = -1;

        [Header("Debug")]
        [SerializeField] private bool showRangeGizmos = true;
        [SerializeField] private Color rangeColor = Color.red;

        public WeaponData WeaponData => weaponData;
        public float CurrentRange => (weaponData?.range ?? 1f) * CombatConstants.WEAPON_RANGE_MULTIPLIER;
        public int CurrentDamage => weaponData?.baseDamage ?? 1;
        public float CurrentSpeed => weaponData?.speed ?? 1f;
        public float CurrentStunDuration => weaponData?.stunDuration ?? 1f;

        private void Awake()
        {
            if (weaponData == null)
            {
                Debug.LogWarning($"WeaponController on {gameObject.name} has no WeaponData assigned. Creating default sword.");
                weaponData = WeaponData.CreateSwordData();
            }
        }

        public void SetWeapon(WeaponData newWeapon)
        {
            if (newWeapon == null)
            {
                Debug.LogError("Cannot set null weapon data");
                return;
            }

            weaponData = newWeapon;
            UpdateWeaponModel();
        }

        public bool IsInRange(Transform target)
        {
            if (target == null) return false;

            // Use squared distance to avoid expensive sqrt operation
            float sqrDistance = (transform.position - target.position).sqrMagnitude;
            float sqrRange = CurrentRange * CurrentRange;
            return sqrDistance <= sqrRange;
        }

        public bool CheckRangeForSkill(Transform target, SkillType skillType)
        {
            if (!SpeedResolver.IsOffensiveSkill(skillType))
            {
                return true; // Defensive skills don't require range checks initially
            }

            return IsInRange(target);
        }

        public float GetDistanceTo(Transform target)
        {
            if (target == null) return float.MaxValue;
            return Vector3.Distance(transform.position, target.position);
        }

        public Transform[] GetTargetsInRange()
        {
            Collider[] colliders = Physics.OverlapSphere(transform.position, CurrentRange, targetLayerMask);

            var targets = new System.Collections.Generic.List<Transform>();
            foreach (var collider in colliders)
            {
                if (collider.transform != transform) // Exclude self
                {
                    targets.Add(collider.transform);
                }
            }

            return targets.ToArray();
        }

        public float GetSpeedModifier()
        {
            return weaponData.speedResolutionModifier;
        }

        public float GetExecutionSpeedModifier()
        {
            return weaponData.executionSpeedModifier;
        }

        private void UpdateWeaponModel()
        {
            if (weaponModel != null && weaponData.weaponPrefab != null)
            {
                // Destroy existing weapon model
                if (weaponModel.childCount > 0)
                {
                    for (int i = weaponModel.childCount - 1; i >= 0; i--)
                    {
                        DestroyImmediate(weaponModel.GetChild(i).gameObject);
                    }
                }

                // Instantiate new weapon model
                GameObject newWeapon = Instantiate(weaponData.weaponPrefab, weaponModel);
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (showRangeGizmos && weaponData != null)
            {
                Gizmos.color = rangeColor;
                Gizmos.DrawWireSphere(transform.position, CurrentRange);

                // Draw weapon name
                #if UNITY_EDITOR
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (CurrentRange + 0.5f),
                    $"{weaponData.weaponName}\nRange: {CurrentRange:F1} ({weaponData.range:F1} x {CombatConstants.WEAPON_RANGE_MULTIPLIER:F1})\nDamage: {weaponData.baseDamage}\nSpeed: {weaponData.speed:F1}"
                );
                #endif
            }
        }

        // Helper methods for creating default weapon types
        public void SetToSword()
        {
            SetWeapon(WeaponData.CreateSwordData());
        }

        public void SetToSpear()
        {
            SetWeapon(WeaponData.CreateSpearData());
        }

        public void SetToDagger()
        {
            SetWeapon(WeaponData.CreateDaggerData());
        }

        public void SetToMace()
        {
            SetWeapon(WeaponData.CreateMaceData());
        }
    }
}