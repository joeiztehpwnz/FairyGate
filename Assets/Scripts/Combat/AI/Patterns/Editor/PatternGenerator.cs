using UnityEngine;
using System.Collections.Generic;

#if UNITY_EDITOR
using UnityEditor;

namespace FairyGate.Combat
{
    /// <summary>
    /// Editor script to generate AI pattern definitions programmatically.
    /// This ensures consistency and provides templates for all enemy archetypes.
    ///
    /// Usage: In Unity Editor, go to Tools → FairyGate → Generate AI Patterns
    /// </summary>
    public class PatternGenerator : EditorWindow
    {
        [MenuItem("Tools/FairyGate/Generate AI Patterns")]
        public static void ShowWindow()
        {
            GetWindow<PatternGenerator>("AI Pattern Generator");
        }

        private void OnGUI()
        {
            GUILayout.Label("AI Pattern Generator", EditorStyles.boldLabel);
            GUILayout.Space(10);

            GUILayout.Label("Generate pattern definitions for all enemy archetypes.");
            GUILayout.Label("Patterns will be created in Assets/Data/AI/Patterns/");
            GUILayout.Space(10);

            if (GUILayout.Button("Generate All Patterns"))
            {
                GenerateAllPatterns();
            }

            GUILayout.Space(10);

            GUILayout.Label("Individual Patterns:", EditorStyles.boldLabel);

            if (GUILayout.Button("Generate Guardian Pattern (Defensive Tank)"))
            {
                GenerateGuardianPattern();
            }

            if (GUILayout.Button("Generate Berserker Pattern (Aggressive Rusher)"))
            {
                GenerateBerserkerPattern();
            }

            if (GUILayout.Button("Generate Assassin Pattern (Opportunistic)"))
            {
                GenerateAssassinPattern();
            }

            if (GUILayout.Button("Generate Archer Pattern (Ranged Kiter)"))
            {
                GenerateArcherPattern();
            }

            if (GUILayout.Button("Generate Soldier Pattern (Balanced Fighter)"))
            {
                GenerateSoldierPattern();
            }
        }

        private static void GenerateAllPatterns()
        {
            GenerateGuardianPattern();
            GenerateBerserkerPattern();
            GenerateAssassinPattern();
            GenerateArcherPattern();
            GenerateSoldierPattern();

            EditorUtility.DisplayDialog("Success", "All AI patterns generated successfully!", "OK");
        }

