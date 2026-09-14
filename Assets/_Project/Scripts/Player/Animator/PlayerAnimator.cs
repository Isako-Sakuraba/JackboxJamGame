using UnityEngine;

namespace Game.Player
{
    public class PlayerAnimator : MonoBehaviour
    {
        private static readonly int IsGrounded = Animator.StringToHash(nameof(IsGrounded));
        private static readonly int IsMoving = Animator.StringToHash(nameof(IsMoving));
        private static readonly int IsSliding = Animator.StringToHash(nameof(IsSliding));

        [Header("Dependencies")]
        [SerializeField] private SimplePlayerController _controller;
        [SerializeField] private Animator _animator;
        [SerializeField] private SpriteRenderer _spriteRenderer;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float _movementThreshold = 0.01f;

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

            UpdateSpriteFlip(viewState, velocityX, isMoving);
        }

        private void UpdateSpriteFlip(SimplePlayerController.PlayerState viewState, float velocityX, bool isMoving)
        {
            if (_spriteRenderer == null || !isMoving)
                return;

            bool invert = viewState.IsWallSliding
                ? _invertSlidingFlipping
                : viewState.IsGrounded
                    ? _invertRunningFlipping
                    : _invertFallingFlipping;

            _spriteRenderer.flipX = (velocityX < 0f) ^ invert;
        }
    }
}
