using UnityEngine;

namespace FairyGate.Combat
{
    /// <summary>
    /// Extension methods for Unity components to reduce code duplication.
    /// </summary>
    public static class ComponentExtensions
    {
        /// <summary>
        /// Gets the component of type T, or adds it if it doesn't exist.
        /// Reduces boilerplate null-checking and component adding code.
        /// </summary>
        public static T GetOrAddComponent<T>(this GameObject gameObject) where T : Component
        {
            T component = gameObject.GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Gets the component of type T, or adds it if it doesn't exist.
        /// Reduces boilerplate null-checking and component adding code.
        /// </summary>
        public static T GetOrAddComponent<T>(this Component component) where T : Component
        {
            return component.gameObject.GetOrAddComponent<T>();
        }

        /// <summary>
        /// Safely gets a component, returning null if the GameObject is null.
        /// </summary>
        public static T SafeGetComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject != null ? gameObject.GetComponent<T>() : null;
        }

        /// <summary>
        /// Safely gets a component, returning null if the Component is null.
        /// </summary>
        public static T SafeGetComponent<T>(this Component component) where T : Component
        {
            return component != null ? component.GetComponent<T>() : null;
        }

        /// <summary>
        /// Checks if a GameObject has a component of type T.
        /// </summary>
        public static bool HasComponent<T>(this GameObject gameObject) where T : Component
        {
            return gameObject != null && gameObject.GetComponent<T>() != null;
        }

        /// <summary>
        /// Checks if a Component has a component of type T on its GameObject.
        /// </summary>
        public static bool HasComponent<T>(this Component component) where T : Component
        {
            return component != null && component.GetComponent<T>() != null;
        }
    }
}