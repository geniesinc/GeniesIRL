using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class DebugUtils : MonoBehaviour
{
    public static void DrawPolygon(Vector3 center, float radius, int sides, Color color)
    {
        float angleStep = 360f / sides;

        Vector3[] vertices = new Vector3[sides];
        for (int i = 0; i < sides; i++)
        {
            float angle = Mathf.Deg2Rad * (i * angleStep);
            vertices[i] = center + new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle)) * radius;
        }

        for (int i = 0; i < sides; i++)
        {
            Vector3 start = vertices[i];
            Vector3 end = vertices[(i + 1) % sides];
            Debug.DrawLine(start, end, color);
        }

    }
}
