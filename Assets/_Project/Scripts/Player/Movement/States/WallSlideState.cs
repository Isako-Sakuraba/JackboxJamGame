using UnityEngine;

namespace Game.Player.Movement.States
{
    public class WallSlideState : LocomotionStateBase
    {
        public WallSlideState(PlayerController context) : base(context) { }

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

            if (!Context.TryGetWallNormal(in state, out Vector2 wallNormal))
            {
                state.MovementState = MovementState.Airborne;
                return;
            }

            if (state.BufferedJumpAvailable || input.JumpPressed)
            {
                Context.PerformWallJump(wallNormal, ref state);
                state.MovementState = MovementState.Airborne;
            }
        }

        public override void Tick(in PlayerController.PlayerInput input, ref PlayerController.PlayerState state, float delta)
        {
            if (!Context.TryGetWallNormal(in state, out Vector2 wallNormal))
                return;

            Context.ApplyGravity(input.JumpHeld, ref state, delta, Context.WallSlideGravityMultiplier, Context.WallSlideSpeed);
            Context.ApplyAirMovement(input.Move.x, ref state, delta);

            state.Velocity += -wallNormal * (Context.WallStickForce * delta);
            state.Velocity.y = Mathf.Max(state.Velocity.y, Context.MaxFallSpeed);
            Context.ApplyJumpCut(input.JumpReleased, ref state);
        }
    }
}
