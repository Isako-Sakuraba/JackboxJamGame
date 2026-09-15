using Game.Core;
using Game.Services;
using PurrNet.Prediction;
using System;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.Serialization;

namespace Game.Player
{
    public class PlayerCameraWeapon : PredictedIdentity<
        PlayerCameraWeapon.WeaponInput,
        PlayerCameraWeapon.WeaponState>,
        IRespawnable
    {
        #region STATE AND INPUT
        public struct WeaponInput : IPredictedData<WeaponInput>
        {
            public bool ShootPressed;
            public bool ShootHeld;
            public bool ShootReleased;

            public Vector2 ShootDirection;

            public void Dispose() { }
        }

        public enum FocusState
        {
            None,
            Focusing,
            Focused,
            Overfocused
        }

        public struct WeaponState : IPredictedData<WeaponState>
        {
            public float FocusTimer;
            public float OverfocusResistanceTimer;
            public bool IsFocusing;
            public Vector2 Direction;

            public bool OverfocusResistanceAvailable => OverfocusResistanceTimer > 0f;
            public bool FocusTimerAvailable => FocusTimer > 0f;

            public FocusState State
            {
                get
                {
                    if (!IsFocusing)
                        return FocusState.None;

                    if (FocusTimerAvailable)
                        return FocusState.Focusing;

                    if (OverfocusResistanceAvailable)
                        return FocusState.Focused;

                    return FocusState.Overfocused;
                }
            }

            public override string ToString()
            {
                return $"FocusTimer: {FocusTimer}\nIsFocusing: {IsFocusing}\nState: {State}";
            }

            public void Dispose() { }
        }
        #endregion

        [Header("Shutter Settings")]
        [SerializeField] private SimplePlayerController _controller;
        [SerializeField] private float _focusTime = 1.2f;
        [SerializeField] private float _overfocusResistanceTime = 1f;
        [SerializeField] private float _overfocusDirectionPenaltyMultiplier = 0.2f;
        [FormerlySerializedAs("_focusAngle")]
        [SerializeField] private Vector2 _focusSpotAngle = new Vector2(120f, 45f);
        [SerializeField] private Vector2 _focusRadius = new Vector2(2f, 6f);
        [SerializeField] private Vector2 _damageRange = new Vector2(6f, 40f);
        [SerializeField] private Vector2 _velocityRange = new Vector2(2f, 12f);
        [SerializeField] private float _overfocusDamage = 30f;
        [SerializeField] private float _directionalKnockbackForce = 6f;
        [SerializeField] private float _verticalKnockbackHeight = 2f;
        [SerializeField] private Transform _shutterOrigin;
        [SerializeField] private LayerMask _hitLayer;
        [SerializeField] private LayerMask _obstacleLayer;
        [SerializeField] private CapsuleCollider2D _selfCollider;

        [Header("Shutter Visuals")]
        [SerializeField] private Light2D _shutterLight;
        [SerializeField] private float _radiusMultiplier = 0.95f;
        [SerializeField] private float _spotAngleMultiplier = 0.95f;

        private IInputService _inputService;

        private Camera _camera;

        [NonSerialized] public PredictedEvent CameraStartedFocus;
        [NonSerialized] public PredictedEvent CameraFocused;
        [NonSerialized] public PredictedEvent CameraOverfocused;
        [NonSerialized] public PredictedEvent CameraShot;

        public float FocusTime => _focusTime;
        public Vector2 FocusSpotAngle => _focusSpotAngle;
        public Vector2 FocusRadius => _focusRadius;
        public float RadiusMultiplier => _radiusMultiplier;
        public float SpotAngleMultiplier => _spotAngleMultiplier;

        private Collider2D[] _colliderCache = new Collider2D[6];
        private ContactFilter2D _hitFilter;

        public override void OnPreSetup()
        {
            base.OnPreSetup();

            _inputService = ServiceLocator.Get<IInputService>();

            RebuildHitFilter();
        }

        protected override void LateAwake()
        {
            base.LateAwake();

            _camera = Camera.main;

            CameraStartedFocus = new PredictedEvent(predictionManager, this);
            CameraFocused = new PredictedEvent(predictionManager, this);
            CameraOverfocused = new PredictedEvent(predictionManager, this);
            CameraShot = new PredictedEvent(predictionManager, this);
        }

        protected override void UpdateInput(ref WeaponInput input)
        {
            input.ShootPressed |= _inputService.Shoot.Pressed;
            input.ShootReleased |= _inputService.Shoot.Released;
        }

        protected override WeaponState GetInitialState()
        {
            return new WeaponState() { FocusTimer = _focusTime };
        }

        protected override void GetFinalInput(ref WeaponInput input)
        {
            input.ShootHeld = _inputService.Shoot.Held;

            Vector2 screenPosition = _inputService.MousePosition;

            Vector2 worldPosition = _camera.ScreenToWorldPoint(
                new Vector3(
                    screenPosition.x, 
                    screenPosition.y, 
                    -_camera.transform.position.z));

            Vector2 origin = _shutterOrigin.position;
            Vector2 direction = (worldPosition - origin).normalized;

            input.ShootDirection = direction;
        }

        protected override void Simulate(WeaponInput input, ref WeaponState state, float delta)
        {
            if (input.ShootPressed)
                StartFocus(ref state);

            if (!state.IsFocusing)
                return;

            UpdateFocusTimers(input, ref state, delta);

            if (input.ShootHeld || input.ShootReleased)
                UpdateDirection(input, ref state, delta);

            if (input.ShootReleased)
                ReleaseShot(ref state);
        }

        private void StartFocus(ref WeaponState state)
        {
            state.OverfocusResistanceTimer = _overfocusResistanceTime;
            state.FocusTimer = _focusTime;
            state.IsFocusing = true;
            CameraStartedFocus.Invoke();
        }

        private void UpdateFocusTimers(WeaponInput input, ref WeaponState state, float delta)
        {
            if (state.FocusTimerAvailable && (input.ShootHeld || input.ShootReleased))
            {
                state.FocusTimer = Mathf.Max(0f, state.FocusTimer - delta);
                if (!state.FocusTimerAvailable)
                    CameraFocused.Invoke();
            }

            if (!state.FocusTimerAvailable && state.OverfocusResistanceAvailable)
            {
                state.OverfocusResistanceTimer = Mathf.Max(0f, state.OverfocusResistanceTimer - delta);
                if (!state.OverfocusResistanceAvailable)
                    CameraOverfocused.Invoke();
            }
        }

        private void UpdateDirection(WeaponInput input, ref WeaponState state, float delta)
        {
            if (state.State == FocusState.Overfocused)
            {
                state.Direction = Vector3.Slerp(
                    state.Direction,
                    input.ShootDirection,
                    delta * _overfocusDirectionPenaltyMultiplier);
                return;
            }

            state.Direction = input.ShootDirection;
        }

        private void ReleaseShot(ref WeaponState state)
        {
            float current = state.FocusTimer;
            float max = _focusTime;
            float t = current / max; // t is 1 means just started focus, t is 0 means finished
            t = Mathf.Clamp01(1 - t);
            float spotAngle = Mathf.Lerp(_focusSpotAngle.x, _focusSpotAngle.y, t);
            float radius = Mathf.Lerp(_focusRadius.x, _focusRadius.y, t);

            Shoot(ref state, radius, spotAngle);

            state.IsFocusing = false;
            state.FocusTimer = _focusTime;
            state.OverfocusResistanceTimer = _overfocusResistanceTime;

            CameraShot.Invoke();
        }

        private void RebuildHitFilter()
        {
            _hitFilter = new ContactFilter2D();
            _hitFilter.SetLayerMask(_hitLayer);
            _hitFilter.useTriggers = false;
        }

        private void Shoot(
            ref WeaponState state,
            float radius,
            float angle)
        {
            int overlaps = Physics2D.OverlapCircle(
                _shutterOrigin.transform.position, 
                radius, 
                _hitFilter, 
                _colliderCache);


            for (int i = 0; i < overlaps; i++)
            {

                Collider2D overlap = _colliderCache[i];
                if (overlap == _selfCollider)
                    continue;

                PlayerReferences targetReferences = overlap.gameObject.GetComponent<PlayerReferences>();
                if (targetReferences == null)
                    continue;


                Vector2 origin = _shutterOrigin.position;
                Vector2 targetPosition = overlap.bounds.center;
                Vector2 directionToTarget = (targetPosition - origin).normalized;

                if (Vector2.Angle(state.Direction, directionToTarget) > angle * 0.5f)
                    continue;


                RaycastHit2D obstacleHit = Physics2D.Linecast(origin, targetPosition, _obstacleLayer);

                if (obstacleHit.collider != null)
                    continue;

                PlayerHealth targetHealth = targetReferences.PlayerHealth;
                SimplePlayerController targetController = targetReferences.SimplePlayerController;

                // Damage calculations
                float damage = state.State switch
                {
                    FocusState.Focusing => Mathf.Lerp(_damageRange.x, _damageRange.y, GetFocusTimeNormalized(state)),
                    FocusState.Focused => _damageRange.y,
                    FocusState.Overfocused => _overfocusDamage,
                    _ => 0f
                };

                // Relative velocity multiplier
                Vector2 myVelocity = _controller.currentState.Velocity;
                Vector2 targetVelocity = targetController.currentState.Velocity;

                Vector2 relativeVelocity = targetVelocity - myVelocity;
                float relativeMagnitude = relativeVelocity.magnitude;
                relativeMagnitude = Mathf.Clamp(relativeMagnitude, _velocityRange.x, _velocityRange.y);

                float relativeVelocityMultiplier = 
                    Mathf.InverseLerp(_velocityRange.x, _velocityRange.y, relativeMagnitude);

                // Applying relative velocity multiplier
                damage *= relativeVelocityMultiplier;

                float directionalKnockback = relativeVelocityMultiplier * _directionalKnockbackForce;
                float verticalKnockbackHeight = relativeVelocityMultiplier * _verticalKnockbackHeight;

                Debug.Log($"Damaged {damage}");
                targetHealth.Sim_Damage(owner.Value, damage);
                targetController.Sim_Knokback(
                    directionToTarget, 
                    directionalKnockback,
                    verticalKnockbackHeight);
            }
        }

        protected override WeaponState Interpolate(WeaponState from, WeaponState to, float t)
        {
            var state = from;
            state.FocusTimer = to.FocusTimer < from.FocusTimer ? Mathf.Lerp(from.FocusTimer, to.FocusTimer, t) : to.FocusTimer;
            state.Direction = Vector3.Slerp(from.Direction, to.Direction, t);
            state.IsFocusing = to.IsFocusing;

            return state;
        }

        public float GetFocusTimeNormalized(WeaponState state)
        {
            return 1f - Mathf.Clamp01(state.FocusTimer / _focusTime);
        }

        public void Respawn()
        {
            currentState = GetInitialState();
            ResetInterpolation();
        }
    }
}
