namespace Game.Player.Movement.States
{
    public class AirborneState : LocomotionStateBase
    {
        public AirborneState(PlayerController context) : base(context) { }

        public override void TryTransition(
            in PlayerController.PlayerInput input,
            ref PlayerController.PlayerState state,
            float delta,
            IPredictedMachine<MovementState> machine)
        {
            if (state.IsGrounded)
            {
                state.MovementState = MovementState.Grounded;
                return;
            }

            if (state.BufferedJumpAvailable && state.CoyoteJumpAvailable)
            {
                Context.PerformGroundJump(ref state);
                return;
            }

            if (Context.CanWallSlide(input.Move.x, in state))
                state.MovementState = MovementState.WallSliding;
        }

        public override void Tick(in PlayerController.PlayerInput input, ref PlayerController.PlayerState state, float delta)
        {
            Context.ApplyGravity(input.JumpHeld, ref state, delta, 1f, Context.MaxFallSpeed);
            Context.ApplyAirMovement(input.Move.x, ref state, delta);
            Context.ApplyJumpCut(input.JumpReleased, ref state);
        }
    }
}
