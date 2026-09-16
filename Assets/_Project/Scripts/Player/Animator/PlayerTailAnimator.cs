using UnityEngine;

namespace Game.Player
{
    public class PlayerTailAnimator : MonoBehaviour
    {
        [Header("Dependencies")]
        [SerializeField] private SimplePlayerController _controller;
        [SerializeField] private Transform _target;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float _rotationSmoothSpeed = 16f;
        [SerializeField, Min(0f)] private float _movementThreshold = 0.05f;

        [Header("Flipping")]
        [SerializeField] private bool _invertFlip;
        [SerializeField] private bool _invertWallSlideFlip;

        [Header("Idle")]
        [SerializeField, Min(0f)] private float _idleSwayAmplitude = 6f;
        [SerializeField, Min(0f)] private float _idleSwayFrequency = 1.5f;

        [Header("Running")]
        [SerializeField] private Vector2 _runSpeedRange = new(0f, 6f);
        [SerializeField] private Vector2 _runTiltRange = new(4f, 18f);

        [Header("Airborne")]
        [SerializeField] private Vector2 _airTiltRange = new(-35f, 35f);
        [SerializeField, Min(0f)] private float _airSwayAmplitude = 2f;
        [SerializeField, Min(0f)] private float _airSwayFrequency = 2f;

        [Header("Wall Slide")]
        [SerializeField] private float _wallSlideRotationOffset = 24f;
        [SerializeField, Min(0f)] private float _wallSlideSwayAmplitude = 4f;
        [SerializeField, Min(0f)] private float _wallSlideSwayFrequency = 2.5f;

        private Vector3 _baseLocalScale;
        private float _baseLocalRotationZ;
        private float _facingSign = 1f;

        private void Awake()
        {
            _controller ??= GetComponentInParent<SimplePlayerController>();
            _target ??= transform;

            _baseLocalScale = _target.localScale;
            _baseLocalRotationZ = _target.localEulerAngles.z;

            if (!Mathf.Approximately(_baseLocalScale.x, 0f))
                _facingSign = Mathf.Sign(_baseLocalScale.x);
        }

        private void Update()
        {
            if (_controller == null || _target == null)
                return;

            SimplePlayerController.PlayerState state = _controller.viewState;
            float targetRotationZ = GetTargetRotation(state);
            float rotationT = GetSmoothingT(_rotationSmoothSpeed);

            Vector3 euler = _target.localEulerAngles;
            euler.z = Mathf.LerpAngle(euler.z, targetRotationZ, rotationT);
            _target.localEulerAngles = euler;
        }

        private float GetTargetRotation(SimplePlayerController.PlayerState state)
        {
            Vector2 velocity = state.Velocity;

            if (state.IsWallSliding)
                return GetWallSlideRotation(state);

            if (!state.IsGrounded)
                return GetAirborneRotation(velocity);

            if (Mathf.Abs(velocity.x) >= _movementThreshold)
                return GetRunningRotation(velocity.x);

            ApplyMovementFlip(_facingSign);
            return _baseLocalRotationZ + GetSine(_idleSwayFrequency, _idleSwayAmplitude);
        }

        private float GetRunningRotation(float velocityX)
        {
            _facingSign = Mathf.Sign(velocityX);
            ApplyMovementFlip(_facingSign);

            float speed = Mathf.Abs(velocityX);
            float t = Mathf.InverseLerp(_runSpeedRange.x, _runSpeedRange.y, speed);
            float tilt = Mathf.Lerp(_runTiltRange.x, _runTiltRange.y, t) * -_facingSign;

            return _baseLocalRotationZ + tilt;
        }

        private float GetAirborneRotation(Vector2 velocity)
        {
            if (Mathf.Abs(velocity.x) >= _movementThreshold)
                _facingSign = Mathf.Sign(velocity.x);

            ApplyMovementFlip(_facingSign);

            float velocityAngle = velocity.sqrMagnitude > _movementThreshold * _movementThreshold
                ? Vector2.SignedAngle(Vector2.right * _facingSign, velocity.normalized)
                : 0f;
            float tilt = Mathf.Clamp(velocityAngle, _airTiltRange.x, _airTiltRange.y);
            float sway = GetSine(_airSwayFrequency, _airSwayAmplitude);

            return _baseLocalRotationZ + tilt + sway;
        }

        private float GetWallSlideRotation(SimplePlayerController.PlayerState state)
        {
            Vector2 wallNormal = GetWallNormal(state);

            if (wallNormal == Vector2.zero)
                wallNormal = _facingSign < 0f ? Vector2.right : Vector2.left;

            bool wallFacingLeft = wallNormal.x > 0f;
            ApplyFlip(wallFacingLeft, _invertWallSlideFlip);

            float sway = GetSine(_wallSlideSwayFrequency, _wallSlideSwayAmplitude);
            return _baseLocalRotationZ + wallNormal.x * _wallSlideRotationOffset + sway;
        }

        private void ApplyMovementFlip(float direction)
        {
            ApplyFlip(direction < 0f, _invertFlip);
        }

        private void ApplyFlip(bool facingLeft, bool invert)
        {
            Vector3 scale = _target.localScale;
            float sign = (facingLeft ^ invert) ? -1f : 1f;
            scale.x = Mathf.Abs(_baseLocalScale.x) * sign;
            _target.localScale = scale;
        }

        private static Vector2 GetWallNormal(SimplePlayerController.PlayerState state)
        {
            if (state.IsCollidingLeft && state.LeftWallNormal != Vector2.zero)
                return state.LeftWallNormal.normalized;

            if (state.IsCollidingRight && state.RightWallNormal != Vector2.zero)
                return state.RightWallNormal.normalized;

            return Vector2.zero;
        }

        private static float GetSmoothingT(float speed)
        {
            if (speed <= 0f)
                return 1f;

            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }

        private static float GetSine(float frequency, float amplitude)
        {
            return Mathf.Sin(Time.time * Mathf.PI * 2f * frequency) * amplitude;
        }
    }
}
