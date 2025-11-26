using UnityEngine;
using UnityEngine.SceneManagement;

namespace FairyGate.Combat
{
    public class GameManager : Singleton<GameManager>
    {
        [Header("Game Configuration")]
        [SerializeField] private KeyCode resetKey = KeyCode.R;
        [SerializeField] private bool enableDebugLogs = true;

        [Header("Scene Management")]
        [SerializeField] private string combatSceneName = "CombatTest"; // Reserved for future scene switching functionality

        private bool gameEnded = false;

        protected override bool PersistAcrossScenes => true;

        private void Update()
        {
            HandleInput();
        }

        private void HandleInput()
        {
            if (Input.GetKeyDown(resetKey))
            {
                ResetScene();
            }
        }

        public void OnCharacterDied(HealthSystem deadCharacter)
        {
            if (gameEnded) return;

            gameEnded = true;

            if (enableDebugLogs)
            {
                CombatLogger.LogSystem($"Game ended! {deadCharacter.name} has died. Press {resetKey} to reset.");
            }

            // Display death message
            ShowDeathMessage(deadCharacter.name);
        }

        private void ShowDeathMessage(string characterName)
        {
            // This could be enhanced with a proper UI system
            CombatLogger.LogSystem($"=== GAME OVER ===\n{characterName} has been defeated!\nPress '{resetKey}' to reset and continue testing.");
        }

        public void ResetScene()
        {
            if (enableDebugLogs)
            {
                CombatLogger.LogSystem("Resetting scene for continued testing...");
            }

            gameEnded = false;

            // Reload current scene, or use the designated combat scene name
            string sceneToLoad = string.IsNullOrEmpty(combatSceneName) ?
                SceneManager.GetActiveScene().name : combatSceneName;
            SceneManager.LoadScene(sceneToLoad);
        }

        public void SetGameEnded(bool ended)
        {
            gameEnded = ended;
        }

        public bool IsGameEnded()
        {
            return gameEnded;
        }

        private void OnGUI()
        {
            if (gameEnded)
            {
                // Draw reset prompt
                GUIStyle style = new GUIStyle(GUI.skin.label);
                style.fontSize = 24;
                style.normal.textColor = Color.white;
                style.alignment = TextAnchor.MiddleCenter;

                GUI.Label(new Rect(0, Screen.height * 0.3f, Screen.width, 100),
                    $"GAME OVER\nPress '{resetKey}' to Reset", style);
            }

            // Always show controls in corner
            GUIStyle smallStyle = new GUIStyle(GUI.skin.label);
            smallStyle.fontSize = 12;
            smallStyle.normal.textColor = Color.yellow;

            GUI.Label(new Rect(10, 10, 200, 60),
                $"Controls:\n" +
                $"WASD - Move\n" +
                $"1-5 - Skills\n" +
                $"TAB - Target\n" +
                $"ESC - Exit Combat\n" +
                $"X - Rest\n" +
                $"Space - Cancel Skill\n" +
                $"{resetKey} - Reset Scene",
                smallStyle);
        }
    }
}