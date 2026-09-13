namespace Game.Player.Movement.States
{
    public class LocomotionStateBase : IPredictedMachineState<MovementState, PlayerController.PlayerInput, PlayerController.PlayerState>
    {
        protected readonly PlayerController Context;

        public LocomotionStateBase(PlayerController context)
        {
            Context = context;
        }

        public virtual void Tick(in PlayerController.PlayerInput input, ref PlayerController.PlayerState state, float delta) { }

        public virtual void TryTransition(
            in PlayerController.PlayerInput input,
            ref PlayerController.PlayerState state,
            float delta,
            IPredictedMachine<MovementState> machine) { }
    }
}
