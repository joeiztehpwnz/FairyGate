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
                Debug.LogWarning($"WeaponTrailController on {gameObject.name}: Cannot initialize with null WeaponData");
                return;
            }

            currentWeaponData = weaponData;

            if (showDebugInfo)
            {
                Debug.Log($"WeaponTrailController initialized for {weaponData.weaponName}");
            }
        }

        public void DrawMeleeSlash(SkillType skillType, float duration, Transform target = null)
        {
            if (currentWeaponData == null)
            {
                Debug.LogWarning("WeaponTrailController: Cannot draw slash - weapon data not initialized");
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
                Debug.Log($"WeaponTrailController: Drew {skillType} slash (duration: {duration:F2}s, range: {weaponRange:F2}, target: {(target != null ? target.name : "none")})");
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
