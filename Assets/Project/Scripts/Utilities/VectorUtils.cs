using System;
using NUnit.Framework;
using UnityEngine;

namespace GeniesIRL 
{
    public static class VectorUtils
    {
        /// <summary>
        /// Checks if two points are within a certain distance of each other.
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsWithinDistance(Vector3 pointA, Vector3 pointB, float distance)
        {
            return (pointA - pointB).sqrMagnitude <= distance * distance;
        }

        /// <summary>
        /// Checks if two points are within a certain distance of each other, ignoring the Y axis.
        /// </summary>
        /// <param name="pointA"></param>
        /// <param name="pointB"></param>
        /// <param name="distance"></param>
        /// <returns></returns>
        public static bool IsWithinDistanceXZ(Vector3 pointA, Vector3 pointB, float distance)
        {
            pointA.y = 0;
            pointB.y = 0;
            return IsWithinDistance(pointA, pointB, distance);
        }

        public static bool IsGreaterThanDistance(Vector3 pointA, Vector3 pointB, float distance)
        {
            return (pointA - pointB).sqrMagnitude > distance * distance;
        }

        public static bool IsGreaterThanDistanceXZ(Vector3 pointA, Vector3 pointB, float distance)
        {
            pointA.y = 0;
            pointB.y = 0;
            return IsGreaterThanDistance(pointA, pointB, distance);
        }

        public static float GetDistanceXZ(Vector3 pointA, Vector3 pointB)
        {
            pointA.y = 0;
            pointB.y = 0;
            return Vector3.Distance(pointA, pointB);
        }

        /// <summary>
        /// Similar to Mathf.Approximately, but for Vector3.
        /// </summary>
        /// <param name="a"></param>
        /// <param name="b"></param>
        /// <returns></returns>
        public static bool Approximately(Vector3 a, Vector3 b)
        {
            if (!Mathf.Approximately(a.x, b.x)) return false;
            if (!Mathf.Approximately(a.y, b.y)) return false;
            return Mathf.Approximately(a.z, b.z);
        }


        /// <summary>
        /// Checks if 'point' is inside a rotated box defined by 'center', 'rotation', and 'size'.
        /// </summary>
        /// <param name="point">World space point to test.</param>
        /// <param name="center">World space center of the rotated box.</param>
        /// <param name="rotation">Rotation (Quaternion) of the box in world space.</param>
        /// <param name="size">Dimensions of the box in its local space.</param>
        /// <returns>True if the point is inside the box; otherwise false.</returns>
        public static bool IsPointInsideBox(Vector3 point, Vector3 center, Quaternion rotation, Vector3 size)
        {
            // 1. Translate point into box's local space by subtracting center
            Vector3 direction = point - center;

            // 2. Rotate into local space using inverse rotation
            //    (inverse of the box rotation will align the box with the axes)
            Vector3 localPos = Quaternion.Inverse(rotation) * direction;

            // 3. Compare the absolute values of localPos to half the box size
            Vector3 halfSize = size * 0.5f;

            // If inside along x, y, and z, then point is within box
            return Mathf.Abs(localPos.x) <= halfSize.x &&
                Mathf.Abs(localPos.y) <= halfSize.y &&
                Mathf.Abs(localPos.z) <= halfSize.z;
        }

        /// <summary>
        /// Checks the XZ components of two vectors for approximate equality. Ignores the Y component.
        /// </summary>
        /// <param name="position"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static bool ApproximatelyXZ(Vector3 position1, Vector3 position2)
        {
            if (!Mathf.Approximately(position1.x, position2.x)) return false;
            return Mathf.Approximately(position1.z, position2.z);
        }

        /// <summary>
        /// Returns the square distance between two points, ignoring the Y axis.
        /// </summary>
        /// <param name="closestPointToUser"></param>
        /// <param name="userPos"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public static float GetSquareDistanceXZ(Vector3 closestPointToUser, Vector3 userPos)
        {
            closestPointToUser.y = 0;
            userPos.y = 0;
            return (closestPointToUser - userPos).sqrMagnitude;
        }
    }
}

