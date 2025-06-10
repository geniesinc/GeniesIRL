using UnityEngine;

public class PIzzaSliceTest : MonoBehaviour
{
    public float angle = 45f; // Angle of the pizza slice in degrees
    public float range = 5f;  // Radius of the pizza slice

    private void OnDrawGizmos()
    {
        // Draw the pizza slice respecting the forward direction
        DrawPizzaSlice(transform.position, transform.forward, angle, range, 30);

        // Calculate and draw the bounds respecting the forward direction
        Bounds bounds = CalculateBounds(transform.position, transform.forward, angle, range);
        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(bounds.center, bounds.size);
    }

    private void DrawPizzaSlice(Vector3 center, Vector3 forward, float angle, float radius, int segments)
    {
        Gizmos.color = Color.yellow;

        // Ensure forward is restricted to the XZ plane and normalized
        forward.y = 0;
        forward.Normalize();

        // Calculate the rotation based on the forward direction
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

        // Convert the angle to radians and calculate the half angle
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;

        // Start and end directions for the arc
        Vector3 startDirection = rotation * new Vector3(Mathf.Sin(-halfAngle), 0, Mathf.Cos(-halfAngle));
        Vector3 endDirection = rotation * new Vector3(Mathf.Sin(halfAngle), 0, Mathf.Cos(halfAngle));

        // Draw the base lines of the pizza slice
        Gizmos.DrawLine(center, center + startDirection * radius);
        Gizmos.DrawLine(center, center + endDirection * radius);

        // Draw the curved edge of the pizza slice
        Vector3 previousPoint = center + startDirection * radius;
        for (int i = 1; i <= segments; i++)
        {
            float t = (float)i / segments;
            float interpolatedAngle = Mathf.Lerp(-halfAngle, halfAngle, t);
            Vector3 point = rotation * new Vector3(Mathf.Sin(interpolatedAngle), 0, Mathf.Cos(interpolatedAngle)) * radius;

            Gizmos.DrawLine(previousPoint, center + point);
            previousPoint = center + point;
        }
    }

    private Bounds CalculateBounds(Vector3 center, Vector3 forward, float angle, float radius)
    {
        // Ensure forward is restricted to the XZ plane and normalized
        forward.y = 0;
        forward.Normalize();

        // Calculate the rotation based on the forward direction
        Quaternion rotation = Quaternion.LookRotation(forward, Vector3.up);

        // Convert the angle to radians and calculate the half angle
        float halfAngle = angle * 0.5f * Mathf.Deg2Rad;

        // Calculate the extreme points of the pizza slice
        Vector3 startDirection = rotation * new Vector3(Mathf.Sin(-halfAngle), 0, Mathf.Cos(-halfAngle)) * radius;
        Vector3 endDirection = rotation * new Vector3(Mathf.Sin(halfAngle), 0, Mathf.Cos(halfAngle)) * radius;
        Vector3 forwardPoint = rotation * new Vector3(0, 0, 1) * radius;

        // Collect all points to consider for the bounds
        Vector3[] points = new Vector3[4]
        {
            center,
            center + startDirection,
            center + endDirection,
            center + forwardPoint
        };

        // Initialize min and max points for the bounding box
        Vector3 min = points[0];
        Vector3 max = points[0];

        // Iterate through all points to find the min and max bounds
        foreach (var point in points)
        {
            min = Vector3.Min(min, point);
            max = Vector3.Max(max, point);
        }

        // Create and return the Bounds
        return new Bounds((min + max) * 0.5f, max - min);
    }
}
