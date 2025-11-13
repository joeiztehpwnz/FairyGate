using UnityEngine;
using System.Collections;

namespace FairyGate.Combat
{
    /// <summary>
    /// Telegraph System provides visual and audio warnings before AI skill execution.
    ///
    /// Classic Mabinogi Design: Telegraphs are subtle but learnable. Players who observe
    /// patterns can predict attacks and react appropriately. This rewards knowledge over reflexes.
    ///
    /// Telegraph Timing: 0.3-0.5s before skill charge begins (fair warning window)
    /// </summary>
    public class TelegraphSystem : MonoBehaviour
    {
        [Header("Visual Telegraph Components")]
        [SerializeField] private Renderer enemyRenderer;
        [SerializeField] private Transform weaponTransform;
        [SerializeField] private ParticleSystem telegraphParticles;

        [Header("Audio Configuration")]
        [SerializeField] private AudioSource audioSource;
        [SerializeField] private float audioSpatialBlend = 1.0f; // Fully 3D audio

        [Header("Visual Effects Configuration")]
        [SerializeField] private float glowIntensity = 2.0f;
        [SerializeField] private string emissionColorProperty = "_EmissionColor";

        [Header("Debug")]
        [SerializeField] private bool enableDebugLogs = false;

        // State tracking
        private bool isTelegraphing = false;
        private Coroutine currentTelegraphCoroutine = null;
        private Material enemyMaterial;
        private Color originalEmissionColor;

        private void Awake()
        {
            // Setup audio source
            if (audioSource == null)
            {
                audioSource = gameObject.AddComponent<AudioSource>();
            }

            audioSource.spatialBlend = audioSpatialBlend;
            audioSource.playOnAwake = false;
            audioSource.loop = false;

            // Cache enemy material for glow effects
            if (enemyRenderer != null)
            {
                enemyMaterial = enemyRenderer.material; // Creates instance
                if (enemyMaterial.HasProperty(emissionColorProperty))
                {
                    originalEmissionColor = enemyMaterial.GetColor(emissionColorProperty);
                }
            }
        }

        private void OnDestroy()
        {
            // Clean up material instance
            if (enemyMaterial != null)
            {
                Destroy(enemyMaterial);
            }
        }

        /// <summary>
        /// Displays a telegraph for the given skill.
        /// Called by PatternExecutor before skill charging begins.
        /// </summary>
        public void ShowTelegraph(TelegraphData telegraphData, SkillType upcomingSkill)
        {
            if (telegraphData == null)
            {
                if (enableDebugLogs)
                {
                    Debug.Log($"[TelegraphSystem] {gameObject.name} - No telegraph data for {upcomingSkill}");
                }
                return;
            }

            // Cancel any ongoing telegraph
            if (currentTelegraphCoroutine != null)
            {
                StopCoroutine(currentTelegraphCoroutine);
                ClearTelegraph();
            }

            // Start new telegraph
            currentTelegraphCoroutine = StartCoroutine(TelegraphCoroutine(telegraphData, upcomingSkill));
        }

        /// <summary>
        /// Immediately cancels and clears any active telegraph.
        /// </summary>
        public void CancelTelegraph()
        {
            if (currentTelegraphCoroutine != null)
            {
                StopCoroutine(currentTelegraphCoroutine);
                currentTelegraphCoroutine = null;
            }

            ClearTelegraph();
        }

        private IEnumerator TelegraphCoroutine(TelegraphData data, SkillType skill)
        {
            isTelegraphing = true;

            if (enableDebugLogs)
            {
                Debug.Log($"[TelegraphSystem] {gameObject.name} telegraphing {skill} ({data.visualType}) for {data.duration}s");
            }

            // Play audio telegraph
            PlayAudioTelegraph(data);

            // Display visual telegraph
            DisplayVisualTelegraph(data);

            // Wait for telegraph duration
            yield return new WaitForSeconds(data.duration);

            // Clear telegraph
            ClearTelegraph();

            isTelegraphing = false;
            currentTelegraphCoroutine = null;
        }

        private void PlayAudioTelegraph(TelegraphData data)
        {
            if (string.IsNullOrEmpty(data.audioClip))
                return;

            // Load audio clip from Resources
            AudioClip clip = Resources.Load<AudioClip>($"Audio/Telegraphs/{data.audioClip}");

            if (clip != null)
            {
                audioSource.clip = clip;
                audioSource.volume = data.audioVolume;
                audioSource.Play();

                if (enableDebugLogs)
                {
                    Debug.Log($"[TelegraphSystem] {gameObject.name} playing audio telegraph: {data.audioClip}");
                }
            }
            else
            {
                if (enableDebugLogs)
                {
                    Debug.LogWarning($"[TelegraphSystem] {gameObject.name} failed to load audio clip: {data.audioClip}");
                }
            }
        }

