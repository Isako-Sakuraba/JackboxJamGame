using PurrNet.Prediction;
using UnityEngine;

namespace Game.Environment
{
    [RequireComponent(typeof(Collider2D))]
    public sealed class JumpPad : StatelessPredictedIdentity
    {
        [SerializeField] private Vector2 _direction = Vector2.up;
        [SerializeField, Min(0f)] private float _impulse = 12f;

        public Vector2 Impulse => _direction.sqrMagnitude > 0f
            ? _direction.normalized * _impulse
            : Vector2.zero;

#if UNITY_EDITOR
        private void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        private void OnValidate()
        {
            _impulse = Mathf.Max(0f, _impulse);
        }
#endif
    }
}
