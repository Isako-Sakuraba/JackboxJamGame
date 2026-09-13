using System;
using System.Collections.Generic;
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
        [SerializeField]
        private LayerMask _collisionMask = ~0;

        [SerializeField, Min(0.0001f)]
        private float _skinWidth = 0.01f;

        [SerializeField, Range(1, 16)]
        private int _maxSlideIterations = 8;

        [Header("Ground")]
        [SerializeField, Range(0f, 89f)]
        private float _maxSlopeAngle = 50f;

        [SerializeField, Min(0f)]
        private float _groundProbeDistance = 0.03f;

        [SerializeField, Min(0f)]
        private float _groundSnapDistance = 0.25f;

        [Header("Precision")]
        [SerializeField, Min(0.000001f)]
        private float _minMoveDistance = 0.0001f;

        private const float MinBlockingDot = 0.0001f;
        private const float SamePlaneDot = 0.999f;
        private const float HitTieEpsilon = 0.000001f;

        private readonly List<RaycastHit2D> _hits = new(32);
        private readonly Vector2[] _planes = new Vector2[8];

        private CapsuleCollider2D _capsule;
        private ContactFilter2D _filter;

        private bool _skipGroundProbeForNextMove;

        // Used so SnapToGround() can update Velocity from the complete
        // displacement of the current simulation step.
        private bool _hasMoveFrame;
        private Vector2 _moveStartPosition;
        private float _moveDelta;

        public bool IsGrounded { get; private set; }

        public Vector2 GroundNormal { get; private set; } =
            Vector2.up;

        public bool IsCollidingLeft { get; private set; }

        public bool IsCollidingRight { get; private set; }

        public Vector2 LeftWallNormal { get; private set; }

        public Vector2 RightWallNormal { get; private set; }

        /// <summary>
        /// Actual velocity produced by the current simulation step.
        ///
        /// Calculated from:
        ///
        ///     (currentPosition - positionBeforeMove) / delta
        ///
        /// SnapToGround() also updates this value if it is called
        /// after Move().
        /// </summary>
        public Vector2 Velocity { get; private set; }

        public CollisionFlags2D CollisionFlags { get; private set; }

        public Vector2 Position =>
            transform.position;

        private float MinGroundDot =>
            Mathf.Cos(_maxSlopeAngle * Mathf.Deg2Rad);

        private void Awake()
        {
            _capsule =
                GetComponent<CapsuleCollider2D>();

            RebuildFilter();
        }

        /// <summary>
        /// Explicitly tells the controller that the character intends
        /// to leave the ground.
        ///
        /// This disables the automatic ground probe for the next
        /// Move() only.
        ///
        /// Use this when jumping.
        /// </summary>
        public void DetachFromGround()
        {
            _skipGroundProbeForNextMove = true;

            IsGrounded = false;
            GroundNormal = Vector2.up;

            CollisionFlags &=
                ~CollisionFlags2D.Below;
        }

        /// <summary>
        /// Moves the controller by the supplied world-space displacement.
        ///
        /// delta is the simulation timestep and is used only to calculate
        /// Velocity.
        ///
        /// Ground snapping is NOT performed here.
        /// A non-moving ground probe IS performed after movement.
        /// </summary>
        public CollisionFlags2D Move(
            Vector2 motion,
            float delta)
        {
            if (delta <= 0f)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delta),
                    "Simulation delta must be greater than zero.");
            }

            Vector2 startPosition =
                transform.position;

            Vector2 position =
                startPosition;

            _moveStartPosition =
                startPosition;

            _moveDelta =
                delta;

            _hasMoveFrame =
                true;

            bool skipGroundProbe =
                _skipGroundProbeForNextMove;

            _skipGroundProbeForNextMove =
                false;

            ResetCollisionState();

            int planeCount = 0;

            Vector2 remaining =
                motion;

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

                // skinWidth represents distance perpendicular
                // to the surface, not along the cast direction.
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
                        // Preserve horizontal displacement when
                        // converting movement onto the slope.
                        //
                        // n.x * x + n.y * y = 0
                        //
                        // y = -(n.x / n.y) * x

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

                // Don't allow a steep slope to convert horizontal or
                // downward movement into upward movement.
                if (IsSteepSlope(normal) &&
                    beforeClip.y <= 0f &&
                    remaining.y > 0f)
                {
                    remaining =
                        Vector2.zero;
                }
            }

            // ========================================================
            // Apply movement
            // ========================================================

            SetPosition(position);

            RecalculateVelocity();

            // ========================================================
            // Ground detection
            // ========================================================
            //
            // IMPORTANT:
            //
            // This does NOT move the character.
            //
            // IsGrounded therefore does not depend on whether
            // SnapToGround() was called.

            if (skipGroundProbe)
            {
                IsGrounded = false;
                GroundNormal = Vector2.up;

                CollisionFlags &=
                    ~CollisionFlags2D.Below;
            }
            else if (!IsGrounded)
            {
                ProbeGroundInternal(
                    _groundProbeDistance);
            }

            return CollisionFlags;
        }

        /// <summary>
        /// Checks whether walkable ground is immediately beneath the
        /// controller without moving it.
        /// </summary>
        public bool ProbeGround()
        {
            return ProbeGround(
                _groundProbeDistance);
        }

        /// <summary>
        /// Checks whether walkable ground exists within maxDistance
        /// beneath the controller without moving it.
        /// </summary>
        public bool ProbeGround(
            float maxDistance)
        {
            IsGrounded = false;
            GroundNormal = Vector2.up;

            CollisionFlags &=
                ~CollisionFlags2D.Below;

            return ProbeGroundInternal(
                maxDistance);
        }

        /// <summary>
        /// Explicitly snaps the controller down to nearby walkable ground.
        ///
        /// This is never called automatically by Move().
        /// </summary>
        public bool SnapToGround()
        {
            return SnapToGround(
                _groundSnapDistance);
        }

        /// <summary>
        /// Explicitly snaps the controller down to nearby walkable ground.
        ///
        /// If called after Move(), Velocity is recalculated from the
        /// complete Move + Snap displacement.
        /// </summary>
        public bool SnapToGround(
            float maxDistance)
        {
            if (maxDistance < 0f)
                return false;

            Vector2 position =
                transform.position;

            if (!TryGetGround(
                    position,
                    maxDistance,
                    out RaycastHit2D hit,
                    out float snapDistance))
            {
                return false;
            }

            if (snapDistance > 0f)
            {
                position +=
                    Vector2.down * snapDistance;

                SetPosition(position);
            }

            SetGrounded(
                hit.normal);

            RecalculateVelocity();

            return true;
        }

        private bool ProbeGroundInternal(
            float maxDistance)
        {
            if (maxDistance < 0f)
                return false;

            Vector2 position =
                transform.position;

            if (!TryGetGround(
                    position,
                    maxDistance,
                    out RaycastHit2D hit,
                    out _))
            {
                return false;
            }

            SetGrounded(
                hit.normal);

            return true;
        }

        private void ResetCollisionState()
        {
            CollisionFlags =
                CollisionFlags2D.None;

            IsGrounded = false;
            GroundNormal = Vector2.up;

            IsCollidingLeft = false;
            IsCollidingRight = false;

            LeftWallNormal = Vector2.zero;
            RightWallNormal = Vector2.zero;
        }

        // ============================================================
        // Movement query
        // ============================================================

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

            bool found = false;

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

                // Ignore surfaces that movement is tangent to or
                // moving away from.
                //
                // This is particularly important when moving along
                // floors and slopes.
                if (approach <= MinBlockingDot)
                    continue;

                if (!found ||
                    IsBetterMovementHit(
                        hit,
                        closestHit,
                        direction))
                {
                    closestHit =
                        hit;

                    found =
                        true;
                }
            }

            return found;
        }

        private static bool IsBetterMovementHit(
            RaycastHit2D candidate,
            RaycastHit2D current,
            Vector2 direction)
        {
            float distanceDifference =
                candidate.distance -
                current.distance;

            if (distanceDifference <
                -HitTieEpsilon)
            {
                return true;
            }

            if (distanceDifference >
                HitTieEpsilon)
            {
                return false;
            }

            // If two contacts are effectively at the same distance,
            // prefer the surface that blocks movement more strongly.
            float candidateApproach =
                -Vector2.Dot(
                    direction,
                    candidate.normal);

            float currentApproach =
                -Vector2.Dot(
                    direction,
                    current.normal);

            float approachDifference =
                candidateApproach -
                currentApproach;

            if (approachDifference >
                HitTieEpsilon)
            {
                return true;
            }

            if (approachDifference <
                -HitTieEpsilon)
            {
                return false;
            }

            // Stable geometry-based tie breaking.
            //
            // Do NOT use collider instance IDs here. Instance IDs are
            // not guaranteed to match between multiplayer peers.

            if (candidate.normal.y != current.normal.y)
            {
                return
                    candidate.normal.y >
                    current.normal.y;
            }

            if (candidate.normal.x != current.normal.x)
            {
                return
                    candidate.normal.x <
                    current.normal.x;
            }

            if (candidate.point.x != current.point.x)
            {
                return
                    candidate.point.x <
                    current.point.x;
            }

            return
                candidate.point.y <
                current.point.y;
        }

        // ============================================================
        // Ground query
        // ============================================================

        private bool TryGetGround(
            Vector2 position,
            float maxDistance,
            out RaycastHit2D closestHit,
            out float groundDistance)
        {
            closestHit =
                default;

            groundDistance =
                0f;

            if (maxDistance < 0f)
                return false;

            // skinWidth is measured perpendicular to a surface.
            //
            // On a slope the equivalent vertical distance is larger.
            float skinCastDistance =
                _skinWidth /
                Mathf.Max(
                    MinGroundDot,
                    0.01f);

            float castDistance =
                maxDistance +
                skinCastDistance;

            int count =
                CapsuleCast(
                    position,
                    Vector2.down,
                    castDistance);

            bool found =
                false;

            float bestGroundDistance =
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

                float verticalSkinDistance =
                    _skinWidth /
                    Mathf.Max(
                        normal.y,
                        0.01f);

                float candidateGroundDistance =
                    Mathf.Max(
                        hit.distance -
                        verticalSkinDistance,
                        0f);

                if (candidateGroundDistance >
                    maxDistance + HitTieEpsilon)
                {
                    continue;
                }

                if (!found ||
                    IsBetterGroundHit(
                        hit,
                        candidateGroundDistance,
                        closestHit,
                        bestGroundDistance))
                {
                    closestHit =
                        hit;

                    groundDistance =
                        candidateGroundDistance;

                    bestGroundDistance =
                        candidateGroundDistance;

                    found =
                        true;
                }
            }

            return found;
        }

        private static bool IsBetterGroundHit(
            RaycastHit2D candidate,
            float candidateDistance,
            RaycastHit2D current,
            float currentDistance)
        {
            float distanceDifference =
                candidateDistance -
                currentDistance;

            if (distanceDifference <
                -HitTieEpsilon)
            {
                return true;
            }

            if (distanceDifference >
                HitTieEpsilon)
            {
                return false;
            }

            // At an exact seam/corner, prefer the flatter walkable
            // surface. This makes slope transitions more stable.
            if (candidate.normal.y != current.normal.y)
            {
                return
                    candidate.normal.y >
                    current.normal.y;
            }

            if (candidate.normal.x != current.normal.x)
            {
                return
                    candidate.normal.x <
                    current.normal.x;
            }

            if (candidate.point.x != current.point.x)
            {
                return
                    candidate.point.x <
                    current.point.x;
            }

            return
                candidate.point.y <
                current.point.y;
        }

        // ============================================================
        // Capsule query
        // ============================================================

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

            _hits.Clear();

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

            // Ignore our own collider and character child colliders.
            if (hitTransform == transform ||
                hitTransform.IsChildOf(transform))
            {
                return false;
            }

            return true;
        }

        // ============================================================
        // Collision state
        // ============================================================

        private void RegisterCollision(
            Vector2 normal)
        {
            if (IsWalkable(normal))
            {
                SetGrounded(normal);

                return;
            }

            if (normal.y < -0.01f)
            {
                CollisionFlags |=
                    CollisionFlags2D.Above;

                return;
            }

            CollisionFlags |=
                CollisionFlags2D.Sides;

            // Collision normals point away from the surface.
            //
            // Positive X normal:
            //
            // wall | --> player
            //
            // Wall is on the left.
            if (normal.x > 0.01f)
            {
                IsCollidingLeft =
                    true;

                LeftWallNormal =
                    normal;
            }

            // Negative X normal:
            //
            // player <-- | wall
            //
            // Wall is on the right.
            if (normal.x < -0.01f)
            {
                IsCollidingRight =
                    true;

                RightWallNormal =
                    normal;
            }
        }

        private void SetGrounded(
            Vector2 normal)
        {
            IsGrounded =
                true;

            GroundNormal =
                normal.normalized;

            CollisionFlags |=
                CollisionFlags2D.Below;
        }

        private bool IsWalkable(
            Vector2 normal)
        {
            return
                normal.y >=
                MinGroundDot;
        }

        private bool IsSteepSlope(
            Vector2 normal)
        {
            return
                normal.y > 0f &&
                normal.y < MinGroundDot;
        }

        // ============================================================
        // Sliding planes
        // ============================================================

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

            // Clipping against one surface may push movement into
            // another surface at a corner.
            //
            // In 2D, if multiple non-parallel planes constrain the
            // displacement, stop instead of oscillating between them.
            for (int i = 0;
                 i < planeCount;
                 i++)
            {
                if (Vector2.Dot(
                        result,
                        _planes[i]) <
                    -HitTieEpsilon)
                {
                    return Vector2.zero;
                }
            }

            return result;
        }

        // ============================================================
        // Position / velocity
        // ============================================================

        private void SetPosition(
            Vector2 position)
        {
            Vector3 worldPosition =
                transform.position;

            worldPosition.x =
                position.x;

            worldPosition.y =
                position.y;

            transform.position =
                worldPosition;
        }

        private void RecalculateVelocity()
        {
            if (!_hasMoveFrame ||
                _moveDelta <= 0f)
            {
                Velocity =
                    Vector2.zero;

                return;
            }

            Vector2 currentPosition =
                transform.position;

            Velocity =
                (currentPosition - _moveStartPosition) /
                _moveDelta;
        }

        // ============================================================
        // Setup
        // ============================================================

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

            _groundProbeDistance =
                Mathf.Max(
                    0f,
                    _groundProbeDistance);

            _groundSnapDistance =
                Mathf.Max(
                    0f,
                    _groundSnapDistance);

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