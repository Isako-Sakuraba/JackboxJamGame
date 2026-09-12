using System;
using UnityEngine;

namespace CC2D
{
    [Flags]
    public enum CollisionFlags2D
    {
        None = 0,
        Sides = 1 << 0,
        Above = 1 << 1,
        Below = 1 << 2,
    }

    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class CharacterController2D : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private LayerMask _collisionMask = ~0;
        [SerializeField, Min(0f)] private float _skinWidth = 0.02f;
        [SerializeField, Min(1)] private int _maxSlideIterations = 5;

        [Header("Ground")]
        [SerializeField, Range(0f, 89f)] private float _maxSlopeAngle = 50f;
        [SerializeField, Min(0f)] private float _groundSnapDistance = 0.2f;
        [SerializeField, Min(0f)] private float _groundProbeDistance = 0.03f;

        [Header("Precision")]
        [SerializeField, Min(0f)] private float _minMoveDistance = 0.0001f;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[16];

        private CapsuleCollider2D _capsule;
        private ContactFilter2D _filter;

        public bool IsGrounded { get; private set; }

        public Vector2 GroundNormal { get; private set; } = Vector2.up;

        public CollisionFlags2D CollisionFlags { get; private set; }

        public Vector2 LastDisplacement { get; private set; }

        public Vector2 Position => transform.position;

        private float MinGroundDot =>
            Mathf.Cos(_maxSlopeAngle * Mathf.Deg2Rad);

        private void Awake()
        {
            _capsule = GetComponent<CapsuleCollider2D>();
            RebuildFilter();
        }

        private void RebuildFilter()
        {
            _filter = new ContactFilter2D();
            _filter.SetLayerMask(_collisionMask);
            _filter.useTriggers = false;
        }

        public CollisionFlags2D Move(Vector2 delta)
        {
            Vector2 startPosition = transform.position;
            Vector2 position = startPosition;

            CollisionFlags = CollisionFlags2D.None;

            // Check whether we were effectively standing on something before
            // performing this move. This is important for slope snapping.
            bool wasGrounded =
                IsGrounded ||
                TryGetGround(
                    position,
                    _groundProbeDistance,
                    out _);

            IsGrounded = false;
            GroundNormal = Vector2.up;

            Vector2 remaining = delta;

            for (int i = 0; i < _maxSlideIterations; i++)
            {
                float distance = remaining.magnitude;

                if (distance <= _minMoveDistance)
                    break;

                Vector2 direction = remaining / distance;

                if (!Cast(
                        position,
                        direction,
                        distance + _skinWidth,
                        out RaycastHit2D hit))
                {
                    position += remaining;
                    remaining = Vector2.zero;
                    break;
                }

                // Stop slightly before the surface.
                float travelDistance =
                    Mathf.Clamp(
                        hit.distance - _skinWidth,
                        0f,
                        distance);

                if (travelDistance > 0f)
                    position += direction * travelDistance;

                ClassifyCollision(hit.normal);

                float remainingDistance =
                    distance - travelDistance;

                if (remainingDistance <= _minMoveDistance)
                    break;

                Vector2 unresolved =
                    direction * remainingDistance;

                float intoSurface =
                    Vector2.Dot(unresolved, hit.normal);

                // This should normally be negative. If not, the surface isn't
                // actually blocking our movement.
                if (intoSurface >= 0f)
                {
                    position += unresolved;
                    break;
                }

                // Remove the component pointing into the surface.
                Vector2 slide =
                    unresolved -
                    hit.normal * intoSurface;

                // Don't allow a horizontal movement to magically climb
                // a slope steeper than maxSlopeAngle.
                if (IsSteepSlope(hit.normal) &&
                    unresolved.y <= 0f &&
                    slide.y > 0f)
                {
                    slide = Vector2.zero;
                }

                remaining = slide;
            }

            ResolveGround(
                ref position,
                delta,
                wasGrounded);

            Vector2 actualDelta =
                position - startPosition;

            LastDisplacement = actualDelta;

            Vector3 worldPosition = transform.position;
            worldPosition.x = position.x;
            worldPosition.y = position.y;

            transform.position = worldPosition;

            return CollisionFlags;
        }

