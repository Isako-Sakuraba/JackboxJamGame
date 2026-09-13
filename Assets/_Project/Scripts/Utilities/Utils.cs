using UnityEngine;

namespace Game.Utilities
{
    public static class Utils
    {
        public static float GetJumpVelocity(float jumpHeight, float gravity)
        {
            return Mathf.Sqrt(2f * gravity * jumpHeight);
        }

        public static float GetAcceleration(
            float input,
            float currentSpeed,
            float acceleration,
            float deceleration,
            float turnAcceleration)
        {
            if (Mathf.Approximately(input, 0f))
                return deceleration;

            float targetSpeedSign = Mathf.Sign(input);
            float currentSpeedSign = Mathf.Sign(currentSpeed);

            if (!Mathf.Approximately(currentSpeed, 0f) &&
                targetSpeedSign != currentSpeedSign)
            {
                return turnAcceleration;
            }

            return acceleration;
        }

        public static Vector2 Project(in Vector2 a, in Vector2 b)
        {
            float bSqrMagnitude = b.sqrMagnitude;

            if (bSqrMagnitude < Mathf.Epsilon)
                return Vector2.zero;

            return b * (Vector2.Dot(a, b) / bSqrMagnitude);
        }
    }
}
