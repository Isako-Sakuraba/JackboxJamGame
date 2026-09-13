using CC2D;
using Game.Core;
using Game.Player.Movement;
using Game.Services;
using Game.Utilities;
using PurrNet.Lobby;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    public class PlayerController : PredictedIdentity<
        PlayerController.PlayerInput,
        PlayerController.PlayerState>
    {
        #region STATE AND INPUT
        public struct PlayerInput : IPredictedData<PlayerInput>
        {
            public Vector2 Move;
            public bool JumpPressed;
            public bool JumpHeld;
            public bool JumpReleased;

            public void Dispose() { }

            public override string ToString()
            {
                return
                    $"Move: {Move}\n" +
                    $"JumpPressed: {JumpPressed}\n" +
                    $"JumpHeld: {JumpHeld}\n" +
                    $"JumpReleased: {JumpReleased}";
            }
        }

        public struct PlayerState : IPredictedData<PlayerState>
        {
            public Vector2 Velocity;

            public float CoyoteTimer;
            public float JumpBufferTimer;
            public float WallJumpTimer;
            public float WallStickTimer;

            public bool WasGrounded;
            public bool IsGrounded;

            public bool IsWallSliding => MovementState == MovementState.WallSliding;
            public bool IsCollidingLeft;
            public bool IsCollidingRight;
            public Vector2 LeftWallNormal;
            public Vector2 RightWallNormal;

            public bool Jumped;
            public MovementState MovementState;

            public bool CoyoteJumpAvailable => CoyoteTimer > 0f;
            public bool BufferedJumpAvailable => JumpBufferTimer > 0f;
            public bool WallJumpTimerTicking => WallJumpTimer > 0f;
            public bool WallStickTimerAvailable => WallStickTimer > 0f;

            public void TickTimers(float delta)
            {
                CoyoteTimer = Mathf.Max(0f, CoyoteTimer - delta);
                JumpBufferTimer = Mathf.Max(0f, JumpBufferTimer - delta);
                WallJumpTimer = Mathf.Max(0f, WallJumpTimer - delta);
                WallStickTimer = Mathf.Max(0f, WallStickTimer - delta);
            }

            public void Dispose() { }

            public override string ToString()
            {
                return
                    $"Velocity: {Velocity}\n" +
                    $"CoyoteTimer: {CoyoteTimer:F2}\n" +
                    $"JumpBufferTimer: {JumpBufferTimer:F2}\n" +
                    $"WasGrounded: {WasGrounded}\n" +
                    $"IsGrounded: {IsGrounded}\n" +
                    $"IsWallSliding: {IsWallSliding}\n" +
                    $"IsCollidingLeft: {IsCollidingLeft}\n" +
                    $"IsCollidingRight: {IsCollidingRight}\n" +
                    $"LeftWallNormal: {LeftWallNormal}\n" +
                    $"RightWallNormal: {RightWallNormal}\n" +
                    $"Jumped: {Jumped}\n" +
                    $"MovementState: {MovementState}";
            }
        }
        #endregion

        [Header("Dependencies")]
        [SerializeField] private CharacterController2D _motor;

        [Header("Debug")]
        [SerializeField] private bool _debug = true;

        [Header("Settings")]
        [Header("Ground Settings")]
        [SerializeField] private float _groundSpeed = 6f;
        [SerializeField] private float _groundAcceleration = 40f;
        [SerializeField] private float _groundFriction = 8f;
        [SerializeField] private float _stopSpeed = 8f;

        [Header("Air Settings")]
        [SerializeField] private float _airSpeed = 4f;
        [SerializeField] private float _airAcceleration = 40f;
        [SerializeField] private float _maxFallSpeed = -10f;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpHeight = 6f;
        [SerializeField] private float _gravity = -18f;
        [SerializeField] private float _jumpBuffer = 0.2f;
        [SerializeField] private float _coyoteTime = 0.2f;

        [Header("Wall Jump Settings")]
        [SerializeField] private float _wallJumpHeight = 2f;
        [SerializeField] private float _wallJumpNormalForce = 1f;
        [SerializeField] private float _wallJumpAccelerationMultiplier = 0.4f;
        [SerializeField] private float _wallJumpAccelerationTimer = 0.8f;

        [Header("Wall Slide Settings")]
        [SerializeField] private float _wallSlideGravityMultiplier = 0.35f;
        [SerializeField] private float _wallSlideSpeed = 4f;
        [SerializeField] private float _wallStickForce = 2f;
        [SerializeField] private float _wallStickTimer = 0.6f;

        [Header("Wall Detection")]
        [SerializeField] private LayerMask _wallDetectionMask = ~0;
        [SerializeField, Min(0f)] private float _wallProbeDistance = 0.08f;
        [SerializeField, Range(1, 5)] private int _wallRayCount = 3;
        [SerializeField, Min(0f)] private float _wallRayVerticalPadding = 0.05f;

        private IInputService _inputService;
        private LocomotionStateMachine _movementMachine;
        private CapsuleCollider2D _capsule;
        private ContactFilter2D _wallFilter;

        private readonly RaycastHit2D[] _wallHits = new RaycastHit2D[8];

        public CharacterController2D Motor => _motor;
        public float AbsoluteGravity => Mathf.Abs(_gravity);
        public float MaxFallSpeed => _maxFallSpeed;
        public float WallSlideGravityMultiplier => _wallSlideGravityMultiplier;
        public float WallStickForce => _wallStickForce;
        public float WallSlideSpeed => _wallSlideSpeed;
        public float WallJumpAccelerationTimer => _wallJumpAccelerationTimer;
        public float WallJumpAccelerationMultiplier => _wallJumpAccelerationMultiplier;
        public float WallStickTimer => _wallStickTimer;

        private void Awake()
        {
            _capsule = _motor.GetComponent<CapsuleCollider2D>();

            RebuildWallFilter();
            _movementMachine = new LocomotionStateMachine(this);
        }

        public override void OnPreSetup()
        {
            base.OnPreSetup();
            _inputService = ServiceLocator.Get<IInputService>();
        }

        protected override PlayerState GetInitialState()
        {
            return new PlayerState
            {
                Velocity = Vector2.zero,
                IsGrounded = _motor != null && _motor.IsGrounded,
                MovementState = _motor != null && _motor.IsGrounded
                    ? MovementState.Grounded
                    : MovementState.Airborne
            };
        }

        protected override void SimulationStart()
        {
            ref PlayerState state = ref currentState;
            SyncCollisionState(ref state);
            state.MovementState = state.IsGrounded ? MovementState.Grounded : MovementState.Airborne;
        }

        protected override void UpdateInput(ref PlayerInput input)
        {
            input.JumpPressed |= _inputService.Jump.Pressed;
            input.JumpReleased |= _inputService.Jump.Released;
        }

        protected override void GetFinalInput(ref PlayerInput input)
        {
            input.Move = _inputService.Move;
            input.JumpHeld = _inputService.Jump.Held;
        }

        protected override void SanitizeInput(ref PlayerInput input)
        {
            input.Move = Vector2.ClampMagnitude(input.Move, 1f);
        }

        protected override void ModifyExtrapolatedInput(ref PlayerInput input)
        {
            input.JumpPressed = false;
            input.JumpReleased = false;
        }

        protected override void Simulate(PlayerInput input, ref PlayerState state, float delta)
        {
            SyncCollisionState(ref state);

            state.TickTimers(delta);
            state.WasGrounded = state.IsGrounded;

            if (state.IsGrounded)
                state.Jumped = false;

            if (input.JumpPressed)
                state.JumpBufferTimer = _jumpBuffer;

            _movementMachine.TryTransition(in input, ref state, delta);
            _movementMachine.Tick(in input, ref state, delta);
        }

        protected override void LateSimulate(PlayerInput input, ref PlayerState state, float delta)
        {
            Vector2 beforeMove = _motor.transform.position;

            _motor.Move(state.Velocity * delta, delta);

            Vector2 afterMove = _motor.transform.position;
            state.Velocity = delta > 0f ? (afterMove - beforeMove) / delta : Vector2.zero;

            bool wasGrounded = state.IsGrounded;
            SyncCollisionState(ref state);

            if (!state.Jumped && wasGrounded && !state.IsGrounded)
                state.CoyoteTimer = _coyoteTime;

            if (state.IsGrounded)
                state.CoyoteTimer = 0f;
        }

        public void PerformGroundJump(ref PlayerState state)
        {
            state.JumpBufferTimer = 0f;
            state.CoyoteTimer = 0f;
            state.Jumped = true;
            state.Velocity.y = Utils.GetJumpVelocity(_jumpHeight, AbsoluteGravity);
            _motor.DetachFromGround();
        }

        public void PerformWallJump(Vector2 wallNormal, ref PlayerState state)
        {
            state.JumpBufferTimer = 0f;
            state.CoyoteTimer = 0f;
            state.WallStickTimer = 0f;
            state.Jumped = true;
            float jumpDirection = wallNormal.x >= 0f ? 1f : -1f;
            state.Velocity.x = jumpDirection * _wallJumpNormalForce;
            state.Velocity.y = Utils.GetJumpVelocity(_wallJumpHeight, AbsoluteGravity);
        }

        public void ApplyGravity(
            bool jumpHeld, 
            ref PlayerState state, 
            float delta, 
            float multiplier,
            float maxFallSpeed)
        {
            state.Velocity.y += _gravity * multiplier * delta;
            state.Velocity.y = Mathf.Max(state.Velocity.y, maxFallSpeed);
        }

        public void ApplyJumpCut(bool jumpReleased, ref PlayerState state)
        {
            if (!state.IsGrounded && jumpReleased && state.Velocity.y > 0f && state.Jumped)
            {
                state.Velocity.y *= 0.5f;
                state.Jumped = false;
            }
        }

        public void ApplyGroundMovement(float moveInput, ref PlayerState state, float delta)
        {
            ApplyFriction(ref state.Velocity, _groundFriction, _stopSpeed, delta);
            ApplyHorizontalMovement(ref state.Velocity, moveInput, _groundSpeed, _groundAcceleration, delta);
        }

        public void ApplyAirMovement(float moveInput, ref PlayerState state, float delta, float accelerationMultiplier)
        {
            ApplyHorizontalMovement(ref state.Velocity, moveInput, _airSpeed, accelerationMultiplier * _airAcceleration, delta);
        }

        public static void ApplyHorizontalMovement(
            ref Vector2 velocity,
            float input,
            float maxSpeed,
            float acceleration,
            float delta)
        {
            if (Mathf.Abs(input) < 0.001f)
                return;

            var wishDir = new Vector2(input, 0f);

            Accelerate(ref velocity, wishDir, maxSpeed, acceleration, delta);
        }

        public bool CanWallSlide(float moveInput, in PlayerState state)
        {
            if (state.IsGrounded)
                return false;

            float velocitySign = Mathf.Sign(state.Velocity.x);
            float inputSign = Mathf.Sign(moveInput);
            float velocityValue = Mathf.Abs(state.Velocity.x) > 0.2f ? 1f : 0f;
            float inputValue = Mathf.Abs(moveInput) > 0.2f ? 1f : 0f;

            float velocityPriority = velocitySign * velocityValue;
            float inputPriority = inputSign * inputValue;

            // If moving vertically near a wall and there is no input
            if (velocityPriority == 0f && inputPriority == 0f)
                return false;

            if (!TryGetWallNormal(in state, out Vector2 wallNormal))
                return false;

            return Mathf.Approximately(moveInput, 0f) || Mathf.Sign(moveInput) == -Mathf.Sign(wallNormal.x);
        }

        public bool TryGetWallNormal(in PlayerState state, out Vector2 wallNormal)
        {
            if (state.IsCollidingLeft)
            {
                wallNormal = state.LeftWallNormal;
                return true;
            }

            if (state.IsCollidingRight)
            {
                wallNormal = state.RightWallNormal;
                return true;
            }

            wallNormal = Vector2.zero;
            return false;
        }

        private void SyncCollisionState(ref PlayerState state)
        {
            bool hasRayLeft = TryProbeWall(Vector2.left, out Vector2 rayLeftNormal);
            bool hasRayRight = TryProbeWall(Vector2.right, out Vector2 rayRightNormal);

            //state.IsGrounded = _motor.IsGrounded;
            //state.IsCollidingLeft = hasRayLeft || _motor.IsCollidingLeft;
            //state.IsCollidingRight = hasRayRight || _motor.IsCollidingRight;
            //state.LeftWallNormal = hasRayLeft ? rayLeftNormal : _motor.LeftWallNormal;
            //state.RightWallNormal = hasRayRight ? rayRightNormal : _motor.RightWallNormal;
            //state.IsWallSliding = state.MovementState == MovementState.WallSliding;

            state.IsGrounded = _motor.IsGrounded;
            state.IsCollidingLeft = hasRayLeft;
            state.IsCollidingRight = hasRayRight;
            state.LeftWallNormal = hasRayLeft ? rayLeftNormal : Vector2.zero;
            state.RightWallNormal = hasRayRight ? rayRightNormal : Vector2.zero;
        }

        private bool TryProbeWall(Vector2 direction, out Vector2 wallNormal)
        {
            wallNormal = Vector2.zero;

            if (_capsule == null)
                return false;

            Bounds bounds = _capsule.bounds;
            float distance = bounds.extents.x + _wallProbeDistance;
            float minY = bounds.min.y + _wallRayVerticalPadding;
            float maxY = bounds.max.y - _wallRayVerticalPadding;

            if (maxY < minY)
            {
                float centerY = bounds.center.y;
                minY = centerY;
                maxY = centerY;
            }

            float closestDistance = float.PositiveInfinity;
            bool hasHit = false;

            for (int i = 0; i < _wallRayCount; i++)
            {
                float t = _wallRayCount == 1 ? 0.5f : i / (_wallRayCount - 1f);
                Vector2 origin = new Vector2(bounds.center.x, Mathf.Lerp(minY, maxY, t));
                int count = Physics2D.Raycast(origin, direction, _wallFilter, _wallHits, distance);

                for (int hitIndex = 0; hitIndex < count; hitIndex++)
                {
                    RaycastHit2D hit = _wallHits[hitIndex];

                    if (!IsValidWallHit(hit))
                        continue;

                    if (hit.distance >= closestDistance)
                        continue;

                    closestDistance = hit.distance;
                    wallNormal = hit.normal.normalized;
                    hasHit = true;
                }
            }

            return hasHit;
        }

        private bool IsValidWallHit(RaycastHit2D hit)
        {
            if (hit.collider == null)
                return false;

            Transform hitTransform = hit.collider.transform;
            if (hitTransform == transform || hitTransform.IsChildOf(transform))
                return false;

            return Mathf.Abs(hit.normal.x) > 0.01f;
        }

        private void RebuildWallFilter()
        {
            _wallFilter = new ContactFilter2D();
            _wallFilter.SetLayerMask(_wallDetectionMask);
            _wallFilter.useTriggers = false;
        }

        private static void Accelerate(
            ref Vector2 velocity, 
            Vector2 wishDir, 
            float wishSpeed,
            float acceleration, 
            float delta)
        {
            float currentSpeed = Vector2.Dot(velocity, wishDir);
            float addSpeed = wishSpeed - currentSpeed;

            if (addSpeed <= 0f)
                return;

            float accelSpeed = acceleration * wishSpeed * delta;
            accelSpeed = Mathf.Min(accelSpeed, addSpeed);

            velocity += wishDir * accelSpeed;
        }

        private static void ApplyFriction(
            ref Vector2 velocity,
            float friction,
            float stopSpeed,
            float delta)
        {
            float speed = velocity.magnitude;

            if (speed < 0.001f)
            {
                velocity = Vector2.zero;
                return;
            }

            float control = Mathf.Max(speed, stopSpeed);
            float drop = control * friction * delta;

            float newSpeed = Mathf.Max(speed - drop, 0f);

            if (newSpeed != speed)
                velocity *= newSpeed / speed;
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (!_debug)
                return;

            CapsuleCollider2D capsule = _capsule != null
                ? _capsule
                : (_motor != null ? _motor.GetComponent<CapsuleCollider2D>() : GetComponent<CapsuleCollider2D>());

            if (capsule == null)
                return;

            Bounds bounds = capsule.bounds;
            float distance = bounds.extents.x + _wallProbeDistance;
            float minY = bounds.min.y + _wallRayVerticalPadding;
            float maxY = bounds.max.y - _wallRayVerticalPadding;

            if (maxY < minY)
            {
                float centerY = bounds.center.y;
                minY = centerY;
                maxY = centerY;
            }

            Gizmos.color = Color.red;
            DrawWallProbeGizmos(bounds.center.x, minY, maxY, distance, Vector2.left);
            DrawWallProbeGizmos(bounds.center.x, minY, maxY, distance, Vector2.right);
        }

        private void DrawWallProbeGizmos(float originX, float minY, float maxY, float distance, Vector2 direction)
        {
            for (int i = 0; i < _wallRayCount; i++)
            {
                float t = _wallRayCount == 1 ? 0.5f : i / (_wallRayCount - 1f);
                Vector3 origin = new Vector3(originX, Mathf.Lerp(minY, maxY, t), transform.position.z);
                Gizmos.DrawLine(origin, origin + (Vector3)(direction * distance));
            }
        }

        private void OnValidate()
        {
            _wallProbeDistance = Mathf.Max(0f, _wallProbeDistance);
            _wallRayCount = Mathf.Clamp(_wallRayCount, 1, 5);
            _wallRayVerticalPadding = Mathf.Max(0f, _wallRayVerticalPadding);

            if (Application.isPlaying)
                RebuildWallFilter();
        }
#endif
    }
}
