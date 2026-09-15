using UnityEngine;

namespace Game.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int IsGrounded = Animator.StringToHash(nameof(IsGrounded));
        private static readonly int IsMoving = Animator.StringToHash(nameof(IsMoving));
        private static readonly int IsSliding = Animator.StringToHash(nameof(IsSliding));
        private static readonly int RunSpeed = Animator.StringToHash(nameof(RunSpeed));

        [Header("Dependencies")]
        [SerializeField] private SimplePlayerController _controller;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _movementThreshold = 0.01f;
        [SerializeField] private Vector2 _speedRange = new(0f, 6f);
        [SerializeField] private Vector2 _multiplierRange = new(1f, 1f);

        [Header("Flipping")]
        [SerializeField] private bool _invertSlidingFlipping;
        [SerializeField] private bool _invertFallingFlipping;
        [SerializeField] private bool _invertRunningFlipping;

        private void Awake()
        {
            _controller ??= GetComponent<SimplePlayerController>();
            _animator ??= GetComponentInChildren<Animator>();
            _spriteRenderer ??= GetComponentInChildren<SpriteRenderer>();
        }

        private void Update()
        {
            if (_controller == null || _animator == null)
                return;

            SimplePlayerController.PlayerState viewState = _controller.viewState;
            float velocityX = viewState.Velocity.x;
            bool isMoving = Mathf.Abs(velocityX) >= _movementThreshold;

            _animator.SetBool(IsGrounded, viewState.IsGrounded);
            _animator.SetBool(IsMoving, isMoving);
            _animator.SetBool(IsSliding, viewState.IsWallSliding);
            _animator.SetFloat(RunSpeed, GetRunSpeedMultiplier(Mathf.Abs(velocityX)));

            UpdateSpriteFlip(viewState, velocityX, isMoving);
        }

        private float GetRunSpeedMultiplier(float speed)
        {
            float t = Mathf.InverseLerp(_speedRange.x, _speedRange.y, speed);
            return Mathf.Lerp(_multiplierRange.x, _multiplierRange.y, t);
        }

        private void UpdateSpriteFlip(SimplePlayerController.PlayerState viewState, float velocityX, bool isMoving)
        {
            if (_spriteRenderer == null)
                return;

            if (viewState.IsWallSliding && TryGetWallFacingLeft(viewState, out bool wallFacingLeft))
            {
                _spriteRenderer.flipX = wallFacingLeft ^ _invertSlidingFlipping;
                return;
            }

            if (!isMoving)
                return;

            bool invert = viewState.IsWallSliding
                ? _invertSlidingFlipping
                : viewState.IsGrounded
                    ? _invertRunningFlipping
                    : _invertFallingFlipping;

            _spriteRenderer.flipX = (velocityX < 0f) ^ invert;
        }

        private static bool TryGetWallFacingLeft(SimplePlayerController.PlayerState viewState, out bool facingLeft)
        {
            if (viewState.IsCollidingLeft && viewState.LeftWallNormal != Vector2.zero)
            {
                facingLeft = viewState.LeftWallNormal.x > 0f;
                return true;
            }

            if (viewState.IsCollidingRight && viewState.RightWallNormal != Vector2.zero)
            {
                facingLeft = viewState.RightWallNormal.x > 0f;
                return true;
            }

            facingLeft = false;
            return false;
        }
    }
}
