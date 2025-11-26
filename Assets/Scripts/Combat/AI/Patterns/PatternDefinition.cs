using UnityEngine;
using System.Collections.Generic;

namespace FairyGate.Combat
{
    /// <summary>
    /// ScriptableObject that defines a complete AI behavior pattern.
    /// Each enemy archetype has one or more patterns that define their combat behavior.
    ///
    /// Classic Mabinogi Design: Patterns are consistent and learnable, rewarding observation
    /// and prediction over reflexes.
    /// </summary>
    [CreateAssetMenu(fileName = "New AI Pattern", menuName = "FairyGate/Combat/AI Pattern", order = 1)]
    public class PatternDefinition : ScriptableObject
    {
        [Header("Pattern Identity")]
        [Tooltip("Display name for this pattern (e.g., 'Bear - Defensive Tank')")]
        public string patternName = "Unnamed Pattern";

        [Tooltip("Enemy archetype this pattern belongs to")]
        public string archetypeTag = "Soldier";

        [Tooltip("Designer notes explaining this pattern's philosophy and counters")]
        [TextArea(3, 6)]
        public string designNotes = "";

        [Header("Pattern Nodes")]
        [Tooltip("All nodes in this pattern (must have at least one)")]
        public List<PatternNode> nodes = new List<PatternNode>();

        [Tooltip("Name of the starting node (must match a node's nodeName)")]
        public string startingNodeName = "Start";

        [Header("Pattern Metadata")]
        [Tooltip("Difficulty tier (1=Beginner, 2=Intermediate, 3=Advanced)")]
        [Range(1, 3)]
        public int difficultyTier = 1;

        [Tooltip("Enable debug logging for this pattern?")]
        public bool enableDebugLogs = false;

        /// <summary>
        /// Finds a node by name. Returns null if not found.
        /// </summary>
        public PatternNode GetNodeByName(string nodeName)
        {
            if (nodes == null || nodes.Count == 0)
            {
                CombatLogger.LogPattern($"[PatternDefinition] '{patternName}' has no nodes defined!", CombatLogger.LogLevel.Warning);
                return null;
            }

            foreach (var node in nodes)
            {
                if (node.nodeName == nodeName)
                    return node;
            }

            if (enableDebugLogs)
            {
                CombatLogger.LogPattern($"[PatternDefinition] Node '{nodeName}' not found in pattern '{patternName}'", CombatLogger.LogLevel.Warning);
            }

            return null;
        }

        /// <summary>
        /// Gets the starting node for this pattern.
        /// </summary>
        public PatternNode GetStartingNode()
        {
            PatternNode startNode = GetNodeByName(startingNodeName);

            if (startNode == null)
            {
                // Fallback: Use first node if starting node not found
                if (nodes != null && nodes.Count > 0)
                {
                    CombatLogger.LogPattern($"[PatternDefinition] Starting node '{startingNodeName}' not found in '{patternName}'. Using first node '{nodes[0].nodeName}' as fallback.", CombatLogger.LogLevel.Warning);
                    return nodes[0];
                }
            }

            return startNode;
        }