        private void DisplayVisualTelegraph(TelegraphData data)
        {
            switch (data.visualType)
            {
                case TelegraphVisual.None:
                    // No visual telegraph
                    break;

                case TelegraphVisual.StanceShift:
                    DisplayStanceShift(data);
                    break;

                case TelegraphVisual.WeaponRaise:
                    DisplayWeaponRaise(data);
                    break;

                case TelegraphVisual.ShieldRaise:
                    DisplayShieldRaise(data);
                    break;

                case TelegraphVisual.EyeGlow:
                    DisplayEyeGlow(data);
                    break;

                case TelegraphVisual.GroundEffect:
                    DisplayGroundEffect(data);
                    break;

                case TelegraphVisual.Crouch:
                    DisplayCrouch(data);
                    break;

                case TelegraphVisual.BackStep:
                    DisplayBackStep(data);
                    break;

                case TelegraphVisual.ParticleEffect:
                    DisplayParticleEffect(data);
                    break;

                default:
                    Debug.LogWarning($"[TelegraphSystem] Unknown telegraph visual type: {data.visualType}");
                    break;
            }
        }

        private void ClearTelegraph()
        {
            // Stop any audio
            if (audioSource != null && audioSource.isPlaying)
            {
                audioSource.Stop();
            }

            // Clear glow effect
            if (enemyMaterial != null && enemyMaterial.HasProperty(emissionColorProperty))
            {
                enemyMaterial.SetColor(emissionColorProperty, originalEmissionColor);
            }

            // Stop particle effects
            if (telegraphParticles != null && telegraphParticles.isPlaying)
            {
                telegraphParticles.Stop();
            }

            isTelegraphing = false;
        }

        // Visual telegraph implementations

        private void DisplayStanceShift(TelegraphData data)
        {
            // Subtle body position animation
            // TODO: Integrate with animator for stance shift animation
            // For now, use simple glow effect
            DisplayEyeGlow(data);
        }

        private void DisplayWeaponRaise(TelegraphData data)
        {
            // Weapon moves to attack position
            if (weaponTransform != null)
            {
                // TODO: Animate weapon raise
                // For now, use particle effect at weapon position
                DisplayParticleEffect(data, weaponTransform.position);
            }
        }

        private void DisplayShieldRaise(TelegraphData data)
        {
            // Shield/defensive posture
            // TODO: Integrate with animator for shield raise animation
            // For now, use blue glow
            Color defensiveColor = new Color(0.2f, 0.5f, 1.0f); // Blue
            DisplayEyeGlow(data, defensiveColor);
        }

        private void DisplayEyeGlow(TelegraphData data, Color? overrideColor = null)
        {
            // Eyes glow with skill color
            if (enemyMaterial != null && enemyMaterial.HasProperty(emissionColorProperty))
            {
                Color glowColor = overrideColor ?? data.glowColor;
                Color emissionColor = glowColor * glowIntensity;
                enemyMaterial.SetColor(emissionColorProperty, emissionColor);
                enemyMaterial.EnableKeyword("_EMISSION");

                if (enableDebugLogs)
                {
                    Debug.Log($"[TelegraphSystem] {gameObject.name} displaying eye glow: {glowColor}");
                }
            }
        }

        private void DisplayGroundEffect(TelegraphData data)
        {
            // AoE indicator on ground (for Windmill, etc.)
            // TODO: Spawn ground decal/ring at enemy feet
            // For now, use particle effect at ground level
            Vector3 groundPosition = transform.position;
            groundPosition.y = 0.1f; // Slightly above ground
            DisplayParticleEffect(data, groundPosition);
        }

        private void DisplayCrouch(TelegraphData data)
        {
            // Lower body before Counter
            // TODO: Integrate with animator for crouch animation
            // For now, use purple glow (Counter color)
            Color counterColor = new Color(0.8f, 0.2f, 1.0f); // Purple
            DisplayEyeGlow(data, counterColor);
        }

        private void DisplayBackStep(TelegraphData data)
        {
            // Step back before Lunge/retreat
            // TODO: Apply slight backward movement
            // For now, use yellow glow (movement indicator)
            Color movementColor = new Color(1.0f, 1.0f, 0.2f); // Yellow
            DisplayEyeGlow(data, movementColor);
        }

        private void DisplayParticleEffect(TelegraphData data, Vector3? customPosition = null)
        {
            if (telegraphParticles != null)
            {
                // Position particles
                if (customPosition.HasValue)
                {
                    telegraphParticles.transform.position = customPosition.Value;
                }
                else
                {
                    telegraphParticles.transform.position = transform.position + Vector3.up * 1.5f;
                }

                // Configure particles
                var main = telegraphParticles.main;
                main.startColor = data.glowColor;
                main.duration = data.duration;

                // Play particles
                telegraphParticles.Play();

                if (enableDebugLogs)
                {
                    Debug.Log($"[TelegraphSystem] {gameObject.name} displaying particle effect");
                }
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Test telegraph visuals in editor.
        /// </summary>
        [ContextMenu("Test Telegraph (EyeGlow)")]
        private void TestTelegraphEyeGlow()
        {
            TelegraphData testData = new TelegraphData
            {
                visualType = TelegraphVisual.EyeGlow,
                glowColor = Color.red,
                audioClip = "",
                duration = 1.0f
            };

            ShowTelegraph(testData, SkillType.Smash);
        }

        [ContextMenu("Test Telegraph (ParticleEffect)")]
        private void TestTelegraphParticle()
        {
            TelegraphData testData = new TelegraphData
            {
                visualType = TelegraphVisual.ParticleEffect,
                glowColor = Color.cyan,
                audioClip = "",
                duration = 1.0f
            };

            ShowTelegraph(testData, SkillType.Windmill);
        }

        [ContextMenu("Clear Telegraph")]
        private void TestClearTelegraph()
        {
            CancelTelegraph();
        }
        #endif
    }
}
