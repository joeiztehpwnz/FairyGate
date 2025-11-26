using UnityEngine;

namespace FairyGate.Combat
{
    public class WeaponTrailController : MonoBehaviour
    {
        [Header("Trail Settings")]
        [SerializeField] private int arcSegments = 12; // Number of line segments for smooth arcs

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = false;

        private WeaponData currentWeaponData;

        public void Initialize(WeaponData weaponData)
        {
            if (weaponData == null)
            {
                CombatLogger.LogCombat($"WeaponTrailController on {gameObject.name}: Cannot initialize with null WeaponData", CombatLogger.LogLevel.Warning);
                return;
            }

            currentWeaponData = weaponData;

            if (showDebugInfo)
            {
                CombatLogger.LogCombat($"WeaponTrailController initialized for {weaponData.weaponName}");
            }
        }

        public void DrawMeleeSlash(SkillType skillType, float duration, Transform target = null)
        {
            if (currentWeaponData == null)
            {
                CombatLogger.LogCombat("WeaponTrailController: Cannot draw slash - weapon data not initialized", CombatLogger.LogLevel.Warning);
                return;
            }

            // Use weapon's melee range directly
            float weaponRange = currentWeaponData.meleeRange;

            // Calculate direction to target if available
            Vector3 directionToTarget = Vector3.forward;
            if (target != null)
            {
                directionToTarget = (target.position - transform.position).normalized;
            }

            // Create slash line based on skill type
            switch (skillType)
            {
                case SkillType.Attack:
                    DrawHorizontalSlash(duration, weaponRange, target, directionToTarget);
                    break;
                case SkillType.Smash:
                    DrawOverheadSlash(duration, weaponRange, target, directionToTarget);
                    break;
                case SkillType.Windmill:
                    Draw360Spin(duration, weaponRange);
                    break;
                case SkillType.Lunge:
                    DrawForwardThrust(duration, weaponRange, target, directionToTarget);
                    break;
                default:
                    DrawHorizontalSlash(duration, weaponRange, target, directionToTarget);
                    break;
            }

            if (showDebugInfo)
            {
                CombatLogger.LogCombat($"WeaponTrailController: Drew {skillType} slash (duration: {duration:F2}s, range: {weaponRange:F2}, target: {(target != null ? target.name : "none")})");
            }
        }

        private void DrawHorizontalSlash(float duration, float weaponRange, Transform target, Vector3 directionToTarget)
        {
            GameObject slashObj = new GameObject("HorizontalSlash");
            slashObj.transform.SetParent(transform);
            slashObj.transform.localPosition = Vector3.zero;

            LineRenderer line = slashObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line, arcSegments);

            // Create horizontal arc oriented toward target
            Vector3 startPos = transform.position;
            Vector3 endPos = target != null ? target.position : startPos + directionToTarget * weaponRange;

            // Calculate perpendicular vector for the sweep
            Vector3 right = Vector3.Cross(directionToTarget, Vector3.up).normalized;
            Vector3 midPoint = startPos + directionToTarget * weaponRange * 0.5f;

            Vector3[] positions = new Vector3[arcSegments];
            for (int i = 0; i < arcSegments; i++)
            {
                float t = (float)i / (arcSegments - 1);
                float arcT = Mathf.Sin(t * Mathf.PI); // Arc intensity (peaks at middle)

                // Sweep from left to right around the midpoint
                float offset = (t - 0.5f) * 2f; // -1 to 1
                positions[i] = midPoint + right * offset * weaponRange * 0.5f + directionToTarget * arcT * weaponRange * 0.3f;
            }

            line.SetPositions(positions);
            Destroy(slashObj, duration);
        }

        private void DrawOverheadSlash(float duration, float weaponRange, Transform target, Vector3 directionToTarget)
        {
            GameObject slashObj = new GameObject("OverheadSlash");
            slashObj.transform.SetParent(transform);
            slashObj.transform.localPosition = Vector3.zero;

            LineRenderer line = slashObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line, arcSegments);

            // Create vertical arc from overhead down toward target
            Vector3 startPos = transform.position;
            Vector3 targetPos = target != null ? target.position : startPos + directionToTarget * weaponRange;

            Vector3[] positions = new Vector3[arcSegments];
            for (int i = 0; i < arcSegments; i++)
            {
                float t = (float)i / (arcSegments - 1);
                float angle = Mathf.Lerp(-45f, 135f, t) * Mathf.Deg2Rad;

                // Arc from high overhead down toward target
                Vector3 verticalOffset = Vector3.up * Mathf.Sin(angle) * weaponRange;
                Vector3 forwardOffset = directionToTarget * Mathf.Cos(angle) * weaponRange;

                positions[i] = startPos + verticalOffset + forwardOffset;
            }

