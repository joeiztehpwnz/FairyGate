using System;
using UnityEngine;

namespace FairyGate.Combat
{
    public class WeaponController : MonoBehaviour
    {
        // Events
        public event Action<Transform> OnHitDealt; // Invoked when this weapon successfully deals damage to a target

        [Header("Weapon Configuration")]
        [SerializeField] private WeaponData primaryWeapon;
        [SerializeField] private WeaponData secondaryWeapon;
        [SerializeField] private bool isPrimaryActive = true;
        [SerializeField] private Transform weaponModel;
        [SerializeField] private LayerMask targetLayerMask = -1;

        // Active weapon accessor
        private WeaponData ActiveWeapon => isPrimaryActive ? primaryWeapon : secondaryWeapon;

        [Header("Trail VFX")]
        [SerializeField] private WeaponTrailController trailController;
        [SerializeField] private Transform trailAttachmentPoint; // Optional: where trail should originate (weapon tip)
        [SerializeField] private float defaultTrailHeight = 1.5f; // Height offset when no weapon model exists

        [Header("Debug")]
        [SerializeField] private bool showRangeGizmos = true;
        [SerializeField] private Color rangeColor = Color.red;

        public WeaponData WeaponData => ActiveWeapon;
        public WeaponData PrimaryWeapon => primaryWeapon;
        public WeaponData SecondaryWeapon => secondaryWeapon;
        public bool IsPrimaryActive => isPrimaryActive;

        // Dual-range system
        // Classic Mabinogi: All melee weapons attack from the same distance
        // Weapon differentiation comes from damage, speed, stun duration, and combo potential - NOT range
        public float GetMeleeRange()
        {
            // Use uniform melee range for all weapons (authentic to classic Mabinogi)
            return CombatConstants.STANDARD_MELEE_RANGE;
        }

        public float GetRangedRange()
        {
            if (ActiveWeapon == null) return 0f;
            return ActiveWeapon.rangedRange;
        }

        public float GetSkillRange(SkillType skillType)
        {
            if (skillType == SkillType.RangedAttack)
                return GetRangedRange();
            else
                return GetMeleeRange();
        }

        // Dual-damage system
        public int GetSkillDamage(SkillType skillType)
        {
            if (ActiveWeapon == null) return 1;

            int baseDamage = ActiveWeapon.baseDamage;
            float multiplier = skillType == SkillType.RangedAttack
                ? ActiveWeapon.rangedDamageMultiplier
                : ActiveWeapon.meleeDamageMultiplier;

            return Mathf.RoundToInt(baseDamage * multiplier);
        }

        // Legacy properties - default to melee for backwards compatibility
        public float CurrentRange => GetMeleeRange();
        public int CurrentDamage => GetSkillDamage(SkillType.Attack);
        public float CurrentSpeed => ActiveWeapon?.speed ?? 1f;
        public float CurrentStunDuration => ActiveWeapon?.stunDuration ?? 1f;

        /// <summary>
        /// Registers that this weapon dealt a hit to the specified target.
        /// Invokes OnHitDealt event for pattern executor and other systems to track.
        /// </summary>
        public void RegisterHitDealt(Transform target)
        {
            OnHitDealt?.Invoke(target);
        }

        private void Awake()
        {
            // Backward compatibility: if no weapons assigned, create defaults
            if (primaryWeapon == null)
            {
                Debug.LogWarning($"WeaponController on {gameObject.name} has no primary weapon assigned. Creating default sword.");
                primaryWeapon = WeaponData.CreateSwordData();
            }

            if (secondaryWeapon == null)
            {
                Debug.Log($"WeaponController on {gameObject.name} has no secondary weapon assigned. Using primary as secondary.");
                secondaryWeapon = primaryWeapon; // Default to same weapon in both slots
            }

            InitializeTrailController();
        }

        public void SetWeapon(WeaponData newWeapon, bool setPrimary = true)
        {
            if (newWeapon == null)
            {
                Debug.LogError("Cannot set null weapon data");
                return;
            }

            // Update the appropriate slot
            if (setPrimary)
            {
                primaryWeapon = newWeapon;
                Debug.Log($"Set primary weapon to {newWeapon.weaponName}");
            }
            else
            {
                secondaryWeapon = newWeapon;
                Debug.Log($"Set secondary weapon to {newWeapon.weaponName}");
            }

            // Only update visuals/trails if setting the currently active weapon
            if (isPrimaryActive == setPrimary)
            {
                UpdateWeaponModel();
                InitializeTrailController();
            }
        }

        public bool SwapWeapons()
        {
            // Validate both weapons exist
            if (primaryWeapon == null || secondaryWeapon == null)
            {
                Debug.LogWarning($"Cannot swap weapons - missing weapon in slot (primary: {primaryWeapon != null}, secondary: {secondaryWeapon != null})");
                return false;
            }

            // Toggle active weapon
            isPrimaryActive = !isPrimaryActive;

            // Update visuals for new active weapon
            UpdateWeaponModel();
            InitializeTrailController();

            string newWeaponName = ActiveWeapon.weaponName;
            Debug.Log($"Swapped to {(isPrimaryActive ? "primary" : "secondary")} weapon: {newWeaponName}");

            return true;
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

            // Use skill-specific range (melee or ranged)
            float skillRange = GetSkillRange(skillType);
            float sqrDistance = (transform.position - target.position).sqrMagnitude;
            return sqrDistance <= (skillRange * skillRange);
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
            return ActiveWeapon.speedResolutionModifier;
        }

        public float GetExecutionSpeedModifier()
        {
            return ActiveWeapon.executionSpeedModifier;
        }

