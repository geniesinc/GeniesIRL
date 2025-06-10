using System;
using GeniesIRL;
using Unity.Collections;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using GeneisIRL;

public static class UsefulExtensions
{
    /// <summary>
    /// Returns true if the status is either Success or Failure. Otherwise, it'll return false. Useful for determining whether an Action was interrupted.
    /// </summary>
    /// <param name="status"></param>
    /// <returns></returns>
    public static bool IsCompleted(this Unity.Behavior.Node.Status status)
    {
        return status == Unity.Behavior.Node.Status.Success || status == Unity.Behavior.Node.Status.Failure;
    }

    /// <summary>
    /// In the legacy animation system, this method will return the currently playing animation state.
    /// </summary>
    /// <param name="anim"></param>
    /// <returns></returns>
    public static AnimationState GetCurrentlyPlayingAnimationState(this Animation anim)
    {
        foreach (AnimationState state in anim)
        {
            if (anim.IsPlaying(state.name))
            {
                return state;
            }
        }

        return null;
    }

    /// <summary>
    /// In the legacy animation component, this method will pause the currently playing animation.
    /// </summary>
    /// <param name="anim"></param>
    public static void PauseCurrentlyPlayingAnimation(this Animation anim)
    {
        AnimationState state = anim.GetCurrentlyPlayingAnimationState();
        
        if (state != null)
        {
            Debug.Log("Currently playing animation state: " + state.name);
            state.speed = 0;
        }
    }

    // public static Transform FindDeepChild(this Transform parent, string childName)
    // {
    //     foreach (Transform child in parent)
    //     {
    //         if (child.name == childName)
    //         {
    //             return child;
    //         }

    //         // Recursively search in child's children
    //         Transform found = FindDeepChild(child, childName);
    //         if (found != null)
    //         {
    //             return found;
    //         }
    //     }
    //     return null;
    // }

    public static string FirstCharToUpper(this string input) =>
        input switch
        {
            null => throw new ArgumentNullException(nameof(input)),
            "" => throw new ArgumentException($"{nameof(input)} cannot be empty", nameof(input)),
            _ => input[0].ToString().ToUpper() + input.Substring(1)
        };

    // AR PLANE EXTENSIONS
    /// <summary>
    /// Returns a random point (in world space) inside the ARPlane’s boundary.
    /// </summary>
    public static Vector3 GetRandomPointOnARPlane(this ARPlane plane)
    {
        // If there are no boundary points, return the plane’s center.
        if (plane.boundary.Length == 0)
            return plane.transform.position;

        // Determine the 2D bounding box of the plane (in plane-local coordinates).
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;
        for (int i = 0; i < plane.boundary.Length; i++)
        {
            Vector2 pt = plane.boundary[i];
            if (pt.x < minX) minX = pt.x;
            if (pt.x > maxX) maxX = pt.x;
            if (pt.y < minY) minY = pt.y;
            if (pt.y > maxY) maxY = pt.y;
        }

        // Try to find a random point within the boundary via rejection sampling.
        Vector2 randomPoint;
        int attempts = 0;
        do
        {
            float x = UnityEngine.Random.Range(minX, maxX);
            float y = UnityEngine.Random.Range(minY, maxY);
            randomPoint = new Vector2(x, y);
            attempts++;

            // Give up after a high number of attempts (falling back to the plane center).
            if (attempts > 100)
            {
                return plane.transform.position;
            }
        }
        while (!IsPointInPolygon(randomPoint, plane.boundary));

        // Convert the found 2D point to 3D.
        // Note: ARPlane.boundary points are in the plane’s local coordinate space,
        // and here we assume the plane’s local XY corresponds to world XZ (with Y=0).
        Vector3 localPoint = new Vector3(randomPoint.x, 0, randomPoint.y);
        return plane.transform.TransformPoint(localPoint);
    }

    // Standard ray-casting algorithm to check if a point is inside a polygon.
    private static bool IsPointInPolygon(Vector2 point, NativeArray<Vector2> polygon)
    {
        bool inside = false;
        int count = polygon.Length;
        for (int i = 0, j = count - 1; i < count; j = i++)
        {
            Vector2 pi = polygon[i];
            Vector2 pj = polygon[j];
            bool intersect = ((pi.y > point.y) != (pj.y > point.y)) &&
                             (point.x < (pj.x - pi.x) * (point.y - pi.y) / (pj.y - pi.y) + pi.x);
            if (intersect)
            {
                inside = !inside;
            }
        }
        return inside;
    }

