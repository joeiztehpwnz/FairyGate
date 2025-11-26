using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Simple camera controller that follows the player with isometric view.
    /// Arrow keys rotate camera around the player.
    /// </summary>
    public class CameraController : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform playerTarget;
        [SerializeField] private bool autoFindPlayer = true;

        [Header("Camera Settings")]
        [SerializeField] private float distance = 10f;
        [SerializeField] private float height = 8f;
        [SerializeField] private float rotationSpeed = 90f; // Degrees per second

        [Header("Zoom Settings")]
        [SerializeField] private bool enableZoom = true;
        [SerializeField] private float minDistance = 5f;
        [SerializeField] private float maxDistance = 20f;
        [SerializeField] private float zoomSpeed = 5f;

        [Header("Debug")]
        [SerializeField] private bool showDebugInfo = true;

        private float currentRotationAngle = 0f;

        private void Start()
        {
            if (autoFindPlayer)
            {
                FindPlayer();
            }

            if (playerTarget == null)
            {
                CombatLogger.LogSystem("CameraController: No player target found! Camera will not follow.", CombatLogger.LogLevel.Error);
            }
        }

        private void LateUpdate()
        {
            if (playerTarget == null) return;

            HandleInput();
            UpdateCameraPosition();
        }

        private void HandleInput()
        {
            // Horizontal rotation with Left/Right arrow keys
            if (Input.GetKey(KeyCode.LeftArrow))
            {
                currentRotationAngle += rotationSpeed * Time.deltaTime;
            }
            if (Input.GetKey(KeyCode.RightArrow))
            {
                currentRotationAngle -= rotationSpeed * Time.deltaTime;
            }

            // Zoom with Up/Down arrow keys (optional)
            if (enableZoom)
            {
                if (Input.GetKey(KeyCode.UpArrow))
                {
                    distance = Mathf.Max(minDistance, distance - zoomSpeed * Time.deltaTime);
                }
                if (Input.GetKey(KeyCode.DownArrow))
                {
                    distance = Mathf.Min(maxDistance, distance + zoomSpeed * Time.deltaTime);
                }
            }

            // Normalize angle to 0-360 range
            if (currentRotationAngle >= 360f) currentRotationAngle -= 360f;
            if (currentRotationAngle < 0f) currentRotationAngle += 360f;
        }

        private void UpdateCameraPosition()
        {
            // Calculate camera position in orbit around player
            Quaternion rotation = Quaternion.Euler(0f, currentRotationAngle, 0f);
            Vector3 offset = rotation * new Vector3(0f, 0f, -distance);
            Vector3 targetPosition = playerTarget.position + offset + Vector3.up * height;

            // Immediate following (no smoothing)
            transform.position = targetPosition;

            // Look at player
            transform.LookAt(playerTarget.position + Vector3.up * 1f); // Look at center of player
        }

        private void FindPlayer()
        {
            // Try to find player by tag
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");

            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                CombatLogger.LogSystem($"CameraController: Found player by tag '{playerObj.name}'");
                return;
            }

            // Try to find GameObject named "Player" (case-insensitive)
            playerObj = GameObject.Find("Player");
            if (playerObj != null)
            {
                playerTarget = playerObj.transform;
                CombatLogger.LogSystem($"CameraController: Found player by name '{playerObj.name}'");
                return;
            }

            // Try to find any CombatController (assumes first one is player in simple setups)
            var combatController = FindFirstObjectByType<CombatController>();
            if (combatController != null)
            {
                playerTarget = combatController.transform;
                CombatLogger.LogSystem($"CameraController: Found player by CombatController '{combatController.name}' (assumed first is player)");
                return;
            }

            CombatLogger.LogSystem("CameraController: Could not auto-find player. Please assign manually.", CombatLogger.LogLevel.Warning);
        }

        private void OnGUI()
        {
            if (showDebugInfo && Application.isPlaying)
            {
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.normal.textColor = Color.white;
                style.fontSize = 12;

                string debugText = $"Camera Controls:\n" +
                                   $"  Left/Right Arrow - Rotate\n" +
                                   (enableZoom ? $"  Up/Down Arrow - Zoom\n" : "") +
                                   $"\n" +
                                   $"Current Angle: {currentRotationAngle:F0}°\n" +
                                   $"Distance: {distance:F1}";

                GUI.Label(new Rect(Screen.width - 220, 10, 210, 100), debugText, style);
            }
        }

        // Public methods for external control
        public void SetTarget(Transform target)
        {
            playerTarget = target;
        }

        public void SetRotation(float angle)
        {
            currentRotationAngle = angle;
        }

        public void SetDistance(float dist)
        {
            distance = Mathf.Clamp(dist, minDistance, maxDistance);
        }
    }
}
