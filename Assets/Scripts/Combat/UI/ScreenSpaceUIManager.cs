using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Manages screen-space UI layout for character info displays.
    /// Handles stacking of UI elements in separate columns for player and AI characters.
    /// </summary>
    public class ScreenSpaceUIManager : Singleton<ScreenSpaceUIManager>
    {
        [Header("Layout Settings")]
        [SerializeField] private float playerColumnX = 10f;
        [SerializeField] private float aiColumnX = 180f;
        [SerializeField] private float startY = 10f;
        [SerializeField] private float itemSpacing = 2f;
        [SerializeField] private float characterGroupSpacing = 15f;
        [SerializeField] private float nameLabelHeight = 18f;

        [Header("Visual Settings")]
        [SerializeField] private int nameFontSize = 12;
        [SerializeField] private Color nameLabelColor = Color.white;
        [SerializeField] private Color nameBgColor = new Color(0.1f, 0.1f, 0.1f, 0.7f);

        // Track elements per character
        private Dictionary<Transform, List<IScreenSpaceUI>> playerElements = new Dictionary<Transform, List<IScreenSpaceUI>>();
        private Dictionary<Transform, List<IScreenSpaceUI>> aiElements = new Dictionary<Transform, List<IScreenSpaceUI>>();

        // Cached layout positions
        private Dictionary<IScreenSpaceUI, Vector2> cachedPositions = new Dictionary<IScreenSpaceUI, Vector2>();
        private bool layoutDirty = true;

        /// <summary>
        /// Register a UI element for screen-space layout.
        /// </summary>
        public void Register(IScreenSpaceUI element, bool isPlayer)
        {
            var dict = isPlayer ? playerElements : aiElements;

            if (!dict.ContainsKey(element.Owner))
            {
                dict[element.Owner] = new List<IScreenSpaceUI>();
            }

            if (!dict[element.Owner].Contains(element))
            {
                dict[element.Owner].Add(element);
                dict[element.Owner].Sort((a, b) => a.StackOrder.CompareTo(b.StackOrder));
                layoutDirty = true;
            }
        }

        /// <summary>
        /// Unregister a UI element from screen-space layout.
        /// </summary>
        public void Unregister(IScreenSpaceUI element)
        {
            // Try player elements
            foreach (var kvp in playerElements)
            {
                if (kvp.Value.Remove(element))
                {
                    cachedPositions.Remove(element);
                    layoutDirty = true;

                    // Clean up empty character entries
                    if (kvp.Value.Count == 0)
                    {
                        playerElements.Remove(kvp.Key);
                    }
                    return;
                }
            }

            // Try AI elements
            foreach (var kvp in aiElements)
            {
                if (kvp.Value.Remove(element))
                {
                    cachedPositions.Remove(element);
                    layoutDirty = true;

                    if (kvp.Value.Count == 0)
                    {
                        aiElements.Remove(kvp.Key);
                    }
                    return;
                }
            }
        }

        /// <summary>
        /// Get the screen position for a registered UI element.
        /// </summary>
        public Vector2 GetScreenPosition(IScreenSpaceUI element)
        {
            // Recalculate layout every frame to handle visibility changes (like knockdown meter becoming > 0)
            RecalculateLayout();

            if (cachedPositions.TryGetValue(element, out Vector2 pos))
            {
                return pos;
            }

            // Fallback: determine correct column based on which dictionary contains the element
            float columnX = playerColumnX;
            foreach (var kvp in aiElements)
            {
                if (kvp.Value.Contains(element))
                {
                    columnX = aiColumnX;
                    break;
                }
            }

            return new Vector2(columnX, startY);
        }

        /// <summary>
        /// Mark layout as needing recalculation (call when visibility changes).
        /// </summary>
        public void InvalidateLayout()
        {
            layoutDirty = true;
        }

        private void RecalculateLayout()
        {
            cachedPositions.Clear();

            // Layout player column (with name labels)
            float currentY = startY;
            foreach (var kvp in playerElements)
            {
                currentY = LayoutCharacterGroup(kvp.Key, kvp.Value, playerColumnX, currentY, true);
                currentY += characterGroupSpacing;
            }

            // Layout AI column (with name labels)
            currentY = startY;
            foreach (var kvp in aiElements)
            {
                currentY = LayoutCharacterGroup(kvp.Key, kvp.Value, aiColumnX, currentY, true);
                currentY += characterGroupSpacing;
            }

            layoutDirty = false;
        }

        private float LayoutCharacterGroup(Transform owner, List<IScreenSpaceUI> elements, float columnX, float startingY, bool showNameLabel)
        {
            float currentY = startingY;

            // Add space for name label if this is AI
            if (showNameLabel && elements.Count > 0 && elements.Any(e => e.IsVisible))
            {
                currentY += nameLabelHeight + itemSpacing;
            }

            // Layout each visible element
            foreach (var element in elements)
            {
                if (element.IsVisible && element.DisplayMode == UIDisplayMode.OnScreenSide)
                {
                    cachedPositions[element] = new Vector2(columnX, currentY);
                    currentY += element.ElementHeight + itemSpacing;
                }
            }

            return currentY;
        }

        private void OnGUI()
        {
            // Draw player character name labels
            foreach (var kvp in playerElements)
            {
                var visibleElements = kvp.Value.Where(e => e.IsVisible && e.DisplayMode == UIDisplayMode.OnScreenSide).ToList();
                if (visibleElements.Count > 0)
                {
                    DrawNameLabel(visibleElements[0]);
                }
            }

            // Draw AI character name labels
            foreach (var kvp in aiElements)
            {
                var visibleElements = kvp.Value.Where(e => e.IsVisible && e.DisplayMode == UIDisplayMode.OnScreenSide).ToList();
                if (visibleElements.Count > 0)
                {
                    DrawNameLabel(visibleElements[0]);
                }
            }
        }

        private void DrawNameLabel(IScreenSpaceUI element)
        {
            if (!cachedPositions.TryGetValue(element, out Vector2 pos))
                return;

            // Name label is drawn above the first element
            float labelY = pos.y - nameLabelHeight - itemSpacing;

            GUIStyle style = new GUIStyle(GUI.skin.label);
            style.fontSize = nameFontSize;
            style.fontStyle = FontStyle.Bold;
            style.alignment = TextAnchor.MiddleLeft;
            style.normal.textColor = nameLabelColor;

            Rect bgRect = new Rect(pos.x, labelY, 150f, nameLabelHeight);

            Color originalColor = GUI.color;
            GUI.color = nameBgColor;
            GUI.DrawTexture(bgRect, Texture2D.whiteTexture);

            GUI.color = nameLabelColor;
            GUI.Label(bgRect, " " + element.CharacterName, style);

            GUI.color = originalColor;
        }

        /// <summary>
        /// Ensure an instance exists in the scene.
        /// </summary>
        public static void EnsureExists()
        {
            if (Instance == null)
            {
                var go = new GameObject("ScreenSpaceUIManager");
                go.AddComponent<ScreenSpaceUIManager>();
            }
        }
    }
}
