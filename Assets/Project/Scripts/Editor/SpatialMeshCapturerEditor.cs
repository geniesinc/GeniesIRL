using UnityEditor;
using UnityEngine;
using System.IO;
using Application = UnityEngine.Application;

[CustomEditor(typeof(SpatialMeshCapturer))]
public class SpatialMeshCapturerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpatialMeshCapturer script = (SpatialMeshCapturer)target;

        GUI.enabled = Application.isPlaying;
        if (GUILayout.Button("Capture Spatial Meshes"))
        {
            script.CaptureSpatialMeshes();
        }
        GUI.enabled = true;

    }
}