        private void UpdateWeaponModel()
        {
            if (weaponModel != null && ActiveWeapon.weaponPrefab != null)
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
                GameObject newWeapon = Instantiate(ActiveWeapon.weaponPrefab, weaponModel);
                newWeapon.transform.localPosition = Vector3.zero;
                newWeapon.transform.localRotation = Quaternion.identity;
            }
        }

        // Melee Trail VFX Methods
        private void InitializeTrailController()
        {
            // Auto-create trail if it doesn't exist
            if (trailController == null)
            {
                CreateTrailController();
            }

            // Initialize trail with weapon data
            if (trailController != null && ActiveWeapon != null)
            {
                trailController.Initialize(ActiveWeapon);
            }
        }

        private void CreateTrailController()
        {
            // Determine where to attach the trail
            Transform attachPoint = trailAttachmentPoint != null ? trailAttachmentPoint : weaponModel;

            bool useHeightOffset = false;
            if (attachPoint == null)
            {
                // Use this gameObject as fallback with height offset
                attachPoint = transform;
                useHeightOffset = true;
            }

            // Check if a trail GameObject already exists
            Transform existingTrail = attachPoint.Find("WeaponTrail");
            GameObject trailObject;

            if (existingTrail != null)
            {
                trailObject = existingTrail.gameObject;
            }
            else
            {
                // Create new trail GameObject
                trailObject = new GameObject("WeaponTrail");
                trailObject.transform.SetParent(attachPoint);

                // Apply height offset if attached to character root
                if (useHeightOffset)
                {
                    trailObject.transform.localPosition = Vector3.up * defaultTrailHeight;
                }
                else
                {
                    trailObject.transform.localPosition = Vector3.zero;
                }

                trailObject.transform.localRotation = Quaternion.identity;
            }

            // Add or get WeaponTrailController component
            trailController = trailObject.GetComponent<WeaponTrailController>();
            if (trailController == null)
            {
                trailController = trailObject.AddComponent<WeaponTrailController>();
            }

            Debug.Log($"WeaponController on {gameObject.name}: Auto-created WeaponTrailController at {(useHeightOffset ? $"height offset {defaultTrailHeight}" : "weapon model")} " +
                     $"(position: {trailObject.transform.position}, parent: {attachPoint.name})");
        }

        public void DrawMeleeSlash(SkillType skillType, float duration, Transform target = null)
        {
            if (trailController != null)
            {
                trailController.DrawMeleeSlash(skillType, duration, target);
            }
        }

        private void OnDrawGizmosSelected()
        {
            if (!showRangeGizmos) return;

            #if UNITY_EDITOR
            float labelStartHeight = 0f;

            // Draw primary weapon ranges
            if (primaryWeapon != null)
            {
                DrawWeaponRanges(primaryWeapon, true, isPrimaryActive, ref labelStartHeight);
            }

            // Draw secondary weapon ranges
            if (secondaryWeapon != null && secondaryWeapon != primaryWeapon)
            {
                DrawWeaponRanges(secondaryWeapon, false, !isPrimaryActive, ref labelStartHeight);
            }

            // Draw active weapon indicator at the top
            if (ActiveWeapon != null)
            {
                string activeSlot = isPrimaryActive ? "PRIMARY" : "SECONDARY";
                UnityEditor.Handles.Label(
                    transform.position + Vector3.up * (labelStartHeight + 0.5f),
                    $"<b><color={(isPrimaryActive ? "red" : "yellow")}>ACTIVE: {activeSlot}</color></b>"
                );
            }
            #endif
        }

        #if UNITY_EDITOR
        private void DrawWeaponRanges(WeaponData weapon, bool isPrimary, bool isActive, ref float labelHeight)
        {
            // Calculate ranges
            float meleeRange = weapon.meleeRange;
            float rangedRange = weapon.rangedRange;

            // Color scheme: Primary = Red/Cyan, Secondary = Yellow/Magenta
            Color meleeColor = isPrimary ? Color.red : Color.yellow;
            Color rangedColor = isPrimary ? Color.cyan : Color.magenta;

            // Make inactive weapons slightly transparent
            if (!isActive)
            {
                meleeColor.a = 0.5f;
                rangedColor.a = 0.5f;
            }

            // Draw melee range
            Gizmos.color = meleeColor;
            Gizmos.DrawWireSphere(transform.position, meleeRange);

            // Draw ranged range if weapon has ranged capability
            if (rangedRange > 0.1f)
            {
                Gizmos.color = rangedColor;
                Gizmos.DrawWireSphere(transform.position, rangedRange);
            }

            // Determine label position (stack labels vertically)
            float maxRange = Mathf.Max(meleeRange, rangedRange);
            if (labelHeight == 0f)
            {
                labelHeight = maxRange + 0.5f;
            }

            // Draw weapon info label
            string slotName = isPrimary ? "PRIMARY" : "SECONDARY";
            string activeMarker = isActive ? " ★" : "";
            string labelColor = isPrimary ? "red" : "#CCCC00"; // Yellow in hex

            UnityEditor.Handles.Label(
                transform.position + Vector3.up * labelHeight,
                $"<color={labelColor}><b>{slotName}{activeMarker}</b>: {weapon.weaponName}</color>\n" +
                $"Melee: {meleeRange:F1} | Ranged: {rangedRange:F1}\n" +
                $"Damage: {weapon.baseDamage} | Speed: {weapon.speed:F1}"
            );

            // Update label height for next weapon
            labelHeight += 2.5f;
        }
        #endif

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