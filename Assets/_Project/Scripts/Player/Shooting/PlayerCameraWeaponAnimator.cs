using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    public class PlayerCameraWeaponAnimator : StatelessPredictedIdentity
    {
        [Header("Dependencies")]
        [SerializeField] private SimplePlayerController _controller;
        [SerializeField] private PlayerCameraWeapon _weapon;
        [SerializeField] private Transform _target;
        [SerializeField] private Transform _focusOrigin;

        [Header("Smoothing")]
        [SerializeField, Min(0f)] private float _positionSmoothSpeed = 12f;
        [SerializeField, Min(0f)] private float _rotationSmoothSpeed = 14f;
        [SerializeField, Min(0f)] private float _movementThreshold = 0.05f;

        [Header("Idle")]
        [SerializeField, Min(0f)] private float _idleBobAmplitude = 0.04f;
        [SerializeField, Min(0f)] private float _idleBobFrequency = 2f;

        [Header("Running")]
        [SerializeField] private Vector2 _runOffset = new(0.12f, 0f);
        [SerializeField] private float _runRotation = 8f;

        [Header("Airborne")]
        [SerializeField] private Vector2 _airborneOffset = new(0.16f, 0.12f);
        [SerializeField] private float _airborneRotation = 14f;

        [Header("Wall Slide")]
        [SerializeField] private Vector2 _wallSlideOffset = new(0.14f, -0.02f);
        [SerializeField] private float _wallSlideRotation = 10f;

        [Header("Focus")]
        [SerializeField] private Vector2 _focusOffset = new(0.18f, 0.12f);
        [SerializeField] private float _focusRotationOffset;
        [SerializeField, Min(0f)] private float _focusLeanMultiplier = 1f;
        [SerializeField] private float _focusVerticalLeanMultiplier = 0.5f;
        [SerializeField, Min(0f)] private float _focusMaxLeanRotation = 12f;
        [SerializeField, Min(0f)] private float _focusSwayAmplitude = 3f;
        [SerializeField, Min(0f)] private float _focusSwayFrequency = 3f;

        private Vector3 _baseLocalPosition;
        private float _baseLocalRotationZ;
        private float _focusFacingSign = 1f;

        private void Awake()
        {
            _controller ??= GetComponentInParent<SimplePlayerController>();
            _weapon ??= GetComponentInParent<PlayerCameraWeapon>();
            _target ??= transform;

            _baseLocalPosition = _target.localPosition;
            _baseLocalRotationZ = _target.localEulerAngles.z;
        }

        protected override void LateUpdateView()
        {
            if (_controller == null || _target == null)
                return;

            SimplePlayerController.PlayerState state = _controller.viewState;
            PlayerCameraWeapon.WeaponState weaponState = _weapon.viewState;
            GetTargetPose(state, out Vector3 targetPosition, out float targetRotationZ);

            if (TryGetFocusPose(weaponState, out Vector3 focusPosition, out float focusRotationZ))
            {
                targetPosition = focusPosition;
                targetRotationZ = focusRotationZ;
            }

            float positionT = GetSmoothingT(_positionSmoothSpeed);
            float rotationT = GetSmoothingT(_rotationSmoothSpeed);

            _target.localPosition = Vector3.Lerp(_target.localPosition, targetPosition, positionT);

            Vector3 euler = _target.localEulerAngles;
            euler.z = Mathf.LerpAngle(euler.z, targetRotationZ, rotationT);
            _target.localEulerAngles = euler;
        }

        private void GetTargetPose(
            SimplePlayerController.PlayerState state,
            out Vector3 targetPosition,
            out float targetRotationZ)
        {
            Vector2 velocity = state.Velocity;

            if (state.IsWallSliding)
            {
                GetWallSlidePose(state, out targetPosition, out targetRotationZ);
                return;
            }

            if (!state.IsGrounded)
            {
                GetAirbornePose(velocity, out targetPosition, out targetRotationZ);
                return;
            }

            if (Mathf.Abs(velocity.x) >= _movementThreshold)
            {
                GetRunPose(velocity.x, out targetPosition, out targetRotationZ);
                return;
            }

            GetIdlePose(out targetPosition, out targetRotationZ);
        }

        private void GetIdlePose(out Vector3 targetPosition, out float targetRotationZ)
        {
            float bob = Mathf.Sin(Time.time * Mathf.PI * 2f * _idleBobFrequency) * _idleBobAmplitude;
            targetPosition = _baseLocalPosition + Vector3.up * bob;
            targetRotationZ = _baseLocalRotationZ;
        }

        private void GetRunPose(float velocityX, out Vector3 targetPosition, out float targetRotationZ)
        {
            float direction = Mathf.Sign(velocityX);
            Vector2 offset = new(_runOffset.x * direction, _runOffset.y);

            targetPosition = _baseLocalPosition + (Vector3)offset;
            targetRotationZ = _baseLocalRotationZ + _runRotation * direction;
        }

        private void GetAirbornePose(Vector2 velocity, out Vector3 targetPosition, out float targetRotationZ)
        {
            Vector2 direction = velocity.sqrMagnitude > _movementThreshold * _movementThreshold
                ? velocity.normalized
                : Vector2.down;

            Vector2 offset = new(direction.x * _airborneOffset.x, direction.y * _airborneOffset.y);

            targetPosition = _baseLocalPosition + (Vector3)offset;
            targetRotationZ = _baseLocalRotationZ + Mathf.Clamp(direction.x + direction.y * 0.35f, -1f, 1f) * _airborneRotation;
        }

        private void GetWallSlidePose(
            SimplePlayerController.PlayerState state,
            out Vector3 targetPosition,
            out float targetRotationZ)
        {
            Vector2 wallNormal = GetWallNormal(state);

            if (wallNormal == Vector2.zero)
                wallNormal = state.Velocity.x < 0f ? Vector2.right : Vector2.left;

            Vector2 offset = wallNormal * _wallSlideOffset.x + Vector2.up * _wallSlideOffset.y;

            targetPosition = _baseLocalPosition + (Vector3)offset;
            targetRotationZ = _baseLocalRotationZ - wallNormal.x * _wallSlideRotation;
        }

        private static Vector2 GetWallNormal(SimplePlayerController.PlayerState state)
        {
            if (state.IsCollidingLeft && state.LeftWallNormal != Vector2.zero)
                return state.LeftWallNormal.normalized;

            if (state.IsCollidingRight && state.RightWallNormal != Vector2.zero)
                return state.RightWallNormal.normalized;

            return Vector2.zero;
        }

        private bool TryGetFocusPose(
            PlayerCameraWeapon.WeaponState weaponState,
            out Vector3 position,
            out float rotationZ)
        {
            position = default;
            rotationZ = 0f;

            if (_weapon == null)
                return false;

            if (weaponState.State == PlayerCameraWeapon.FocusState.None || weaponState.Direction == Vector2.zero)
                return false;

            Vector2 direction = weaponState.Direction.normalized;
            Vector3 originPosition = GetFocusOriginLocalPosition();
            Vector2 offset = new(direction.x * _focusOffset.x, direction.y * _focusOffset.y);

            if (Mathf.Abs(direction.x) >= 0.01f)
                _focusFacingSign = Mathf.Sign(direction.x);

            float lean = Mathf.Clamp(
                (direction.x + direction.y * _focusVerticalLeanMultiplier * _focusFacingSign) * _focusMaxLeanRotation * _focusLeanMultiplier,
                -_focusMaxLeanRotation,
                _focusMaxLeanRotation);
            float sway = Mathf.Sin(Time.time * Mathf.PI * 2f * _focusSwayFrequency) * _focusSwayAmplitude;

            position = originPosition + (Vector3)offset;
            rotationZ = _baseLocalRotationZ + _focusRotationOffset + lean + sway;
            return true;
        }

        private Vector3 GetFocusOriginLocalPosition()
        {
            if (_focusOrigin == null)
                return _baseLocalPosition;

            Transform parent = _target.parent;
            return parent == null
                ? _focusOrigin.position
                : parent.InverseTransformPoint(_focusOrigin.position);
        }

        private static float GetSmoothingT(float speed)
        {
            if (speed <= 0f)
                return 1f;

            return 1f - Mathf.Exp(-speed * Time.deltaTime);
        }

    }
}
