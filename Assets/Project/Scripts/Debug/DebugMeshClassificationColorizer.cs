using System;
using System.Collections.Generic;
using System.Globalization;
using Unity.Collections;
using UnityEngine;
using UnityEngine.XR;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;
using UnityEngine.XR.VisionOS;

namespace GeniesIRL 
{
    /// <summary>
    /// Attach this to an ARMeshManager to colorize the meshes based on classification. Debug material should use the shader called
    /// DebugMeshClassification.
    /// </summary>
    [RequireComponent(typeof(ARMeshManager))]
    public class DebugMeshClassificationColorizer : MonoBehaviour
    {
        [Header("Shader‑Graph material that shows vertex colours")]
        public Material debugMaterial;

    static readonly Dictionary<ARMeshClassification,Color32> kColor =
        new() {
            { ARMeshClassification.None,          new(128,128,128, 40) }, // un‑classified / unknown
            { ARMeshClassification.Wall,          new(  0,128,255,120) },
            { ARMeshClassification.Floor,         new(  0,255,  0,120) },
            { ARMeshClassification.Ceiling,       new(255,255,  0,120) },
            { ARMeshClassification.Table,         new(210,105, 30,120) },
            { ARMeshClassification.Seat,          new(255,105,180,120) },
            { ARMeshClassification.Window,        new(135,206,250,120) },
            { ARMeshClassification.Door,          new(160, 82, 45,120) },
            { ARMeshClassification.WallDecoration,new(255,215,  0,120) },
            { ARMeshClassification.Blinds,        new(176,196,222,120) },
            { ARMeshClassification.Fireplace,     new(255, 69,  0,120) },
            { ARMeshClassification.Stairs,        new(205,133, 63,120) },
            { ARMeshClassification.Bed,           new(218,112,214,120) },
            { ARMeshClassification.Counter,       new(112,128,144,120) },
            { ARMeshClassification.Cabinet,       new(139, 69, 19,120) },
            { ARMeshClassification.HomeAppliance, new(192,192,192,120) },
            { ARMeshClassification.DoorFrame,     new(184,134, 11,120) },
            { ARMeshClassification.TV,            new( 30,144,255,120) },
            { ARMeshClassification.Whiteboard,    new(245,245,245,120) },
            { ARMeshClassification.Plant,         new( 34,139, 34,120) },
        };
        ARMeshManager  _meshMgr;
        XRMeshSubsystem _subsystem;

        void Awake()
        {
            _meshMgr   = GetComponent<ARMeshManager>();
            _subsystem = _meshMgr.subsystem as XRMeshSubsystem;
            _subsystem?.SetClassificationEnabled(true);        // visionOS API :contentReference[oaicite:2]{index=2}

            _meshMgr.meshesChanged += OnMeshesChanged;
        }

        void OnDestroy() => _meshMgr.meshesChanged -= OnMeshesChanged;

        void OnMeshesChanged(ARMeshesChangedEventArgs evt)
        {
            foreach (var mf in evt.added)   Colorize(mf);
            foreach (var mf in evt.updated) Colorize(mf);
        }

        void Colorize(MeshFilter mf)
        {
            var mesh = mf.sharedMesh;
            if (mesh == null) return;

            // ARMeshManager names filters with the TrackableId string
            if (!TryGetTrackableId(mf, out var tId)) {
                Debug.LogWarning($"MeshFilter {mf.name} has no valid TrackableId");
                return;
            }
        
            using var faces = _subsystem.GetFaceClassifications(tId, Allocator.Temp);   // :contentReference[oaicite:3]{index=3}
            if (!faces.IsCreated || faces.Length == 0) return;

            // grow the colour array only once
            var cols = mesh.colors32.Length == mesh.vertexCount
                    ? mesh.colors32
                    : new Color32[mesh.vertexCount];

            var tris = mesh.triangles;
            for (int face = 0; face < faces.Length; ++face)
            {
                // look up colour or fall back to magenta for never‑seen enums
                var clr = kColor.TryGetValue(faces[face], out var c) ? c : new Color32(255, 0, 255, 255);

                int i0 = tris[face * 3 + 0];
                int i1 = tris[face * 3 + 1];
                int i2 = tris[face * 3 + 2];
                cols[i0] = cols[i1] = cols[i2] = clr;
            }

            mesh.colors32 = cols;                       // pushes data to PolySpatial

            var mr = mf.GetComponent<MeshRenderer>();
            if (mr.sharedMaterial != debugMaterial)
                mr.sharedMaterial = debugMaterial;     // draw with the debug shader
        }

        static bool TryGetTrackableId(MeshFilter mf, out TrackableId id)
        {
            id = TrackableId.invalidId;

            // 1. Strip "Mesh " prefix and any dash Unity added
            string raw = mf.name.StartsWith("Mesh ")
                    ? mf.name.Substring(5)
                    : mf.name;
            raw = raw.Replace("-", "");

            if (raw.Length != 32)        // quick sanity‑check
                return false;

            // 2. Parse the two 16‑hex‑digit halves
            try
            {
                ulong hi = ulong.Parse(raw.AsSpan(0, 16),  NumberStyles.HexNumber);
                ulong lo = ulong.Parse(raw.AsSpan(16, 16), NumberStyles.HexNumber);
                id = new TrackableId(hi, lo);              // ← no FormatException path
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}