        private static void GenerateGuardianPattern()
        {
            PatternDefinition pattern = CreatePattern(
                "Guardian - Defensive Tank",
                "Guardian",
                "Punishes aggression with defensive counters. Uses Defense after taking hits, " +
                "then Smash when Defense succeeds. Methodical and predictable - rewards patient players.",
                1 // Difficulty tier: Beginner
            );

            // Node 1: Observe
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Observe",
                description = "Watch player and prepare for defensive stance",
                skillToUse = SkillType.Defense,
                requiresCharge = true,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition
                    {
                        type = ConditionType.PlayerInRange,
                        floatValue = 3.0f
                    }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Defensive Stance",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.PlayerCharging, boolValue = true }
                        },
                        priority = 10
                    },
                    new PatternTransition
                    {
                        targetNodeName = "Pressure",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 4.0f }
                        },
                        priority = 5
                    }
                },
                telegraph = new TelegraphData
                {
                    visualType = TelegraphVisual.StanceShift,
                    glowColor = new Color(0.2f, 0.5f, 1.0f), // Blue (defensive)
                    audioClip = "guardian_ready",
                    duration = 0.5f,
                    anticipation = 0.3f
                }
            });

            // Node 2: Defensive Stance
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Defensive Stance",
                description = "Use Defense to block player attack",
                skillToUse = SkillType.Defense,
                requiresCharge = true,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition { type = ConditionType.StaminaAbove, floatValue = 10f }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Punish",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.HitsDealtCount, intValue = 1 }
                        },
                        priority = 10
                    },
                    new PatternTransition
                    {
                        targetNodeName = "Observe",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.HitsTakenCount, intValue = 1 }
                        },
                        priority = 5,
                        resetHitCounters = true
                    }
                },
                telegraph = new TelegraphData
                {
                    visualType = TelegraphVisual.ShieldRaise,
                    glowColor = new Color(0.2f, 0.6f, 1.0f),
                    audioClip = "shield_raise",
                    duration = 0.4f
                }
            });

            // Node 3: Punish
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Punish",
                description = "Smash after successful Defense",
                skillToUse = SkillType.Smash,
                requiresCharge = true,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition { type = ConditionType.StaminaAbove, floatValue = 5f }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Observe",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 1.0f }
                        },
                        resetHitCounters = true
                    }
                },
                telegraph = new TelegraphData
                {
                    visualType = TelegraphVisual.WeaponRaise,
                    glowColor = new Color(1.0f, 0.3f, 0.2f), // Red (offensive)
                    audioClip = "heavy_swing",
                    duration = 0.6f
                }
            });

            // Node 4: Pressure
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Pressure",
                description = "Occasional Attack to maintain pressure",
                skillToUse = SkillType.Attack,
                requiresCharge = false,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition { type = ConditionType.PlayerInRange, floatValue = 2.0f }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Defensive Stance",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.HitsTakenCount, intValue = 3 }
                        },
                        priority = 10,
                        resetHitCounters = true
                    },
                    new PatternTransition
                    {
                        targetNodeName = "Observe",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 0.5f }
                        },
                        priority = 5
                    }
                }
            });

            pattern.startingNodeName = "Observe";

            SavePattern(pattern, "GuardianPattern");
            Debug.Log("[PatternGenerator] Guardian pattern generated successfully!");
        }

        private static void GenerateBerserkerPattern()
        {
            PatternDefinition pattern = CreatePattern(
                "Berserker - Aggressive Rusher",
                "Berserker",
                "Relentless offense with minimal defense. Chains Attacks into Smash frequently. " +
                "Uses Windmill when cornered or low HP. Predictable but dangerous.",
                2 // Difficulty tier: Intermediate
            );

            // Node 1: Aggressive Approach
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Aggressive Approach",
                description = "Rush toward player with Attack",
                skillToUse = SkillType.Attack,
                requiresCharge = false,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition { type = ConditionType.PlayerInRange, floatValue = 2.5f }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Smash Assault",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.RandomChance, floatValue = 0.7f } // 70% chance
                        },
                        priority = 10
                    },
                    new PatternTransition
                    {
                        targetNodeName = "Aggressive Approach",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 0.5f }
                        },
                        priority = 5
                    }
                }
            });

            // Node 2: Smash Assault
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Smash Assault",
                description = "Heavy Smash attack",
                skillToUse = SkillType.Smash,
                requiresCharge = true,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition { type = ConditionType.StaminaAbove, floatValue = 5f }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Desperate Windmill",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.HealthBelow, floatValue = 30f }
                        },
                        priority = 10
                    },
                    new PatternTransition
                    {
                        targetNodeName = "Aggressive Approach",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 0.8f }
                        },
                        priority = 5
                    }
                },
                telegraph = new TelegraphData
                {
                    visualType = TelegraphVisual.WeaponRaise,
                    glowColor = new Color(1.0f, 0.2f, 0.1f), // Bright red
                    audioClip = "berserker_roar",
                    duration = 0.5f
                }
            });

            // Node 3: Desperate Windmill
            pattern.nodes.Add(new PatternNode
            {
                nodeName = "Desperate Windmill",
                description = "AoE Windmill when low HP",
                skillToUse = SkillType.Windmill,
                requiresCharge = true,
                conditions = new List<PatternCondition>
                {
                    new PatternCondition { type = ConditionType.StaminaAbove, floatValue = 4f }
                },
                transitions = new List<PatternTransition>
                {
                    new PatternTransition
                    {
                        targetNodeName = "Aggressive Approach",
                        conditions = new List<PatternCondition>
                        {
                            new PatternCondition { type = ConditionType.TimeElapsed, floatValue = 1.0f }
                        }
                    }
                },
                telegraph = new TelegraphData
                {
                    visualType = TelegraphVisual.GroundEffect,
                    glowColor = new Color(1.0f, 0.5f, 0.1f), // Orange
                    audioClip = "spin_charge",
                    duration = 0.4f
                }
            });

            pattern.startingNodeName = "Aggressive Approach";

            SavePattern(pattern, "BerserkerPattern");
            Debug.Log("[PatternGenerator] Berserker pattern generated successfully!");
        }

        // Helper Methods

        private static PatternDefinition CreatePattern(string name, string archetype, string notes, int tier)
        {
            PatternDefinition pattern = ScriptableObject.CreateInstance<PatternDefinition>();
            pattern.patternName = name;
            pattern.archetypeTag = archetype;
            pattern.designNotes = notes;
            pattern.difficultyTier = tier;
            pattern.enableDebugLogs = false;
            pattern.nodes = new List<PatternNode>();

            return pattern;
        }

        private static void SavePattern(PatternDefinition pattern, string fileName)
        {
            string path = "Assets/Data/AI/Patterns";

            // Create directory if it doesn't exist
            if (!AssetDatabase.IsValidFolder(path))
            {
                string[] folders = path.Split('/');
                string currentPath = folders[0];

                for (int i = 1; i < folders.Length; i++)
                {
                    string nextPath = $"{currentPath}/{folders[i]}";
                    if (!AssetDatabase.IsValidFolder(nextPath))
                    {
                        AssetDatabase.CreateFolder(currentPath, folders[i]);
                    }
                    currentPath = nextPath;
                }
            }

            // Save asset
            string assetPath = $"{path}/{fileName}.asset";
            AssetDatabase.CreateAsset(pattern, assetPath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = pattern;
        }

        private static void GenerateAssassinPattern()
        {
            // TODO: Implement Assassin pattern (Counter-focused, opportunistic)
            Debug.Log("[PatternGenerator] Assassin pattern generation not yet implemented.");
        }

        private static void GenerateArcherPattern()
        {
            // TODO: Implement Archer pattern (Ranged kiter)
            Debug.Log("[PatternGenerator] Archer pattern generation not yet implemented.");
        }

        private static void GenerateSoldierPattern()
        {
            // TODO: Implement Soldier pattern (Balanced fighter)
            Debug.Log("[PatternGenerator] Soldier pattern generation not yet implemented.");
        }
    }
}
#endif