            line.SetPositions(positions);
            Destroy(slashObj, duration);
        }

        private void Draw360Spin(float duration, float weaponRange)
        {
            GameObject slashObj = new GameObject("WindmillSpin");
            slashObj.transform.SetParent(transform);
            slashObj.transform.localPosition = Vector3.zero;

            LineRenderer line = slashObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line, arcSegments * 2); // More segments for full circle

            // Create full circular path
            int totalSegments = arcSegments * 2;
            Vector3[] positions = new Vector3[totalSegments + 1]; // +1 to close the circle
            for (int i = 0; i <= totalSegments; i++)
            {
                float t = (float)i / totalSegments;
                float angle = t * 360f * Mathf.Deg2Rad;

                positions[i] = transform.position + new Vector3(
                    Mathf.Sin(angle) * weaponRange,
                    0f,
                    Mathf.Cos(angle) * weaponRange
                );
            }

            line.positionCount = totalSegments + 1;
            line.SetPositions(positions);
            Destroy(slashObj, duration);
        }

        private void DrawForwardThrust(float duration, float weaponRange, Transform target, Vector3 directionToTarget)
        {
            GameObject slashObj = new GameObject("ForwardThrust");
            slashObj.transform.SetParent(transform);
            slashObj.transform.localPosition = Vector3.zero;

            LineRenderer line = slashObj.AddComponent<LineRenderer>();
            ConfigureLineRenderer(line, 2); // Simple straight line

            // Draw straight line directly toward target
            Vector3[] positions = new Vector3[2];
            positions[0] = transform.position;
            positions[1] = target != null ? target.position : transform.position + directionToTarget * weaponRange;

            line.SetPositions(positions);
            Destroy(slashObj, duration);
        }

        /// <summary>
        /// Draws a ranged attack trail from source to target position.
        /// Creates a visible arrow/projectile trail in the game view.
        /// </summary>
        /// <param name="sourcePosition">Starting position of the arrow</param>
        /// <param name="targetPosition">Target position of the arrow</param>
        /// <param name="weaponData">Weapon data containing trail visual properties</param>
        /// <param name="isHit">Whether the ranged attack hit (affects color)</param>
        public void DrawRangedTrail(Vector3 sourcePosition, Vector3 targetPosition, WeaponData weaponData, bool isHit)
        {
            if (weaponData == null)
            {
                CombatLogger.LogCombat("WeaponTrailController: Cannot draw ranged trail - weapon data is null", CombatLogger.LogLevel.Warning);
                return;
            }

            // Create trail GameObject
            GameObject trailObj = new GameObject("RangedAttackTrail");
            trailObj.transform.position = Vector3.zero;

            // Add and configure LineRenderer
            LineRenderer line = trailObj.AddComponent<LineRenderer>();

            // Configure visual properties
            line.startWidth = weaponData.trailWidth;
            line.endWidth = weaponData.trailWidth * 0.5f; // Taper toward target

            // Color based on hit/miss
            if (isHit)
            {
                line.startColor = weaponData.trailColorStart; // Yellow for hits
                line.endColor = weaponData.trailColorEnd;     // Red at impact
            }
            else
            {
                // Red trail for misses
                line.startColor = Color.red;
                line.endColor = new Color(0.5f, 0.1f, 0.1f, 0.8f); // Darker red
            }

            // Material/Shader
            line.material = new Material(Shader.Find("Sprites/Default"));

            // Smoothness settings
            line.numCornerVertices = 2;
            line.numCapVertices = 2;

            // Alignment - face camera for best visibility
            line.alignment = LineAlignment.View;

            // Texture mode
            line.textureMode = LineTextureMode.Stretch;

            // Shadow settings
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;

            // Set trail positions (straight line from source to target)
            line.positionCount = 2;
            Vector3[] positions = new Vector3[2];
            positions[0] = sourcePosition;
            positions[1] = targetPosition;
            line.SetPositions(positions);

            // Auto-destroy after 0.5 seconds
            Destroy(trailObj, 0.5f);

            if (showDebugInfo)
            {
                CombatLogger.LogCombat($"WeaponTrailController: Drew ranged trail ({(isHit ? "HIT" : "MISS")}) from {sourcePosition} to {targetPosition}");
            }
        }

        private void ConfigureLineRenderer(LineRenderer line, int positionCount)
        {
            // Width with tapering support
            line.startWidth = currentWeaponData.meleeTrailWidth * currentWeaponData.meleeTrailStartWidthMultiplier;
            line.endWidth = currentWeaponData.meleeTrailWidth * currentWeaponData.meleeTrailEndWidthMultiplier;

            // Color with gradient support
            line.startColor = currentWeaponData.meleeTrailColor;
            line.endColor = currentWeaponData.meleeTrailEndColor;

            // Material/Shader
            line.material = new Material(Shader.Find("Sprites/Default"));

            // Smoothness settings
            line.numCornerVertices = currentWeaponData.meleeTrailCornerVertices;
            line.numCapVertices = currentWeaponData.meleeTrailEndCapVertices;

            // Alignment - face camera for best visibility
            line.alignment = LineAlignment.View;

            // Texture mode - stretch along line
            line.textureMode = LineTextureMode.Stretch;

            // Position count
            line.positionCount = positionCount;

            // Shadow settings
            line.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            line.receiveShadows = false;
        }
    }
}
