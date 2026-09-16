using PurrNet;
using PurrNet.Prediction;
using UnityEngine;

namespace Game.Player
{
    public sealed class TutorialDummy : PredictedIdentity<TutorialDummy.DummyState>
    {
        public enum MovementMode
        {
            Stationary,
            Patrol
        }

        public struct DummyState : IPredictedData<DummyState>
        {
            public Vector2 Origin;
            public float PatrolDirection;

            public void Dispose() { }
        }

        [SerializeField] private PlayerReferences _references;
        [SerializeField] private MovementMode _movementMode;
        [SerializeField, Min(0f)] private float _patrolRange = 3f;
        [SerializeField, Range(0f, 1f)] private float _movementInput = 1f;
        [SerializeField, Min(0f)] private float _returnDeadZone = 0.1f;
        [SerializeField, Min(0.1f)] private float _teleportDistance = 8f;

        protected override DummyState GetInitialState()
        {
            return new DummyState
            {
                Origin = _references.SimplePlayerController.Origin.position,
                PatrolDirection = 1f
            };
        }

        protected override void LateAwake()
        {
            base.LateAwake();
            _references.PlayerHealth.Sim_Died += Sim_OnDied;
        }

        protected override void Destroyed()
        {
            _references.PlayerHealth.Sim_Died -= Sim_OnDied;
            base.Destroyed();
        }

        protected override void Simulate(ref DummyState state, float delta)
        {
            Vector2 position = _references.SimplePlayerController.Origin.position;
            Vector2 offset = position - state.Origin;

            if (offset.sqrMagnitude > _teleportDistance * _teleportDistance)
            {
                _references.SimplePlayerController.Sim_SetPosition(state.Origin);
                return;
            }

            if (_movementMode != MovementMode.Patrol)
                return;

            if (offset.x >= _patrolRange)
                state.PatrolDirection = -1f;
            else if (offset.x <= -_patrolRange)
                state.PatrolDirection = 1f;
        }

        public SimplePlayerController.PlayerInput GetInput()
        {
            Vector2 position = _references.SimplePlayerController.Origin.position;
            float offset = position.x - currentState.Origin.x;
            float direction;

            if (_movementMode == MovementMode.Patrol && Mathf.Abs(offset) <= _patrolRange)
                direction = currentState.PatrolDirection;
            else if (Mathf.Abs(offset) > _returnDeadZone)
                direction = -Mathf.Sign(offset);
            else
                direction = 0f;

            return new SimplePlayerController.PlayerInput
            {
                Move = new Vector2(direction * _movementInput, 0f)
            };
        }

        private void Sim_OnDied(PlayerID from)
        {
            _references.PlayerHealth.Respawn();
            _references.SimplePlayerController.Respawn();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (_references == null)
                _references = GetComponent<PlayerReferences>();

            _patrolRange = Mathf.Max(0f, _patrolRange);
            _returnDeadZone = Mathf.Max(0f, _returnDeadZone);
            _teleportDistance = Mathf.Max(0.1f, _teleportDistance);
        }
#endif
    }
}
