using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// As of May 15, 2025, there is a bug in PolySpatial  that prevents characters with multiple skinned mesh renderers from
    /// properly updating while moved. This script searches for any child skinned mesh renderers and modifies them on launch
    /// to prevent the glitch.
    /// It's recommended that you also turn on Update
    /// </summary>
    [DefaultExecutionOrder(-9000)]
    public class PolySpatialMeshRendererBugWorkAround : MonoBehaviour
    {
        private void Awake() {
            SkinnedMeshRenderer[] skinnedMeshRenderers = GetComponentsInChildren<SkinnedMeshRenderer>();

            foreach (var smr in skinnedMeshRenderers){
                var b = smr.localBounds;
                b.extents = Vector3.one * 10f;   // big enough for any pose
                smr.localBounds = b;
                smr.updateWhenOffscreen = true; // also bypasses culling
                smr.motionVectorGenerationMode = MotionVectorGenerationMode.ForceNoMotion;
            }

            Debug.Log("Set up " + skinnedMeshRenderers.Length + " SkinnedMeshRenderers to work around PolySpatial bug.");
        }
    }
}

