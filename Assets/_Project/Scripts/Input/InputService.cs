using Game.Lifecycle;
using Game.Services;
using UnityEngine;
using UnityEngine.InputSystem;
using Input.Generated;

namespace Game.Core
{
    public class InputService : MonoBehaviour, IInputService, IBootstrapable
    {
        private GameInputActions _actions;

        public Vector2 Move => GetValue<Vector2>(_actions.Gameplay.Move);

        public ButtonState Jump => GetButtonState(_actions.Gameplay.Jump);
        public ButtonState Dash => GetButtonState(_actions.Gameplay.Dash);
        public ButtonState Shoot => GetButtonState(_actions.Gameplay.Shoot);

        public Vector2 MousePosition => GetValue<Vector2>(_actions.Gameplay.MousePosition);

        private void Awake()
        {
            _actions = new GameInputActions();
            _actions.Gameplay.Enable();
        }

        public void Bootstrap()
        {
            ServiceLocator.Register<IInputService>(this);
        }

        public ButtonState GetButtonState(InputAction action)
        {
            return new ButtonState(
                action.WasPressedThisFrame(),
                action.IsPressed(),
                action.WasReleasedThisFrame());
        }

        public T GetValue<T>(InputAction action) where T : struct
        {
            return action.ReadValue<T>();
        }
    }
}