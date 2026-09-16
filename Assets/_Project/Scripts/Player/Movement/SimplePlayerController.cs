using Game.Core;
using Game.Player.Movement;
using Game.Services;
using Game.Utilities;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PredictedRigidbody2D))]
    public class SimplePlayerController : PredictedIdentity<
        SimplePlayerController.PlayerInput,
        SimplePlayerController.PlayerState>,
        IRespawnable
    {
        public struct PlayerInput : IPredictedData<PlayerInput>
        {
            public Vector2 Move;
            public bool JumpPressed;
            public bool JumpHeld;
            public bool JumpReleased;

            public void Dispose() { }
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
            public bool IsCollidingLeft;
            public bool IsCollidingRight;
            public bool SkipGroundProbe;

            public Vector2 LeftWallNormal;
            public Vector2 RightWallNormal;

            public bool Jumped;
            public MovementState MovementState;

            public bool IsWallSliding => MovementState == MovementState.WallSliding;
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
        }

        [Header("Dependencies")]
        [SerializeField] private Transform _origin;
        [SerializeField] private PredictedRigidbody2D _predictedBody;
        [SerializeField] private Rigidbody2D _rigidbody;
        [SerializeField] private PredictedTransform _predictedTransform;
        [SerializeField] private Collider2D _collider;

        [Header("Ground Settings")]
        [SerializeField] private float _groundSpeed = 6f;
        [SerializeField] private float _groundAcceleration = 64f;
        [SerializeField] private float _groundFriction = 12f;
        [SerializeField] private float _stopSpeed = 0.02f;

        [Header("Air Settings")]
        [SerializeField] private float _airSpeed = 6f;
        [SerializeField] private float _airAcceleration = 12f;
        [SerializeField] private float _maxFallSpeed = -12f;

        [Header("Jump Settings")]
        [SerializeField] private float _jumpHeight = 3.6f;
        [SerializeField] private float _gravity = -22f;
        [SerializeField] private float _jumpBuffer = 0.16f;
        [SerializeField] private float _coyoteTime = 0.12f;

        [Header("Wall Jump Settings")]
        [SerializeField] private float _wallJumpHeight = 2f;
        [SerializeField] private float _wallJumpNormalForce = 6f;
        [SerializeField] private float _wallJumpAccelerationMultiplier = 0.4f;
        [SerializeField] private float _wallJumpAccelerationTimer = 0.8f;

        [Header("Wall Slide Settings")]
        [SerializeField] private float _wallSlideGravityMultiplier = 1.2f;
        [SerializeField] private float _wallSlideSpeed = -3.4f;
        [SerializeField] private float _maxWallSlideSpeed = -8f;
        [SerializeField] private float _wallStickForce = 0.2f;
        [SerializeField] private float _wallStickTimer = 0.4f;

        [Header("Collision Checks")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField, Min(0f)] private float _groundProbeDistance = 0.05f;
        [SerializeField, Min(0f)] private float _wallProbeDistance = 0.026f;
        [SerializeField] private bool _disableRigidbodyGravity = true;
        [SerializeField] private bool _freezeRotation = true;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

        private IInputService _inputService;
        private PlayerHealth _health;
        private PlayerLifeManager _lifeManager;
        private ContactFilter2D _collisionFilter;
        private Vector2 _positionBeforePhysics;

        public Transform Origin => _origin;

        private float AbsoluteGravity => Mathf.Abs(_gravity);

        private void Awake()
        {
            ResolveDependencies();
            _health = GetComponentInParent<PlayerReferences>()?.PlayerHealth;
            RebuildCollisionFilter();
            ConfigureRigidbody();
        }

        public override void OnPreSetup()
        {
            base.OnPreSetup();
            _inputService = ServiceLocator.Get<IInputService>();
            _lifeManager = ServiceLocator.Get<PlayerLifeManager>();
            ConfigureRigidbody();
        }

        protected override PlayerState GetInitialState()
        {
            bool isGrounded = CheckGrounded();

            return new PlayerState
            {
                Velocity = _predictedBody != null ? _predictedBody.velocity : Vector2.zero,
                IsGrounded = isGrounded,
                MovementState = isGrounded ? MovementState.Grounded : MovementState.Airborne
            };
        }

        protected override void GetUnityState(ref PlayerState state)
        {
            if (_predictedBody != null)
                state.Velocity = _predictedBody.velocity;
        }

        protected override void SetUnityState(PlayerState state)
        {
            if (_predictedBody != null)
                _predictedBody.velocity = state.Velocity;
        }

        protected override void SimulationStart()
        {
            ref PlayerState state = ref currentState;
            SyncCollisionState(ref state);
            state.MovementState = state.IsGrounded ? MovementState.Grounded : MovementState.Airborne;
        }

        protected override void UpdateInput(ref PlayerInput input)
        {
            if (IsGameplayLocked())
            {
                input = default;
                return;
            }

            input.JumpPressed |= _inputService.Jump.Pressed;
            input.JumpReleased |= _inputService.Jump.Released;
        }

        protected override void GetFinalInput(ref PlayerInput input)
        {
            if (IsGameplayLocked())
            {
                input = default;
                return;
            }

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
            if (IsGameplayLocked())
            {
                StopMovement(ref state);
                return;
            }

            SyncCollisionState(ref state);

            state.TickTimers(delta);
            state.WasGrounded = state.IsGrounded;

            if (state.IsGrounded)
                state.Jumped = false;

            if (input.JumpPressed)
                state.JumpBufferTimer = _jumpBuffer;

            TryTransition(in input, ref state);
            TickMovement(in input, ref state, delta);

            if (_predictedBody != null)
            {
                _positionBeforePhysics = _predictedBody.position;
                _predictedBody.velocity = state.Velocity;
            }
        }

        protected override void LateSimulate(PlayerInput input, ref PlayerState state, float delta)
        {
            if (IsGameplayLocked())
            {
                StopMovement(ref state);
                return;
            }

            if (_predictedBody != null)
            {
                Vector2 afterMove = _predictedBody.position;
                state.Velocity = delta > 0f ? (afterMove - _positionBeforePhysics) / delta : Vector2.zero;
                _predictedBody.velocity = state.Velocity;
            }

            bool wasGrounded = state.IsGrounded;
            SyncCollisionState(ref state);

            if (!state.Jumped && wasGrounded && !state.IsGrounded)
                state.CoyoteTimer = _coyoteTime;

            if (state.IsGrounded)
                state.CoyoteTimer = 0f;
        }

        protected override PlayerState Interpolate(PlayerState from, PlayerState to, float t)
        {
            var state = to;
            state.Velocity = Vector2.Lerp(from.Velocity, to.Velocity, t);

            return state;
        }

        private void TryTransition(in PlayerInput input, ref PlayerState state)
        {
            switch (state.MovementState)
            {
                case MovementState.Grounded:
                    TryTransitionGrounded(ref state);
                    break;
                case MovementState.Airborne:
                    TryTransitionAirborne(ref state, input.Move.x);
                    break;
                case MovementState.WallSliding:
                    TryTransitionWallSliding(in input, ref state);
                    break;
            }
        }

        private void TryTransitionGrounded(ref PlayerState state)
        {
            if (!state.IsGrounded)
            {
                state.MovementState = MovementState.Airborne;
                return;
            }

            if (state.BufferedJumpAvailable)
            {
                PerformGroundJump(ref state);
                state.MovementState = MovementState.Airborne;
            }
        }

        private void TryTransitionAirborne(ref PlayerState state, float moveInput)
        {
            if (state.IsGrounded)
            {
                state.WallJumpTimer = 0f;
                state.MovementState = MovementState.Grounded;
                return;
            }

            if (state.BufferedJumpAvailable && state.CoyoteJumpAvailable)
            {
                PerformGroundJump(ref state);
                return;
            }

            if (CanWallSlide(moveInput, in state))
                state.MovementState = MovementState.WallSliding;
        }

        private void TryTransitionWallSliding(in PlayerInput input, ref PlayerState state)
        {
            if (state.IsGrounded)
            {
                state.MovementState = MovementState.Grounded;
                return;
            }

            if (!TryGetWallNormal(in state, out Vector2 wallNormal))
            {
                state.MovementState = MovementState.Airborne;
                return;
            }

            if (state.BufferedJumpAvailable || input.JumpPressed)
            {
                state.WallJumpTimer = _wallJumpAccelerationTimer;
                PerformWallJump(wallNormal, ref state);
                state.MovementState = MovementState.Airborne;
            }
        }

        private void TickMovement(in PlayerInput input, ref PlayerState state, float delta)
        {
            switch (state.MovementState)
            {
                case MovementState.Grounded:
                    ApplyGroundMovement(input.Move.x, ref state, delta);
                    break;
                case MovementState.Airborne:
                    TickAirborne(in input, ref state, delta);
                    break;
                case MovementState.WallSliding:
                    TickWallSliding(in input, ref state, delta);
                    break;
            }
        }

        private void TickAirborne(in PlayerInput input, ref PlayerState state, float delta)
        {
            float multiplier = 1f;

            if (state.WallJumpTimerTicking &&
                Mathf.Sign(input.Move.x) != Mathf.Sign(state.Velocity.x))
            {
                float t = Mathf.Clamp01(state.WallJumpTimer / _wallJumpAccelerationTimer);
                multiplier = Mathf.Lerp(1f, _wallJumpAccelerationMultiplier, t);
            }

            ApplyGravity(ref state, delta, 1f, _maxFallSpeed);
            ApplyAirMovement(input.Move.x, ref state, delta, multiplier);
            ApplyJumpCut(input.JumpReleased, ref state);
        }

        private void TickWallSliding(in PlayerInput input, ref PlayerState state, float delta)
        {
            if (!TryGetWallNormal(in state, out Vector2 wallNormal))
                return;

            float moveMultiplier = 0f;

            if (Mathf.Abs(input.Move.x) > 0.2f)
            {
                float inputDotNormal = Vector2.Dot(wallNormal, new Vector2(input.Move.x, 0f));

                if (inputDotNormal < 0.5f)
                    state.WallStickTimer = _wallStickTimer;
            }
            else
            {
                state.WallStickTimer = _wallStickTimer;
            }

            if (!state.WallStickTimerAvailable)
                moveMultiplier = 1f;

            float wallSlideSpeed = input.Move.y < -0.2f ? _maxWallSlideSpeed : _wallSlideSpeed;

            ApplyGravity(ref state, delta, _wallSlideGravityMultiplier, wallSlideSpeed);
            ApplyAirMovement(input.Move.x * moveMultiplier, ref state, delta, 1f);

            state.Velocity += -wallNormal * (_wallStickForce * delta);
            state.Velocity.y = Mathf.Max(state.Velocity.y, _maxFallSpeed);
            ApplyJumpCut(input.JumpReleased, ref state);
        }

        private void PerformGroundJump(ref PlayerState state)
        {
            state.JumpBufferTimer = 0f;
            state.CoyoteTimer = 0f;
            state.Jumped = true;
            state.SkipGroundProbe = true;
            state.Velocity.y = Utils.GetJumpVelocity(_jumpHeight, AbsoluteGravity);
        }

        private void PerformWallJump(Vector2 wallNormal, ref PlayerState state)
        {
            state.JumpBufferTimer = 0f;
            state.CoyoteTimer = 0f;
            state.WallStickTimer = 0f;
            state.Jumped = true;
            state.SkipGroundProbe = true;
            state.IsGrounded = false;

            float jumpDirection = wallNormal.x >= 0f ? 1f : -1f;
            state.Velocity.x = jumpDirection * _wallJumpNormalForce;
            state.Velocity.y = Utils.GetJumpVelocity(_wallJumpHeight, AbsoluteGravity);
        }

        private void ApplyGravity(ref PlayerState state, float delta, float multiplier, float maxFallSpeed)
        {
            state.Velocity.y += _gravity * multiplier * delta;
            state.Velocity.y = Mathf.Max(state.Velocity.y, maxFallSpeed);
        }

        private void ApplyJumpCut(bool jumpReleased, ref PlayerState state)
        {
            if (!state.IsGrounded && jumpReleased && state.Velocity.y > 0f && state.Jumped)
            {
                state.Velocity.y *= 0.5f;
                state.Jumped = false;
            }
        }

        private void ApplyGroundMovement(float moveInput, ref PlayerState state, float delta)
        {
            ApplyFriction(ref state.Velocity, _groundFriction, _stopSpeed, delta);
            ApplyHorizontalMovement(ref state.Velocity, moveInput, _groundSpeed, _groundAcceleration, delta);
        }

        private void ApplyAirMovement(float moveInput, ref PlayerState state, float delta, float accelerationMultiplier)
        {
            ApplyHorizontalMovement(ref state.Velocity, moveInput, _airSpeed, accelerationMultiplier * _airAcceleration, delta);
        }

        private bool CanWallSlide(float moveInput, in PlayerState state)
        {
            if (state.IsGrounded)
                return false;

            float velocitySign = Mathf.Sign(state.Velocity.x);
            float inputSign = Mathf.Sign(moveInput);
            float velocityValue = Mathf.Abs(state.Velocity.x) > 0.2f ? 1f : 0f;
            float inputValue = Mathf.Abs(moveInput) > 0.2f ? 1f : 0f;

            float velocityPriority = velocitySign * velocityValue;
            float inputPriority = inputSign * inputValue;

            if (velocityPriority == 0f && inputPriority == 0f)
                return false;

            if (!TryGetWallNormal(in state, out Vector2 wallNormal))
                return false;

            return Mathf.Approximately(moveInput, 0f) ||
                   Mathf.Sign(moveInput) == -Mathf.Sign(wallNormal.x);
        }

        private bool TryGetWallNormal(in PlayerState state, out Vector2 wallNormal)
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
            if (state.SkipGroundProbe)
            {
                state.IsGrounded = false;
                state.SkipGroundProbe = false;
            }
            else
            {
                state.IsGrounded = CheckGrounded();
            }

            state.IsCollidingLeft = CheckWall(Vector2.left, out Vector2 leftWallNormal);
            state.IsCollidingRight = CheckWall(Vector2.right, out Vector2 rightWallNormal);
            state.LeftWallNormal = state.IsCollidingLeft ? leftWallNormal : Vector2.zero;
            state.RightWallNormal = state.IsCollidingRight ? rightWallNormal : Vector2.zero;
        }

        private bool CheckGrounded()
        {
            return CheckDirection(Vector2.down, _groundProbeDistance, out _);
        }

        private bool CheckWall(Vector2 direction, out Vector2 wallNormal)
        {
            if (CheckDirection(direction, _wallProbeDistance, out RaycastHit2D hit) &&
                Mathf.Abs(hit.normal.x) > 0.01f)
            {
                wallNormal = hit.normal.normalized;
                return true;
            }

            wallNormal = Vector2.zero;
            return false;
        }

        private bool CheckDirection(Vector2 direction, float distance, out RaycastHit2D closestHit)
        {
            closestHit = default;

            if (_collider == null)
                return false;

            int count = _collider.Cast(direction, _collisionFilter, _hits, distance);
            bool found = false;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];

                if (!IsValidHit(hit))
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                closestHit = hit;
                found = true;
            }

            return found;
        }

        private bool IsValidHit(RaycastHit2D hit)
        {
            if (hit.collider == null)
                return false;

            Transform hitTransform = hit.collider.transform;
            return hitTransform != transform && !hitTransform.IsChildOf(transform);
        }

        private void ResolveDependencies()
        {
            if (_origin == null)
                _origin = transform;

            if (_predictedBody == null)
                _predictedBody = GetComponent<PredictedRigidbody2D>();

            if (_rigidbody == null)
                _rigidbody = GetComponent<Rigidbody2D>();

            if (_collider == null)
                _collider = GetComponent<Collider2D>();

            if (_predictedTransform == null)
                _predictedTransform = GetComponent<PredictedTransform>();
        }

        private void ConfigureRigidbody()
        {
            if (_rigidbody == null)
                return;

            if (_disableRigidbodyGravity)
                _rigidbody.gravityScale = 0f;

            if (_freezeRotation)
                _rigidbody.constraints |= RigidbodyConstraints2D.FreezeRotation;
        }

        private void RebuildCollisionFilter()
        {
            _collisionFilter = new ContactFilter2D();
            _collisionFilter.SetLayerMask(_collisionMask);
            _collisionFilter.useTriggers = false;
        }

        private static void ApplyHorizontalMovement(
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

        public void Sim_Knokback(
            Vector2 direction, 
            float directionalForce, 
            float verticalForce)
        {
            if (IsGameplayLocked())
                return;

            ////Debug.Log("Applied Knockback!");
            //_predictedBody.AddForce(direction * directionalForce, ForceMode2D.Impulse);
            //_predictedBody.AddForce(Vector2.up * Utils.GetJumpVelocity(verticalForce, AbsoluteGravity), ForceMode2D.Impulse);

            Vector2 knockback = 
                (direction * directionalForce) + 
                (Vector2.up * Utils.GetJumpVelocity(verticalForce, AbsoluteGravity));

            currentState.Velocity += knockback;

            // Set to current velocity if experiencing issues with knockback
            _predictedBody.velocity += knockback;
        }

        private bool IsGameplayLocked()
        {
            return (_health && _health.IsDead)
                || (_lifeManager && (_lifeManager.IsChangingMap || _lifeManager.IsRoundFinished));
        }

        private void StopMovement(ref PlayerState state)
        {
            state.Velocity = Vector2.zero;

            if (_predictedBody != null)
                _predictedBody.velocity = Vector2.zero;

            if (_rigidbody != null)
                _rigidbody.linearVelocity = Vector2.zero;
        }

        public void Respawn()
        {
            currentState = GetInitialState();
            ResetInterpolation();
        }

        public void Sim_SetPosition(Vector2 position)
        {
            currentState.Velocity = Vector2.zero;
            _predictedBody.velocity = Vector2.zero;
            _rigidbody.linearVelocity = Vector2.zero;

            _rigidbody.position = position;
            transform.position = position;

            _predictedTransform.ResetInterpolation();
            _predictedBody.ResetInterpolation();
            _predictedTransform.graphics.position = position; // Very crude solution, but it works
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            ResolveDependencies();

            _groundProbeDistance = Mathf.Max(0f, _groundProbeDistance);
            _wallProbeDistance = Mathf.Max(0f, _wallProbeDistance);

            if (Application.isPlaying)
            {
                RebuildCollisionFilter();
                ConfigureRigidbody();
            }
        }
#endif
    }
}
