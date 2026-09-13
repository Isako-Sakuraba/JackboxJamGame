using Game.Core;
using Game.Services;
using PurrNet.Prediction;
using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace Game.Player
{
    public class PlayerCameraWeapon : PredictedIdentity<
        PlayerCameraWeapon.WeaponInput,
        PlayerCameraWeapon.WeaponState>
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

        public struct WeaponState : IPredictedData<WeaponState>
        {
            public float FocusTimer;
            public float OverfocusResistanceTimer;
            public bool IsFocusing;
            public Vector2 Direction;

            public bool OverfocusResistanceAvailable => OverfocusResistanceTimer > 0f;
            public bool FocusTimerAvailable => FocusTimer > 0f;

            public override string ToString()
            {
                return $"FocusTimer: {FocusTimer}\nIsFocusing: {IsFocusing}";
            }

            public void Dispose() { }
        }
        #endregion

        [Header("Stutter Settings")]
        [SerializeField] private float _focusTime = 1.2f;
        [SerializeField] private float _overfocusResistanceTime = 1f;
        [SerializeField] private float _overfocusDirectionPenaltyMultiplier = 0.2f;
        [SerializeField] private Vector2 _focusAngle = new Vector2(120f, 45f);
        [SerializeField] private Vector2 _focusRadius = new Vector2(2f, 6f);
        [SerializeField] private Transform _stutterOrigin;

        [Header("Stutter Visuals")]
        [SerializeField] private Light2D _stutterLight;
        [SerializeField] private float _radiusMultiplier = 0.95f;
        [SerializeField] private float _spotAngleMultiplier = 0.95f;

        private IInputService _inputService;

        private Camera _camera;

        public PredictedEvent CameraStartedFocus;
        public PredictedEvent CameraFocused;
        public PredictedEvent CameraOverfocused;
        public PredictedEvent CameraShot;

        public override void OnPreSetup()
        {
            base.OnPreSetup();

            _inputService = ServiceLocator.Get<IInputService>();
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

            Vector2 origin = _stutterOrigin.position;
            Vector2 direction = (worldPosition - origin).normalized;

            input.ShootDirection = direction;
        }

        protected override void Simulate(WeaponInput input, ref WeaponState state, float delta)
        {
            if (state.IsFocusing && !state.FocusTimerAvailable && state.OverfocusResistanceAvailable)
            {
                state.OverfocusResistanceTimer = Mathf.Max(0f, state.OverfocusResistanceTimer - delta);
                if (!state.OverfocusResistanceAvailable)
                    CameraOverfocused.Invoke();
            }

            if (input.ShootPressed)
            {
                state.OverfocusResistanceTimer = _overfocusResistanceTime;
                state.FocusTimer = _focusTime;
                state.IsFocusing = true;
                CameraStartedFocus.Invoke();
            }

            if ((input.ShootHeld || input.ShootReleased) && state.IsFocusing)
            {
                if (state.FocusTimerAvailable)
                {
                    state.FocusTimer = Mathf.Max(0f, state.FocusTimer - delta);
                    if (!state.FocusTimerAvailable)
                        CameraFocused.Invoke();
                }

                if (!state.FocusTimerAvailable && !state.OverfocusResistanceAvailable)
                {
                    state.Direction = Vector3.Slerp(state.Direction, input.ShootDirection, delta * _overfocusDirectionPenaltyMultiplier);
                }
                else
                {
                    state.Direction = input.ShootDirection;
                }
            }

            if (input.ShootReleased && state.IsFocusing)
            {
                float current = state.FocusTimer;
                float max = _focusTime;
                float t = current / max; // t is 1 means just started focus, t is 0 means finished
                t = Mathf.Clamp01(1 - t);
                float radius = Mathf.Lerp(_focusAngle.x, _focusAngle.y, t);
                float distance = Mathf.Lerp(_focusRadius.x, _focusRadius.y, t);

                state.IsFocusing = false;
                state.FocusTimer = _focusTime;
                state.OverfocusResistanceTimer = _overfocusResistanceTime;

                CameraShot.Invoke();
                // Shoot or something
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

        protected override void UpdateView(WeaponState viewState, WeaponState? verified)
        {
            if (!viewState.IsFocusing)
                return;

            float current = viewState.FocusTimer;
            float max = _focusTime;
            float t = current / max; // t is 1 means just started focus, t is 0 means finished
            t = Mathf.Clamp01(1 - t);
            float outerRadius = Mathf.Lerp(_focusRadius.x, _focusRadius.y, t);
            float outerAngle = Mathf.Lerp(_focusAngle.x, _focusAngle.y, t);
            float innerRadius = _radiusMultiplier * outerRadius;
            float innerAngle = _spotAngleMultiplier * outerAngle;
            Vector2 direction = viewState.Direction;

            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg - 90f;

            _stutterLight.transform.rotation = Quaternion.Euler(0f, 0f, angle);

            _stutterLight.pointLightInnerAngle = innerAngle;
            _stutterLight.pointLightOuterAngle = outerAngle;
            _stutterLight.pointLightInnerRadius = innerRadius;
            _stutterLight.pointLightOuterRadius = outerRadius;
        }
    }
}
