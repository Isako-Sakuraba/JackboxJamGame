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

    [DisallowMultipleComponent]
    [RequireComponent(typeof(CapsuleCollider2D))]
    public sealed class CharacterController2D : MonoBehaviour
    {
        [Header("Collision")]
        [SerializeField] private LayerMask _collisionMask = ~0;

        [SerializeField, Min(0.0001f)]
        private float _skinWidth = 0.01f;

        [SerializeField, Range(1, 16)]
        private int _maxSlideIterations = 8;

        [Header("Ground")]
        [SerializeField, Range(0f, 89f)]
        private float _maxSlopeAngle = 50f;

        [SerializeField, Min(0f)]
        private float _groundSnapDistance = 0.25f;

        [SerializeField, Min(0f)]
        private float _groundProbeDistance = 0.03f;

        [Header("Precision")]
        [SerializeField, Min(0.000001f)]
        private float _minMoveDistance = 0.0001f;

        private const float MinBlockingDot = 0.0001f;
        private const float SamePlaneDot = 0.999f;

        private readonly RaycastHit2D[] _hits = new RaycastHit2D[32];
        private readonly Vector2[] _planes = new Vector2[8];

        private CapsuleCollider2D _capsule;
        private ContactFilter2D _filter;

        private bool _disableGroundSnappingForNextMove;

        public CapsuleCollider2D Capsule => _capsule;

        public bool IsGrounded { get; private set; }

        public Vector2 GroundNormal { get; private set; } = Vector2.up;

        public bool IsCollidingLeft { get; private set; }
        public bool IsCollidingRight { get; private set; }

        public Vector2 LeftWallNormal { get; private set; }
        public Vector2 RightWallNormal { get; private set; }

        /// <summary>
        /// Actual velocity produced by the latest Move call.
        /// Calculated from actual position difference / delta time.
        /// </summary>
        public Vector2 Velocity { get; private set; }

        public CollisionFlags2D CollisionFlags { get; private set; }

        public Vector2 Position => transform.position;

        public Vector2 Origin => Position + _capsule.offset;

        private float MinGroundDot =>
            Mathf.Cos(_maxSlopeAngle * Mathf.Deg2Rad);

        private void Awake()
        {
            _capsule = GetComponent<CapsuleCollider2D>();

            RebuildFilter();
        }

        /// <summary>
        /// Disables ground snapping for the next Move call.
        /// The flag is automatically cleared afterward.
        ///
        /// Call this when starting a jump.
        /// </summary>
        public void DisableGroundSnapping()
        {
            _disableGroundSnappingForNextMove = true;
        }

        public CollisionFlags2D Move(Vector2 delta)
        {
            return Move(delta, Time.deltaTime);
        }

        public CollisionFlags2D Move(
            Vector2 delta,
            float deltaTime)
        {


            Vector2 startPosition =
                transform.position;

            Vector2 position =
                startPosition;

            bool disableGroundSnapping =
                _disableGroundSnappingForNextMove;

            // One-shot flag.
            _disableGroundSnappingForNextMove =
                false;

            bool wasGrounded =
                IsGrounded;

            // Recover grounded state if we're extremely close to the
            // floor, but don't do this on a jump frame.
            if (!wasGrounded &&
                !disableGroundSnapping &&
                TryGetGround(
                    position,
                    _groundProbeDistance,
                    out _,
                    out _))
            {
                wasGrounded = true;
            }

            CollisionFlags = CollisionFlags2D.None;

            IsGrounded = false;
            GroundNormal = Vector2.up;

            IsCollidingLeft = false;
            IsCollidingRight = false;

            LeftWallNormal = Vector2.zero;
            RightWallNormal = Vector2.zero;

            int planeCount = 0;

            Vector2 remaining =
                delta;

            // ========================================================
            // Collide and slide
            // ========================================================

            for (int iteration = 0;
                 iteration < _maxSlideIterations;
                 iteration++)
            {
                float distance =
                    remaining.magnitude;

                if (distance <= _minMoveDistance)
                    break;

                Vector2 direction =
                    remaining / distance;

                if (!CastMovement(
                        position,
                        direction,
                        distance,
                        out RaycastHit2D hit))
                {
                    position += remaining;
                    remaining = Vector2.zero;

                    break;
                }

                Vector2 normal =
                    hit.normal.normalized;

                float approach =
                    -Vector2.Dot(
                        direction,
                        normal);

                float skinAlongDirection =
                    approach > MinBlockingDot
                        ? _skinWidth / approach
                        : 0f;

                float travelDistance =
                    Mathf.Clamp(
                        hit.distance - skinAlongDirection,
                        0f,
                        distance);

                if (travelDistance > 0f)
                {
                    Vector2 travel =
                        direction * travelDistance;

                    position += travel;
                    remaining -= travel;
                }

                RegisterCollision(normal);

                AddPlane(
                    normal,
                    ref planeCount);

                if (remaining.sqrMagnitude <=
                    _minMoveDistance * _minMoveDistance)
                {
                    break;
                }

                // ====================================================
                // Walkable slope
                // ====================================================

                if (IsWalkable(normal))
                {
                    if (Mathf.Abs(remaining.x) >
                        _minMoveDistance)
                    {
                        // Preserve horizontal distance while converting
                        // it into movement along the slope.
                        //
                        // normal.x * x + normal.y * y = 0
                        //
                        // y = -(normal.x / normal.y) * x

                        float slopeY =
                            -(normal.x / normal.y) *
                            remaining.x;

                        remaining =
                            new Vector2(
                                remaining.x,
                                slopeY);

                        remaining =
                            ClipAgainstPlanes(
                                remaining,
                                planeCount);
                    }
                    else
                    {
                        remaining =
                            Vector2.zero;
                    }

                    continue;
                }

                // ====================================================
                // Wall / ceiling / steep slope
                // ====================================================

                Vector2 beforeClip =
                    remaining;

                remaining =
                    ClipAgainstPlanes(
                        remaining,
                        planeCount);

                // Don't allow steep slopes to turn horizontal/downward
                // movement into upward climbing.
                if (IsSteepSlope(normal) &&
                    beforeClip.y <= 0f &&
                    remaining.y > 0f)
                {
                    remaining =
                        Vector2.zero;
                }
            }

            // ========================================================
            // Ground snapping
            // ========================================================

            ResolveGround(
                ref position,
                wasGrounded,
                disableGroundSnapping);

            // ========================================================
            // Apply result
            // ========================================================

            Vector3 finalPosition =
                transform.position;

            finalPosition.x = position.x;
            finalPosition.y = position.y;

            transform.position =
                finalPosition;

            Vector2 endPosition =
                transform.position;

            Velocity =
                deltaTime > 0f
                    ? (endPosition - startPosition) / deltaTime
                    : Vector2.zero;

            return CollisionFlags;
        }

        private void ResolveGround(
            ref Vector2 position,
            bool wasGrounded,
            bool disableGroundSnapping)
        {
            // An explicit jump disables all ground attachment for this
            // Move call.
            if (disableGroundSnapping)
            {
                IsGrounded = false;
                GroundNormal = Vector2.up;

                CollisionFlags &=
                    ~CollisionFlags2D.Below;

                return;
            }

            // If collide-and-slide already found a walkable floor, we're
            // grounded already.
            if (IsGrounded)
                return;

            // If we were grounded before moving, allow a larger downward
            // snap. This is what keeps the character attached when:
            //
            // - descending a slope
            // - stopping after moving uphill
            // - moving over tiny discontinuities in the floor
            if (wasGrounded &&
                TryGetGround(
                    position,
                    _groundSnapDistance,
                    out RaycastHit2D snapHit,
                    out float snapDistance))
            {
                if (snapDistance > 0f)
                {
                    position +=
                        Vector2.down * snapDistance;
                }

                SetGrounded(
                    snapHit.normal);

                return;
            }

            // When airborne, only use a very small probe.
            // This detects landings without magnetically pulling the
            // character down from a significant distance.
            if (TryGetGround(
                    position,
                    _groundProbeDistance,
                    out RaycastHit2D groundHit,
                    out float groundDistance))
            {
                if (groundDistance > 0f)
                {
                    position +=
                        Vector2.down * groundDistance;
                }

                SetGrounded(
                    groundHit.normal);

                return;
            }

            IsGrounded = false;
            GroundNormal = Vector2.up;
        }

        private bool CastMovement(
            Vector2 position,
            Vector2 direction,
            float distance,
            out RaycastHit2D closestHit)
        {
            int count =
                CapsuleCast(
                    position,
                    direction,
                    distance);

            closestHit =
                default;

            float closestDistance =
                float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit =
                    _hits[i];

                if (!IsValidHit(hit))
                    continue;

                Vector2 normal =
                    hit.normal.normalized;

                float approach =
                    -Vector2.Dot(
                        direction,
                        normal);

                // Ignore surfaces we're moving parallel to or away from.
                //
                // This prevents the ground from repeatedly blocking
                // tangent movement along slopes.
                if (approach <= MinBlockingDot)
                    continue;

                if (hit.distance >=
                    closestDistance)
                {
                    continue;
                }

                closestDistance =
                    hit.distance;

                closestHit =
                    hit;
            }

            return
                closestHit.collider != null;
        }

        private bool TryGetGround(
            Vector2 position,
            float maxSnapDistance,
            out RaycastHit2D closestHit,
            out float snapDistance)
        {
            float skinCastDistance =
                _skinWidth /
                Mathf.Max(
                    MinGroundDot,
                    0.01f);

            float castDistance =
                maxSnapDistance +
                skinCastDistance;

            int count =
                CapsuleCast(
                    position,
                    Vector2.down,
                    castDistance);

            closestHit =
                default;

            snapDistance =
                0f;

            float closestDistance =
                float.PositiveInfinity;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit =
                    _hits[i];

                if (!IsValidHit(hit))
                    continue;

                Vector2 normal =
                    hit.normal.normalized;

                if (!IsWalkable(normal))
                    continue;

                if (hit.distance >=
                    closestDistance)
                {
                    continue;
                }

                float verticalSkinDistance =
                    _skinWidth /
                    Mathf.Max(
                        normal.y,
                        0.01f);

                float candidateSnapDistance =
                    Mathf.Max(
                        hit.distance -
                        verticalSkinDistance,
                        0f);

                if (candidateSnapDistance >
                    maxSnapDistance)
                {
                    continue;
                }

                closestDistance =
                    hit.distance;

                closestHit =
                    hit;

                snapDistance =
                    candidateSnapDistance;
            }

            return
                closestHit.collider != null;
        }

        private int CapsuleCast(
            Vector2 position,
            Vector2 direction,
            float distance)
        {
            Vector3 scale =
                transform.lossyScale;

            Vector2 size =
                Vector2.Scale(
                    _capsule.size,
                    new Vector2(
                        Mathf.Abs(scale.x),
                        Mathf.Abs(scale.y)));

            Vector2 offset =
                transform.TransformVector(
                    _capsule.offset);

            Vector2 center =
                position + offset;

            float angle =
                transform.eulerAngles.z;

            return Physics2D.CapsuleCast(
                center,
                size,
                _capsule.direction,
                angle,
                direction,
                _filter,
                _hits,
                distance);
        }

        private bool IsValidHit(
            RaycastHit2D hit)
        {
            if (hit.collider == null)
                return false;

            Transform hitTransform =
                hit.collider.transform;

            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                return false;
            }

            return true;
        }

        private void RegisterCollision(Vector2 normal)
        {
            if (IsWalkable(normal))
            {
                CollisionFlags |= CollisionFlags2D.Below;

                IsGrounded = true;
                GroundNormal = normal;

                return;
            }

            if (normal.y < -0.01f)
            {
                CollisionFlags |= CollisionFlags2D.Above;

                return;
            }

            CollisionFlags |= CollisionFlags2D.Sides;

            // Collision normal points away from the wall.
            //
            // Left wall:
            //
            // wall | -> normal
            //      |  player
            //
            // normal.x > 0
            if (normal.x > 0.01f)
            {
                IsCollidingLeft = true;
                LeftWallNormal = normal;
            }

            // Right wall:
            //
            // player <- | wall
            //
            // normal.x < 0
            if (normal.x < -0.01f)
            {
                IsCollidingRight = true;
                RightWallNormal = normal;
            }
        }

        private void SetGrounded(
            Vector2 normal)
        {
            IsGrounded = true;

            GroundNormal =
                normal.normalized;

            CollisionFlags |=
                CollisionFlags2D.Below;
        }

        private bool IsWalkable(
            Vector2 normal)
        {
            return
                normal.y >= MinGroundDot;
        }

        private bool IsSteepSlope(
            Vector2 normal)
        {
            return
                normal.y > 0f &&
                normal.y < MinGroundDot;
        }

        private void AddPlane(
            Vector2 normal,
            ref int planeCount)
        {
            for (int i = 0;
                 i < planeCount;
                 i++)
            {
                if (Vector2.Dot(
                        _planes[i],
                        normal) >
                    SamePlaneDot)
                {
                    return;
                }
            }

            if (planeCount >=
                _planes.Length)
            {
                return;
            }

            _planes[planeCount++] =
                normal;
        }

        private Vector2 ClipAgainstPlanes(
            Vector2 displacement,
            int planeCount)
        {
            Vector2 result =
                displacement;

            for (int i = 0;
                 i < planeCount;
                 i++)
            {
                float into =
                    Vector2.Dot(
                        result,
                        _planes[i]);

                if (into < 0f)
                {
                    result -=
                        _planes[i] * into;
                }
            }

            // Clipping against one surface can push us back into another
            // at corners.
            for (int i = 0;
                 i < planeCount;
                 i++)
            {
                if (Vector2.Dot(
                        result,
                        _planes[i]) <
                    -0.0001f)
                {
                    return Vector2.zero;
                }
            }

            return result;
        }

        private void RebuildFilter()
        {
            _filter =
                new ContactFilter2D();

            _filter.SetLayerMask(
                _collisionMask);

            _filter.useTriggers =
                false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            _skinWidth =
                Mathf.Max(
                    0.0001f,
                    _skinWidth);

            _groundSnapDistance =
                Mathf.Max(
                    0f,
                    _groundSnapDistance);

            _groundProbeDistance =
                Mathf.Max(
                    0f,
                    _groundProbeDistance);

            _minMoveDistance =
                Mathf.Max(
                    0.000001f,
                    _minMoveDistance);

            _maxSlideIterations =
                Mathf.Clamp(
                    _maxSlideIterations,
                    1,
                    16);

            if (Application.isPlaying)
                RebuildFilter();
        }
#endif
    }
}