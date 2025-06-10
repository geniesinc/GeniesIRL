using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GeniesIRL
{
    public static class ARPlaneUtility
    {
        public const float FLOOR_MAX_HEIGHT_IN_EDITOR = 0.1f;
        public const float CEILING_MIN_HEIGHT_IN_EDITOR = 2f;
        public const float CHAIR_MIN_HEIGHT_IN_EDITOR = 0.4064f;
        public const float CHAIR_MAX_HEIGHT_IN_EDITOR = 0.5334f;
        public const float TABLE_MIN_HEIGHT_IN_EDITOR = 0.72f; 
        public const float TABLE_MAX_HEIGHT_IN_EDITOR = 1.19f;

        /// <summary>
        /// Plane classification doesn't work in the Editor, so we'll use a heuristic to determine classification in that case.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool IsSeat(ARPlane plane)
        {
    #if UNITY_EDITOR
            // We don't have plane classification enabled in editor presently,
            // so we will just say anything flat is floor, and vertical is obstacle.
            return plane.alignment == PlaneAlignment.HorizontalUp &&
                    plane.transform.position.y >= CHAIR_MIN_HEIGHT_IN_EDITOR &&
                    plane.transform.position.y <= CHAIR_MAX_HEIGHT_IN_EDITOR;
    #else
            // If it's a flat surface but not the floor, OR if it's a vertical surface,
            // then it is an obstacle.
            return plane.classifications == PlaneClassifications.SeatOfAnyType;
    #endif
        }

        /// <summary>
        /// Plane classification doesn't work in the Editor, so we'll use a heuristic to determine classification in that case.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool IsTable(ARPlane plane)
        {
    #if UNITY_EDITOR
            // We don't have plane classification enabled in editor presently,
            // so we will just say anything flat is floor, and vertical is obstacle.
            return plane.alignment == PlaneAlignment.HorizontalUp &&
                    plane.transform.position.y >= TABLE_MIN_HEIGHT_IN_EDITOR &&
                    plane.transform.position.y <= TABLE_MAX_HEIGHT_IN_EDITOR;
    #else
            // If it's a flat surface but not the floor, OR if it's a vertical surface,
            // then it is an obstacle.
            return plane.classifications == PlaneClassifications.Table;
    #endif
        }

        /// <summary>
        /// Plane classification doesn't work in the Editor, so we'll use a heuristic to determine classification in that case.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool IsFloor(ARPlane plane)
        {

    #if UNITY_EDITOR
            // We don't have plane classification enabled in editor presently,
            // so we will just say anything flat is floor, and vertical is obstacle.
            return plane.alignment == PlaneAlignment.HorizontalUp &&
                    plane.transform.position.y <= FLOOR_MAX_HEIGHT_IN_EDITOR;
    #else
            // If it's a flat surface but not the floor, OR if it's a vertical surface,
            // then it is an obstacle.
            return plane.classifications == PlaneClassifications.Floor;
    #endif
        }

        /// <summary>
        /// Plane classification doesn't work in the Editor, so we'll use a heuristic to determine classification in that case.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool IsCeiling(ARPlane plane)
        {
    #if UNITY_EDITOR
            return plane.alignment == PlaneAlignment.HorizontalDown &&
                plane.transform.position.y >= CEILING_MIN_HEIGHT_IN_EDITOR;
    #else       
            return plane.classifications == PlaneClassifications.Ceiling;
    #endif
        }

        /// <summary>
        /// Plane classification doesn't work in the Editor, so we'll use a heuristic to determine classification in that case.
        /// </summary>
        /// <param name="plane"></param>
        /// <returns></returns>
        public static bool IsWall(ARPlane plane)
        {

    #if UNITY_EDITOR
            // We don't have plane classification enabled in editor presently,
            // so we will just say anything flat is floor, and vertical is obstacle.
            return plane.alignment == PlaneAlignment.Vertical;
    #else
            // If it's a flat surface but not the floor, OR if it's a vertical surface,
            // then it is an obstacle.
            return plane.classifications == PlaneClassifications.WallFace;
    #endif
        }
    }
}

