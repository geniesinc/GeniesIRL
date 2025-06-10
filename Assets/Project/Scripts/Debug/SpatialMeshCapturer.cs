using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using System.IO;

#if UNITY_EDITOR
using UnityEditor;
#endif

[RequireComponent(typeof(ARMeshManager))]
public class SpatialMeshCapturer : MonoBehaviour
{
    [Tooltip("This is where your captured spatial meshes will go.")]
    public string OutputDirectory = "Assets/Project/3D/Debug/CapturedSpatialMeshes/";

    private ARMeshManager meshManager;

    private void Awake()
    {
        meshManager = GetComponent<ARMeshManager>();
        if (meshManager == null)
        {
            Debug.LogError("ARMeshManager component is missing.");
        }
    }

    public void CaptureSpatialMeshes()
    {
        if (meshManager == null)
        {
            Debug.LogError("ARMeshManager component is missing.");
            return;
        }

        IList<MeshFilter> meshFilters = meshManager.meshes;
        if (meshFilters.Count == 0)
        {
            Debug.LogWarning("No meshes found to capture.");
            return;
        }

        Mesh combinedMesh = new Mesh();

        combinedMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        CombineInstance[] combine = new CombineInstance[meshFilters.Count];

        for (int i = 0; i < meshFilters.Count; i++)
        {
            combine[i].mesh = meshFilters[i].sharedMesh;
            combine[i].transform = meshFilters[i].transform.localToWorldMatrix;
        }

        combinedMesh.CombineMeshes(combine, true, true);

        SaveMeshToFile(combinedMesh);
    }

    private void SaveMeshToFile(Mesh mesh)
    {

#if UNITY_EDITOR
        // Ensure the persistent data directory exists
        string directoryPath = OutputDirectory;
        if (!Directory.Exists(directoryPath))
        {
            Directory.CreateDirectory(directoryPath);
        }

        // Generate a unique filename
        string fileName = GetUniqueFileName(directoryPath, "CapturedSpatialMesh", ".asset");
        string filePath = Path.Combine(directoryPath, fileName);

        // Save the mesh
        AssetDatabase.CreateAsset(mesh, filePath);
        AssetDatabase.SaveAssets();

        Debug.Log($"Mesh saved to: {filePath}");
#else
        Debug.LogError("Capturing spatial meshes is currently only supported in Editor mode");
#endif
    }

    private string GetUniqueFileName(string directory, string baseName, string extension)
    {
        string fileName = $"{baseName}{extension}";
        int count = 1;
        while (File.Exists(Path.Combine(directory, fileName)))
        {
            fileName = $"{baseName} ({count}){extension}";
            count++;
        }
        return fileName;
    }
}
