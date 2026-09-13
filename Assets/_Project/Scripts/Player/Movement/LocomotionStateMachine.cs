using Game.Player.Movement.States;
using System.Collections.Generic;

namespace Game.Player.Movement
{
    public class LocomotionStateMachine : IPredictedMachine<MovementState>
    {
        private readonly Dictionary<MovementState, IPredictedMachineState<MovementState, PlayerController.PlayerInput, PlayerController.PlayerState>> _states;

        public LocomotionStateMachine(PlayerController context)
        {
            _states = new Dictionary<MovementState, IPredictedMachineState<MovementState, PlayerController.PlayerInput, PlayerController.PlayerState>>
            {
                { MovementState.Grounded, new GroundedState(context) },
                { MovementState.Airborne, new AirborneState(context) },
                { MovementState.WallSliding, new WallSlideState(context) }
            };
        }

        public void TryTransition(in PlayerController.PlayerInput input, ref PlayerController.PlayerState state, float delta)
        {
            _states[state.MovementState].TryTransition(in input, ref state, delta, this);
        }

        public void Tick(in PlayerController.PlayerInput input, ref PlayerController.PlayerState state, float delta)
        {
            _states[state.MovementState].Tick(in input, ref state, delta);
        }

        public void ChangeState(MovementState state) { }
    }
}
