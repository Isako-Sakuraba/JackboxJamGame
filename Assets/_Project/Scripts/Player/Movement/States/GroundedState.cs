namespace Game.Player.Movement.States
{
    public class GroundedState : LocomotionStateBase
    {
        public GroundedState(PlayerController context) : base(context) { }

        public override void TryTransition(
            in PlayerController.PlayerInput input,
            ref PlayerController.PlayerState state,
            float delta,
            IPredictedMachine<MovementState> machine)
        {
            if (!state.IsGrounded)
            {
                state.MovementState = MovementState.Airborne;
                return;
            }

            if (state.BufferedJumpAvailable)
            {
                Context.PerformGroundJump(ref state);
                state.MovementState = MovementState.Airborne;
            }
        }

        public override void Tick(in PlayerController.PlayerInput input, ref PlayerController.PlayerState state, float delta)
        {
            Context.ApplyGroundMovement(input.Move.x, ref state, delta);
        }
    }
}
