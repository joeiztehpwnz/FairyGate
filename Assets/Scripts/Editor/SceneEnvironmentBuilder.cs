using UnityEngine;
using UnityEditor;
using FairyGate.Combat;

namespace FairyGate.Combat.Editor
{
    /// <summary>
    /// Responsible for creating and configuring the combat scene environment.
    /// Handles ground plane, camera setup, and lighting configuration.
    /// </summary>
    public class SceneEnvironmentBuilder
    {
        /// <summary>
        /// Creates all environment components: ground, camera, and lighting.
        /// </summary>
        public void CreateEnvironment()
        {
            CreateGroundPlane();
            CreateMainCamera();
            CreateLightingSetup();
        }

        /// <summary>
        /// Creates a 30x30 ground plane with dark green material.
        /// </summary>
        public void CreateGroundPlane()
        {
            if (GameObject.Find("Ground") == null)
            {
                var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
                ground.name = "Ground";
                ground.transform.position = Vector3.zero;
                ground.transform.localScale = new Vector3(3, 1, 3); // 30x30 units

                var groundRenderer = ground.GetComponent<Renderer>();
                if (groundRenderer != null)
                {
                    var groundMaterial = new Material(Shader.Find("Standard"));
                    groundMaterial.color = new Color(0.0314f, 0.3412f, 0.0314f); // #085708 - Dark green
                    groundRenderer.material = groundMaterial;
                }

                Debug.Log("✅ Created Ground (30x30 units)");
            }
        }

        /// <summary>
        /// Creates main camera with CameraController for player following.
        /// </summary>
        public void CreateMainCamera()
        {
            if (Camera.main == null)
            {
                var cameraGO = new GameObject("Main Camera");
                var camera = cameraGO.AddComponent<Camera>();
                cameraGO.tag = "MainCamera";

                // Position for good combat view (will be overridden by CameraController)
                cameraGO.transform.position = new Vector3(0f, 10f, -8f);
                cameraGO.transform.rotation = Quaternion.Euler(50f, 0f, 0f);

                camera.fieldOfView = 60f;
                camera.nearClipPlane = 0.3f;
                camera.farClipPlane = 100f;
                camera.clearFlags = CameraClearFlags.Skybox;

                // Add CameraController for player following
                var cameraController = cameraGO.AddComponent<CameraController>();
                EditorUtilities.SetSerializedProperty(cameraController, "autoFindPlayer", true);
                EditorUtilities.SetSerializedProperty(cameraController, "distance", 10f);
                EditorUtilities.SetSerializedProperty(cameraController, "height", 8f);
                EditorUtilities.SetSerializedProperty(cameraController, "rotationSpeed", 90f);
                EditorUtilities.SetSerializedProperty(cameraController, "enableZoom", true);
                EditorUtilities.SetSerializedProperty(cameraController, "showDebugInfo", true);

                Debug.Log("✅ Created Main Camera with CameraController");
            }
        }

        /// <summary>
        /// Creates directional light with soft shadows.
        /// </summary>
        public void CreateLightingSetup()
        {
            if (GameObject.Find("Directional Light") == null)
            {
                var lightGO = new GameObject("Directional Light");
                var light = lightGO.AddComponent<Light>();

                light.type = LightType.Directional;
                light.color = Color.white;
                light.intensity = 1f;
                light.shadows = LightShadows.Soft;
                lightGO.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                Debug.Log("✅ Created Directional Light");
            }
        }
    }
}