        private void ResolveGround(
            ref Vector2 position,
            Vector2 requestedDelta,
            bool wasGrounded)
        {
            // An upward move means the character is intentionally leaving
            // the floor. Do not snap it back down.
            if (requestedDelta.y > 0f)
            {
                IsGrounded = false;
                return;
            }

            // If we were grounded before moving, allow a larger downward
            // search. This is what makes descending slopes smooth.
            if (wasGrounded &&
                TryGetGround(
                    position,
                    _groundSnapDistance,
                    out RaycastHit2D snapHit))
            {
                float snapDistance =
                    Mathf.Max(
                        snapHit.distance - _skinWidth,
                        0f);

                if (snapDistance <= _groundSnapDistance)
                    position += Vector2.down * snapDistance;

                SetGrounded(snapHit.normal);
                return;
            }

            // Otherwise only perform a tiny probe.
            // This detects normal landings without pulling airborne
            // characters toward the floor.
            if (TryGetGround(
                    position,
                    _groundProbeDistance,
                    out RaycastHit2D groundHit))
            {
                SetGrounded(groundHit.normal);
                return;
            }

            IsGrounded = false;
            GroundNormal = Vector2.up;
        }

        private void SetGrounded(Vector2 normal)
        {
            IsGrounded = true;
            GroundNormal = normal;
            CollisionFlags |= CollisionFlags2D.Below;
        }

        private void ClassifyCollision(Vector2 normal)
        {
            if (IsWalkable(normal))
            {
                CollisionFlags |= CollisionFlags2D.Below;
            }
            else if (normal.y < -0.01f)
            {
                CollisionFlags |= CollisionFlags2D.Above;
            }
            else
            {
                CollisionFlags |= CollisionFlags2D.Sides;
            }
        }

        private bool TryGetGround(
            Vector2 position,
            float distance,
            out RaycastHit2D groundHit)
        {
            if (Cast(
                    position,
                    Vector2.down,
                    distance + _skinWidth,
                    out RaycastHit2D hit) &&
                IsWalkable(hit.normal))
            {
                groundHit = hit;
                return true;
            }

            groundHit = default;
            return false;
        }

        private bool IsWalkable(Vector2 normal)
        {
            return normal.y >= MinGroundDot;
        }

        private bool IsSteepSlope(Vector2 normal)
        {
            return normal.y > 0f &&
                   normal.y < MinGroundDot;
        }

        private bool Cast(
            Vector2 position,
            Vector2 direction,
            float distance,
            out RaycastHit2D closestHit)
        {
            Vector3 scale = transform.lossyScale;

            Vector2 size = Vector2.Scale(
                _capsule.size,
                new Vector2(
                    Mathf.Abs(scale.x),
                    Mathf.Abs(scale.y)));

            // Capsule offset expressed in world space.
            Vector2 offset =
                transform.TransformVector(_capsule.offset);

            Vector2 center =
                position + offset;

            float angle =
                transform.eulerAngles.z;

            int count = Physics2D.CapsuleCast(
                center,
                size,
                _capsule.direction,
                angle,
                direction,
                _filter,
                _hits,
                distance);

            closestHit = default;
            float closestDistance = float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];

                if (hit.collider == null)
                    continue;

                // Ignore our own movement collider.
                if (hit.collider == _capsule)
                    continue;

                // Also ignore additional colliders attached below this
                // controller, such as hurtboxes.
                if (hit.collider.transform.IsChildOf(transform))
                    continue;

                if (hit.distance >= closestDistance)
                    continue;

                closestDistance = hit.distance;
                closestHit = hit;
            }

            return closestHit.collider != null;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _skinWidth = Mathf.Max(0f, _skinWidth);
            _groundSnapDistance = Mathf.Max(0f, _groundSnapDistance);
            _groundProbeDistance = Mathf.Max(0f, _groundProbeDistance);
            _minMoveDistance = Mathf.Max(0f, _minMoveDistance);
            _maxSlideIterations = Mathf.Max(1, _maxSlideIterations);

            if (Application.isPlaying)
                RebuildFilter();
        }
#endif
    }
}