    /// <summary>
    ///For whatever reason, checking against "SeatOfAnyType" is not a "catch all" for all types of seats, so we have to check for
    // "Seat" and "Couch" as well.
    /// </summary>
    /// <param name="plane"></param>
    /// <param name="horizontalThreshold">The heigher the threshold, the more forgiving. (Max should be 1)</param>
    /// <returns></returns>
    public static bool IsSittable(this ARPlane plane, float horizontalThreshold = 0.2f)
    {
        bool isClassifiedAsSeat = plane.classifications == PlaneClassifications.SeatOfAnyType || 
            plane.classifications == PlaneClassifications.Seat || 
            plane.classifications == PlaneClassifications.Couch;
          
            if (!GeniesIRL.App.XR.IsPolySpatialEnabled)
            {
                // Special case for non-polyspatial Editor testing, where we don't have plane classification. If the plane is at a certain height, we'll consider it a seat.
                isClassifiedAsSeat = plane.transform.position.y > 0.25f && plane.transform.position.y < 0.5f;
            }

        if (!isClassifiedAsSeat) return false;

        // Check if the plane is horizontal (within a certain threshold of being flat)
        return Mathf.Abs(Vector3.Dot(plane.normal, Vector3.up)) >= 1f-horizontalThreshold;
    }

    public static float GetSmallestEulerValue(this float angle)
    {
        if (angle > 180)
        {
            return angle - 360;
        }
        else if (angle < -180)
        {
            return angle + 360;
        }
        return angle;
    }

    public static float GetPositiveEulerValue(this float angle)
    {
        if (angle < 0)
        {
            return angle + 360;
        }
        return angle;
    }

    public static float GetAspectRatio(this Vector2 rect)
    {
        if(rect.y == 0)
        {
            return 0;
        }
        return rect.x / rect.y;
    }

    public static Quaternion ExtractYaw(this Quaternion q)
    {
        q.x = 0;
        q.z = 0;
        float mag = Mathf.Sqrt(q.w * q.w + q.y * q.y);
        q.w /= mag;
        q.y /= mag;
        return q;
    }

    public static Quaternion ExtractPitch(this Quaternion q)
    {
        q.y = 0;
        q.z = 0;
        float mag = Mathf.Sqrt(q.w * q.w + q.x * q.x);
        q.w /= mag;
        q.x /= mag;
        return q;
    }

    public static Quaternion ExtractRoll(this Quaternion q)
    {
        q.x = 0;
        q.y = 0;
        float mag = Mathf.Sqrt(q.w * q.w + q.z * q.z);
        q.w /= mag;
        q.z /= mag;
        return q;
    }

    public static bool CanPlaceOnSurfaceWithNormal(this Vector3 surfaceNormal)
    {
        return Mathf.Abs(Vector3.Dot(surfaceNormal, Vector3.up)) >= 0.5f;
    }

    public static void RemoveAllSources(this ParentConstraint parentConstraint)
    {
        for (int i = parentConstraint.sourceCount - 1; i >= 0; i--)
        {
            parentConstraint.RemoveSource(i);
        }
    }
}

public static class RectTransformExtensions
{
    public static void SetLeft(this RectTransform rt, float left)
    {
        rt.offsetMin = new Vector2(left, rt.offsetMin.y);
    }

    public static void SetRight(this RectTransform rt, float right)
    {
        rt.offsetMax = new Vector2(-right, rt.offsetMax.y);
    }

    public static void SetTop(this RectTransform rt, float top)
    {
        rt.offsetMax = new Vector2(rt.offsetMax.x, -top);
    }

    public static void SetBottom(this RectTransform rt, float bottom)
    {
        rt.offsetMin = new Vector2(rt.offsetMin.x, bottom);
    }
}

// Values we should consider adopting from Genies Camera:
public static class Tags
{
    public const string Item = "Item";
}

public enum Layers
{
    Default = 0,
    TransparentFX = 1,
    IgnoreRaycast = 2,
    Water = 4,
    UI = 5,
    PlacementObject = 6,
    Placement_Surface = 7,
    Spawner_Contact = 8,
    User = 9,
    Interactable = 10,
    SpatialMesh = 29,
    XR_Simulatinon = 30,
    PolySpatial = 31
}
