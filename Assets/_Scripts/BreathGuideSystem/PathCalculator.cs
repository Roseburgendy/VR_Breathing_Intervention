using UnityEngine;

namespace _Scripts.BreathGuideSystem
{
    /// <summary>
    /// Utility class for calculating beam paths
    /// Handles bezier curve calculations and path generation
    /// </summary>
    public static class PathCalculator
    {
        /// <summary>
        /// Calculate position along a quadratic bezier curve
        /// </summary>
        /// <param name="p0">Start point</param>
        /// <param name="p1">Control point</param>
        /// <param name="p2">End point</param>
        /// <param name="t">Progress along curve (0-1)</param>
        /// <returns>Position on the curve at time t</returns>
        public static Vector3 QuadraticBezier(Vector3 p0, Vector3 p1, Vector3 p2, float t)
        {
            float u = 1 - t;
            return u * u * p0 + 2 * u * t * p1 + t * t * p2;
        }

        public static Vector3 GetCP2OnPathXY(
            Vector3 ringCenter,
            float radius,
            MovementType type,
            bool isLeftHand,
            float t01,
            Vector3 ringRight,
            float chestInnerRatio = 0.4f
        )
        {
            t01 = Mathf.Clamp01(t01);
            switch (type)
            {
                // UpDown
                case MovementType.VerticalUp:
                case MovementType.VerticalDown:
                {
                    // Up: start 270; Down: start 90
                    float start = (type == MovementType.VerticalUp) ? 270f : 90f; 
                    bool isDown = (type == MovementType.VerticalDown); 
                    // Up: left = +180, right = -180
                    // Down: left = -180, right = +180 
                    float delta = isLeftHand
                        ? (isDown ? -180f : +180f)
                        : (isDown ? +180f : -180f);

                    float angle = start + delta * t01;
                    return PointOnRingXY(ringCenter, radius, angle);
                }

                // Chest Expand
                case MovementType.HorizontalOpen:
                case MovementType.HorizontalClose:
                {
                    Vector3 right = ringRight.normalized;

                    Vector3 outer = ringCenter + (isLeftHand ? right : -right) * radius;
                    Vector3 inner = Vector3.Lerp(ringCenter, outer, Mathf.Clamp01(chestInnerRatio));

                    bool inhaleOpen = (type == MovementType.HorizontalOpen);
                    Vector3 from = inhaleOpen ? inner : outer;
                    Vector3 to   = inhaleOpen ? outer : inner;

                    return Vector3.Lerp(from, to, t01);
                }


                // Circle Breath
                case MovementType.CircleInhale:
                case MovementType.CircleExhale:
                {
                    float leftTop = 135f, leftBottom = 225f;
                    float rightTop = 45f, rightBottom = 315f;

                    bool inhale = (type == MovementType.CircleInhale);
                    float startDeg, endDeg;
                    bool useLeftPath = !isLeftHand;
    
                    // Inhale 
                    if (inhale)
                    {
                        if (useLeftPath) { startDeg = leftTop;    endDeg = leftBottom; }
                        else             { startDeg = rightBottom; endDeg = rightTop;   }
                    }
                    // Exhale
                    else
                    {
                        if (useLeftPath) { startDeg = leftBottom; endDeg = leftTop;    }
                        else             { startDeg = rightTop;    endDeg = rightBottom; }
                    }

                    float delta = Mathf.DeltaAngle(startDeg, endDeg);
                    float angle = startDeg + delta * t01;

                    return PointOnRingXY(ringCenter, radius, angle);
                }

                default:
                    // safe fallback
                    return ringCenter;
            }
        }

        // XY ring: right = +X, up = +Y
        public static Vector3 PointOnRingXY(Vector3 center, float radius, float angleDeg)
        {
            float rad = angleDeg * Mathf.Deg2Rad;
            return center + radius * (Mathf.Cos(rad) * Vector3.right + Mathf.Sin(rad) * Vector3.up);
        }

    }
}