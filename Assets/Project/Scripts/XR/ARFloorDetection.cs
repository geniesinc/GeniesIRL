using System;
using System.Collections;
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
    /// Uses AR Mesh data to detect the floor height near the user. Note that this will ONLY work on device, because it uses Mesh Classification.
    /// </summary>
    [RequireComponent(typeof(ARMeshManager))]
    public class ARFloorDetection : MonoBehaviour
    {
        /* ───────── Inspector ───────── */
        public Camera userHeadCamera;
        [Tooltip("XZ-plane radius (metres) around the camera inside which a mesh "
            + "triangle is considered for the floor calculation.")]
        public float detectionRadiusFromCameraXZ = 2f;

        [Tooltip("Cull any mesh triangles that are more than this distance below the Camera. This prevents us from being confused by lower floors.")]
        public float maxDistanceBelowCamera = 2.5f;
        /* --- DEBUG (Inspector) -------------------------------------------- */
        [Header("Debug options")]
        public bool  showWinnerSpheres    = false;
        public bool  showCandidateSpheres = false;

        [Header("Winner-bin spheres (yellow)")]
        public int   debugSphereCount  = 5;
        public float debugSphereScale  = 0.05f;
        public Color debugSphereColor  = Color.yellow;

        [Header("Candidate-face spheres (cyan)")]
        public int   candidateSphereCount = 2000;
        public float candidateSphereScale = 0.02f;
        public Color candidateSphereColor = Color.cyan;
        /* ------------------------------------------------------------------ */

        /// <summary>
        /// This is the Y coordinate of the floor, as determined by the algorithm. It's null until the first time it's set. Note that in the Editor, this
        /// value is always set to 0, because we are relying on MeshClassification, which is only available on device.
        /// </summary>
        public float? FloorY { get; private set; }

        /* ───────── Tweakables ───────── */

        const float kBinSize       = 0.01f;  // 1 cm
        const float kHeadClearance = 0.10f;
        const int   kFacesPerFrame = 1000;
        const int   kMinVotes      = 5;
        const float kNormalThreshold = 0.5f;

        static readonly Vector3 kUp = Vector3.up;

        /* ───────── Internals ───────── */

        ARMeshManager   _meshMgr;
        XRMeshSubsystem _subsystem;

        readonly Dictionary<int,int>           _hist  = new();
        readonly Dictionary<int,float> _sumY  = new();
        readonly Dictionary<int,List<Vector3>> _binsSamples = new();
        readonly Dictionary<MeshFilter,CachedMesh> _cache = new();
        readonly List<MeshFilter> _meshFilters = new();

        int _curMeshIdx, _curFaceIdx;

        /* --- debug pools --------------------------------------------------- */
        List<GameObject> _debugSpheres;                 // yellow – winner bin
        List<GameObject> _candidateSpheres;             // cyan   – candidates
        readonly List<Vector3> _candidatePos = new();   // per-frame cache

        /* ================================================================== */
        #region Unity lifecycle

        void Awake()
        {
            _meshMgr = GetComponent<ARMeshManager>();

            if (Application.isEditor) 
            {
                FloorY = 0f; // in the Editor, we don't have mesh classification, so we'll just assume the floor is at 0
                this.enabled = false;
                return;
            }

            _meshMgr.meshesChanged += OnMeshesChanged;

            /* ----- winner-bin pool --------------------------------------- */
            if (showWinnerSpheres)
            {
                _debugSpheres = new List<GameObject>(debugSphereCount);
                for (int i = 0; i < debugSphereCount; ++i)
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.name = $"FloorWinnerSphere_{i}";
                    s.transform.localScale = Vector3.one * debugSphereScale;
                    var mr = s.GetComponent<Renderer>();
                    mr.material = new Material(Shader.Find("Standard"))
                    { color = debugSphereColor };
                    s.SetActive(false);
                    _debugSpheres.Add(s);
                }
            }

            /* ----- candidate-face pool ------------------------------------ */
            if (showCandidateSpheres)
            {
                _candidateSpheres = new List<GameObject>(candidateSphereCount);
                for (int i = 0; i < candidateSphereCount; ++i)
                {
                    var s = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                    s.name = $"FloorCandidateSphere_{i}";
                    s.transform.localScale = Vector3.one * candidateSphereScale;
                    var mr = s.GetComponent<Renderer>();
                    mr.material = new Material(Shader.Find("Standard"))
                    { color = candidateSphereColor };
                    s.SetActive(false);
                    _candidateSpheres.Add(s);
                }
            }
        }

        IEnumerator Start()
        {
            if (Application.isEditor) yield break;

            while (_meshMgr.subsystem == null) yield return null;
            _subsystem = _meshMgr.subsystem as XRMeshSubsystem;

   
            if (_subsystem != null && _subsystem.running)
                _subsystem.SetClassificationEnabled(true);
    
        }

        void OnDestroy() => _meshMgr.meshesChanged -= OnMeshesChanged;

        void Update()
        {
            if (Application.isEditor) return;

            if (_meshFilters.Count == 0 || userHeadCamera == null) return;

            float camX = userHeadCamera.transform.position.x;
            float camZ = userHeadCamera.transform.position.z;
            float camY = userHeadCamera.transform.position.y;
            float radiusSq = detectionRadiusFromCameraXZ * detectionRadiusFromCameraXZ;

            int facesLeft = kFacesPerFrame;

            while (facesLeft > 0 && _curMeshIdx < _meshFilters.Count)
            {
                var mf = _meshFilters[_curMeshIdx];
                if (!_cache.TryGetValue(mf, out var cm) || cm.floorFaces.Count == 0)
                { _curMeshIdx++; _curFaceIdx = 0; continue; }

                var verts = cm.vertices;
                var tris  = cm.triangles;
                var faces = cm.floorFaces;
                var l2w   = mf.transform.localToWorldMatrix;

                for (; _curFaceIdx < faces.Count && facesLeft-- > 0; _curFaceIdx++)
                {
                    int face = faces[_curFaceIdx];
                    int vIdx = tris[face * 3];
                    Vector3 worldV = l2w.MultiplyPoint3x4(verts[vIdx]);

                    if (worldV.y > camY - kHeadClearance) continue;

                    if (worldV.y < camY - maxDistanceBelowCamera) continue;

                    float dx = worldV.x - camX, dz = worldV.z - camZ;
                    if (dx*dx + dz*dz > radiusSq) continue;

                    if (showCandidateSpheres && _candidatePos.Count < candidateSphereCount)
                        _candidatePos.Add(worldV);

                    int bin = Mathf.FloorToInt(worldV.y / kBinSize);
                    _hist.TryGetValue(bin, out int hits);
                    _hist[bin] = hits + 1;

                    _sumY.TryGetValue(bin, out float acc); 
                    _sumY[bin] = acc + worldV.y; 

                    if (!_binsSamples.TryGetValue(bin, out var list))
                        _binsSamples[bin] = list = new List<Vector3>(debugSphereCount);
                    if (list.Count < debugSphereCount) list.Add(worldV);
                }

                if (_curFaceIdx >= faces.Count) { _curMeshIdx++; _curFaceIdx = 0; }
            }

            if (showCandidateSpheres) ShowCandidateSpheres();

            if (_curMeshIdx >= _meshFilters.Count)
            {
                UpdateFloorHeight();
                _hist.Clear(); _sumY.Clear(); _binsSamples.Clear();
                _curMeshIdx = _curFaceIdx = 0;
            }
        }

        #endregion
        /* ================================================================== */
        #region Mesh caching  (unchanged)

        void OnMeshesChanged(ARMeshesChangedEventArgs e)
        {
            foreach (var mf in e.removed) { _cache.Remove(mf); _meshFilters.Remove(mf); }
            foreach (var mf in e.added)   CacheMesh(mf);
            foreach (var mf in e.updated) CacheMesh(mf);
        }

        void CacheMesh(MeshFilter mf)
        {
            if (mf.sharedMesh == null) return;
            if (!TryGetTrackableId(mf, out var tId)) return;

            using var cls = _subsystem.GetFaceClassifications(tId, Allocator.Temp);
            if (!cls.IsCreated || cls.Length == 0) return;

            var mesh = mf.sharedMesh;
            var cm = new CachedMesh
            {
                vertices   = mesh.vertices,
                triangles  = mesh.triangles,
                floorFaces = new List<int>(64)
            };

            for (int face = 0; face < cls.Length; ++face)
            {
                if (cls[face] != ARMeshClassification.Floor) continue;

                int i0 = cm.triangles[face*3], i1 = cm.triangles[face*3+1],
                    i2 = cm.triangles[face*3+2];

                Vector3 n = Vector3.Cross(cm.vertices[i1]-cm.vertices[i0],
                                        cm.vertices[i2]-cm.vertices[i0]).normalized;
                if (Vector3.Dot(n, kUp) < kNormalThreshold) continue;

                cm.floorFaces.Add(face);
            }
            if (cm.floorFaces.Count == 0) return;

            _cache[mf] = cm;
            if (!_meshFilters.Contains(mf)) _meshFilters.Add(mf);
        }

        #endregion
        /* ================================================================== */
        #region TrackableId helper (unchanged)

        static bool TryGetTrackableId(MeshFilter mf, out TrackableId id)
        {
            id = TrackableId.invalidId;
            string raw = mf.name.StartsWith("Mesh ", StringComparison.Ordinal)
                    ? mf.name[5..] : mf.name;
            raw = raw.Replace("-", "");
            if (raw.Length != 32) return false;
            try {
                ulong hi = ulong.Parse(raw.AsSpan(0,16), NumberStyles.HexNumber);
                ulong lo = ulong.Parse(raw.AsSpan(16,16), NumberStyles.HexNumber);
                id = new TrackableId(hi, lo); return true;
            } catch { return false; }
        }

        #endregion
        /* ================================================================== */
        #region Floor estimation & debug spheres

        void UpdateFloorHeight()
        {
            if (_hist.Count == 0)
            {
                if (showWinnerSpheres) ShowDebugSpheres(null);
                return;
            }

            int bestBin   = int.MinValue;
            int bestVotes = 0;

            foreach (var (bin, votes) in _hist)
            {
                if (votes > bestVotes || (votes == bestVotes && bin > bestBin))
                {
                    bestBin   = bin;
                    bestVotes = votes;
                }
            }

            if (bestBin == int.MinValue || bestVotes < kMinVotes)
            {
                if (showWinnerSpheres) ShowDebugSpheres(null);
                return;
            }

            //FloorY = (bestBin + 0.5f) * kBinSize;
            FloorY = _sumY[bestBin] / bestVotes;;

            if (showWinnerSpheres)
            {
                _binsSamples.TryGetValue(bestBin, out var winSamples);  // ← local var
                ShowDebugSpheres(winSamples);
            }
        }

        void ShowDebugSpheres(List<Vector3>? samples)
        {
            if (!showWinnerSpheres || _debugSpheres == null) return;

            if (samples == null || samples.Count == 0)
            { foreach (var s in _debugSpheres) s.SetActive(false); return; }

            for (int i = 0; i < _debugSpheres.Count; ++i)
            {
                if (i < samples.Count)
                { _debugSpheres[i].transform.position = samples[i];
                _debugSpheres[i].SetActive(true); }
                else _debugSpheres[i].SetActive(false);
            }
        }

        void ShowCandidateSpheres()
        {
            if (!showCandidateSpheres || _candidateSpheres == null) return;

            foreach (var s in _candidateSpheres) s.SetActive(false);

            for (int i = 0; i < _candidatePos.Count && i < _candidateSpheres.Count; ++i)
            {
                _candidateSpheres[i].transform.position = _candidatePos[i];
                _candidateSpheres[i].SetActive(true);
            }
            _candidatePos.Clear();
        }

        #endregion
        /* ================================================================== */
        #region Helper struct

        class CachedMesh
        {
            public Vector3[] vertices;
            public int[]     triangles;
            public List<int> floorFaces;
        }

        #endregion
    }
}
