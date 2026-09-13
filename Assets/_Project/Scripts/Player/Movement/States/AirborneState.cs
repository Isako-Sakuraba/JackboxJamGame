using UnityEngine;

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
                state.WallJumpTimer = 0f;
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
            float multiplier = 1f;

            if (state.WallJumpTimerTicking
                && Mathf.Sign(input.Move.x) != Mathf.Sign(state.Velocity.x))
            {
                float t = Mathf.Clamp01(state.WallJumpTimer / Context.WallJumpAccelerationTimer);
                multiplier = Mathf.Lerp(1f, Context.WallJumpAccelerationMultiplier, t);
            }

            Context.ApplyGravity(input.JumpHeld, ref state, delta, 1f, Context.MaxFallSpeed);
            Context.ApplyAirMovement(input.Move.x, ref state, delta, multiplier);
            Context.ApplyJumpCut(input.JumpReleased, ref state);
        }
    }
}
