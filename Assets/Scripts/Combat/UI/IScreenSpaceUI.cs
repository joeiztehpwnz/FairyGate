using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Display mode for UI elements - on character (world-space) or on screen side (screen-space).
    /// </summary>
    public enum UIDisplayMode
    {
        OnCharacter,    // World-space, rendered above character
        OnScreenSide    // Screen-space, stacked on left side of screen
    }

    /// <summary>
    /// Interface for UI elements that can be displayed in screen-space stacking layout.
    /// </summary>
    public interface IScreenSpaceUI
    {
        /// <summary>
        /// The character transform this UI element belongs to.
        /// </summary>
        Transform Owner { get; }

        /// <summary>
        /// Height of this element in pixels for stacking calculations.
        /// </summary>
        float ElementHeight { get; }

        /// <summary>
        /// Order within the character's group (lower = higher on screen).
        /// 0 = Health, 1 = Stamina, 2 = Knockdown, 3 = Status Effects
        /// </summary>
        int StackOrder { get; }

        /// <summary>
        /// Name to display for this character's group (used for AI labels).
        /// </summary>
        string CharacterName { get; }

        /// <summary>
        /// Whether this element is currently visible.
        /// </summary>
        bool IsVisible { get; }

        /// <summary>
        /// Current display mode.
        /// </summary>
        UIDisplayMode DisplayMode { get; }
    }
}
