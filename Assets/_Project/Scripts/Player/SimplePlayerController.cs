using Game.Core;
using Game.Services;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    [RequireComponent(typeof(Rigidbody2D))]
    [RequireComponent(typeof(PredictedRigidbody2D))]
    public class SimplePlayerController : PredictedIdentity<
        SimplePlayerController.SimpleInput,
        SimplePlayerController.SimpleState>
    {
        public struct SimpleInput : IPredictedData<SimpleInput>
        {
            public float MoveX;
            public bool JumpPressed;

            public void Dispose() { }
        }

        public struct SimpleState : IPredictedData<SimpleState>
        {
            public void Dispose() { }
        }

        [SerializeField] private PredictedRigidbody2D _body;
        [SerializeField] private float _moveSpeed = 6f;
        [SerializeField] private float _jumpImpulse = 8f;

        private IInputService _inputService;

        private void Awake()
        {
            if (_body == null)
                _body = GetComponent<PredictedRigidbody2D>();
        }

        public override void OnPreSetup()
        {
            base.OnPreSetup();
            _inputService = ServiceLocator.Get<IInputService>();
        }

        protected override void UpdateInput(ref SimpleInput input)
        {
            input.JumpPressed |= _inputService.Jump.Pressed;
        }

        protected override void GetFinalInput(ref SimpleInput input)
        {
            input.MoveX = _inputService.Move.x;
        }

        protected override void SanitizeInput(ref SimpleInput input)
        {
            input.MoveX = Mathf.Clamp(input.MoveX, -1f, 1f);
        }

        protected override void ModifyExtrapolatedInput(ref SimpleInput input)
        {
            input.JumpPressed = false;
        }

        protected override void Simulate(SimpleInput input, ref SimpleState state, float delta)
        {
            Vector2 velocity = _body.linearVelocity;
            velocity.x = input.MoveX * _moveSpeed;
            _body.linearVelocity = velocity;

            if (input.JumpPressed)
                _body.AddForce(Vector2.up * _jumpImpulse, ForceMode2D.Impulse);
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_body == null)
                _body = GetComponent<PredictedRigidbody2D>();
        }
#endif
    }
}