        /// <summary>
        /// Validates this pattern definition for common errors.
        /// Called automatically in OnValidate() in the editor.
        /// </summary>
        public bool Validate(out string errorMessage)
        {
            // Check: Must have at least one node
            if (nodes == null || nodes.Count == 0)
            {
                errorMessage = "Pattern has no nodes defined!";
                return false;
            }

            // Check: Starting node must exist
            if (GetNodeByName(startingNodeName) == null)
            {
                errorMessage = $"Starting node '{startingNodeName}' not found!";
                return false;
            }

            // Check: All node names must be unique
            HashSet<string> nodeNames = new HashSet<string>();
            foreach (var node in nodes)
            {
                if (string.IsNullOrEmpty(node.nodeName))
                {
                    errorMessage = "Found node with empty name!";
                    return false;
                }

                if (nodeNames.Contains(node.nodeName))
                {
                    errorMessage = $"Duplicate node name found: '{node.nodeName}'";
                    return false;
                }

                nodeNames.Add(node.nodeName);
            }

            // Check: All transition targets must reference valid nodes
            foreach (var node in nodes)
            {
                if (node.transitions != null)
                {
                    foreach (var transition in node.transitions)
                    {
                        if (!string.IsNullOrEmpty(transition.targetNodeName))
                        {
                            if (GetNodeByName(transition.targetNodeName) == null)
                            {
                                errorMessage = $"Node '{node.nodeName}' has transition to invalid node '{transition.targetNodeName}'";
                                return false;
                            }
                        }
                    }
                }
            }

            // Check: No unreachable nodes (every node must be reachable from start)
            HashSet<string> reachableNodes = new HashSet<string>();
            Queue<string> nodesToCheck = new Queue<string>();
            nodesToCheck.Enqueue(startingNodeName);
            reachableNodes.Add(startingNodeName);

            while (nodesToCheck.Count > 0)
            {
                string currentNodeName = nodesToCheck.Dequeue();
                PatternNode currentNode = GetNodeByName(currentNodeName);

                if (currentNode != null && currentNode.transitions != null)
                {
                    foreach (var transition in currentNode.transitions)
                    {
                        if (!string.IsNullOrEmpty(transition.targetNodeName) &&
                            !reachableNodes.Contains(transition.targetNodeName))
                        {
                            reachableNodes.Add(transition.targetNodeName);
                            nodesToCheck.Enqueue(transition.targetNodeName);
                        }
                    }
                }
            }

            foreach (var node in nodes)
            {
                if (!reachableNodes.Contains(node.nodeName))
                {
                    errorMessage = $"Node '{node.nodeName}' is unreachable from starting node '{startingNodeName}'!";
                    return false;
                }
            }

            errorMessage = "";
            return true;
        }

        private void OnValidate()
        {
            // Validate pattern when edited in inspector
            if (Validate(out string error))
            {
                // Pattern is valid
                if (enableDebugLogs)
                {
                    CombatLogger.LogPattern($"[PatternDefinition] '{patternName}' validated successfully ({nodes?.Count ?? 0} nodes)");
                }
            }
            else
            {
                // Pattern has errors
                CombatLogger.LogPattern($"[PatternDefinition] '{patternName}' validation failed: {error}", CombatLogger.LogLevel.Error, this);
            }
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor helper: Adds a new empty node to this pattern.
        /// </summary>
        [ContextMenu("Add New Node")]
        private void AddNewNode()
        {
            if (nodes == null)
                nodes = new List<PatternNode>();

            int nodeNumber = nodes.Count + 1;
            PatternNode newNode = new PatternNode
            {
                nodeName = $"Node{nodeNumber}",
                description = "Describe this node's behavior...",
                skillToUse = SkillType.Attack,
                requiresCharge = true,
                conditions = new List<PatternCondition>(),
                transitions = new List<PatternTransition>()
            };

            nodes.Add(newNode);
            UnityEditor.EditorUtility.SetDirty(this);
        }

        /// <summary>
        /// Editor helper: Generates pattern visualization graph.
        /// </summary>
        [ContextMenu("Log Pattern Graph")]
        private void LogPatternGraph()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine($"=== Pattern Graph: {patternName} ===");
            sb.AppendLine($"Starting Node: {startingNodeName}");
            sb.AppendLine($"Total Nodes: {nodes?.Count ?? 0}");
            sb.AppendLine();

            if (nodes != null)
            {
                foreach (var node in nodes)
                {
                    sb.AppendLine($"[{node.nodeName}] → Skill: {node.skillToUse}");

                    if (node.conditions != null && node.conditions.Count > 0)
                    {
                        sb.AppendLine($"  Conditions: {node.conditions.Count}");
                        foreach (var condition in node.conditions)
                        {
                            sb.AppendLine($"    - {condition.type}");
                        }
                    }

                    if (node.transitions != null && node.transitions.Count > 0)
                    {
                        sb.AppendLine($"  Transitions:");
                        foreach (var transition in node.transitions)
                        {
                            sb.AppendLine($"    → {transition.targetNodeName} (priority {transition.priority})");
                        }
                    }

                    sb.AppendLine();
                }
            }

            CombatLogger.LogPattern(sb.ToString(), CombatLogger.LogLevel.Info, this);
        }
        #endif
    }
}
