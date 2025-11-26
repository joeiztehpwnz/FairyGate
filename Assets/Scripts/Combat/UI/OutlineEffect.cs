using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Creates a simple outline effect by rendering a slightly larger duplicate mesh behind the original.
    /// Used for visual feedback when an enemy is targeted by the player.
    /// </summary>
    public class OutlineEffect : MonoBehaviour
    {
        [Header("Outline Settings")]
        [SerializeField] private Color outlineColor = new Color(1f, 0.9f, 0f, 1f); // Yellow/gold
        [SerializeField] private float outlineWidth = 0.03f;
        [SerializeField] private bool showOutline = false;

        private GameObject outlineObject;
        private Material outlineMaterial;
        private MeshFilter[] meshFilters;

        private void Awake()
        {
            // Find all mesh filters in children (for multi-mesh characters)
            meshFilters = GetComponentsInChildren<MeshFilter>();

            if (meshFilters.Length == 0)
            {
                CombatLogger.LogUI($"OutlineEffect on {gameObject.name} found no MeshFilters. Outline will not render.", CombatLogger.LogLevel.Warning);
                return;
            }

            CreateOutline();
        }

        private void CreateOutline()
        {
            // Create outline material using custom shader with front-face culling
            Shader shader = Shader.Find("Custom/OutlineShader");
            if (shader == null)
            {
                CombatLogger.LogUI($"OutlineEffect on {gameObject.name}: Custom/OutlineShader not found! Outline will not work correctly.", CombatLogger.LogLevel.Error);
                return;
            }

            outlineMaterial = new Material(shader);
            outlineMaterial.SetColor("_Color", outlineColor);

            // Create container object for outline meshes
            outlineObject = new GameObject("Outline");
            outlineObject.transform.SetParent(transform);
            outlineObject.transform.localPosition = Vector3.zero;
            outlineObject.transform.localRotation = Quaternion.identity;
            outlineObject.transform.localScale = Vector3.one * (1f + outlineWidth);

            // Duplicate each mesh for outline
            foreach (var meshFilter in meshFilters)
            {
                if (meshFilter.sharedMesh == null) continue;

                GameObject outlinePart = new GameObject($"Outline_{meshFilter.gameObject.name}");
                outlinePart.transform.SetParent(outlineObject.transform);
                outlinePart.transform.localPosition = meshFilter.transform.localPosition;
                outlinePart.transform.localRotation = meshFilter.transform.localRotation;
                outlinePart.transform.localScale = meshFilter.transform.localScale;

                // Add mesh filter and renderer
                var outlineMeshFilter = outlinePart.AddComponent<MeshFilter>();
                outlineMeshFilter.sharedMesh = meshFilter.sharedMesh;

                var outlineRenderer = outlinePart.AddComponent<MeshRenderer>();
                outlineRenderer.material = outlineMaterial;
                outlineRenderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                outlineRenderer.receiveShadows = false;
            }

            // Set initial visibility
            outlineObject.SetActive(showOutline);
        }

        /// <summary>
        /// Enable or disable the outline effect
        /// </summary>
        public void SetOutlineEnabled(bool enabled)
        {
            showOutline = enabled;

            if (outlineObject != null)
            {
                outlineObject.SetActive(enabled);
            }
        }

        /// <summary>
        /// Change outline color at runtime
        /// </summary>
        public void SetOutlineColor(Color color)
        {
            outlineColor = color;

            if (outlineMaterial != null)
            {
                outlineMaterial.SetColor("_Color", color);
            }
        }

        /// <summary>
        /// Change outline width at runtime
        /// </summary>
        public void SetOutlineWidth(float width)
        {
            outlineWidth = width;

            if (outlineObject != null)
            {
                outlineObject.transform.localScale = Vector3.one * (1f + width);
            }
        }

        private void OnDestroy()
        {
            // Clean up created material
            if (outlineMaterial != null)
            {
                if (Application.isPlaying)
                    Destroy(outlineMaterial);
                else
                    DestroyImmediate(outlineMaterial);
            }
        }

        private void OnValidate()
        {
            // Update outline in editor when values change
            if (outlineMaterial != null)
            {
                outlineMaterial.SetColor("_Color", outlineColor);
            }

            if (outlineObject != null)
            {
                outlineObject.transform.localScale = Vector3.one * (1f + outlineWidth);
                outlineObject.SetActive(showOutline);
            }
        }
    }
}
