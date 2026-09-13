using UnityEngine;

namespace Game.Core
{
    /// <summary>
    /// Stores button state for the current frame.
    /// </summary>
    public readonly struct ButtonState
    {
        /// <summary>
        /// Gets whether the button was pressed this frame.
        /// </summary>
        public bool Pressed { get; }

        /// <summary>
        /// Gets whether the button is currently held.
        /// </summary>
        public bool Held { get; }

        /// <summary>
        /// Gets whether the button was released this frame.
        /// </summary>
        public bool Released { get; }

        /// <summary>
        /// Creates a button state snapshot for the current frame.
        /// </summary>
        /// <param name="pressed">Whether the button was pressed this frame.</param>
        /// <param name="held">Whether the button is currently held.</param>
        /// <param name="released">Whether the button was released this frame.</param>
        public ButtonState(bool pressed, bool held, bool released)
        {
            Pressed = pressed;
            Held = held;
            Released = released;
        }
    }

    public interface IInputService
    {
        public Vector2 Move { get; }
        public ButtonState Jump { get; }
        public ButtonState Dash { get; }
        public ButtonState Shoot { get; }

        public Vector2 MousePosition { get; }
    }
}