using System;
using System.Collections.Generic;
using System.Linq;
using Unity.AppUI.UI;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL 
{
    /// <summary>
    /// Attached to a prefab which spawns at runtime when a window plane is detected. Can also be dragged into the scene manually
    /// by a developer for testing. Used to determine proper Genie standing position for looking outside.
    /// </summary>
    public class Window : MonoBehaviour
    {
        [Tooltip("Each side of the window has a margin to prevent the Genie from standing too close to the edge.")]
        public float margins = 0.25f;

        private float _floorY = 0;

        private bool _isTiedToARPlane = false;

        /// <summary>
        /// This is only called when the window is spawned at runtime by WindowProcessor.
        /// </summary>
        /// <param name="plane"></param>
        /// <exception cref="System.NotImplementedException"></exception>
        public void Initialize(ARPlane plane, bool isDebugMode)
        {
            transform.position = plane.center;
            transform.LookAt(plane.center + plane.normal, Vector3.up);
            transform.eulerAngles = new Vector3(0, transform.eulerAngles.y, 0); // Make sure we're perfectly, vertically aligned.

            // Scale the window to fit the plane.
            transform.localScale = new Vector3(plane.size.x, plane.size.y, transform.localScale.z);

            _isTiedToARPlane = true;

            if (!isDebugMode)
            {
                // Disable the renderer while not in Debug mode.
                Renderer renderer = GetComponent<Renderer>();
                renderer.enabled = false;
            }
        }

        /// <summary>
        /// Determines whether the Genie can navigate to a valid standing position to look out the window. Returns false if no valid position is found.
        /// </summary>
        /// <param name="position"></param>
        /// <returns></returns>
        public bool EvaluateStandingPosition(Genie genie, Transform userHead, out Vector3 position)
        {
            GenieNavigation nav = genie.genieNavigation;

            _floorY = AstarPath.active.data.gridGraph.center.y; // The floor Y position is the grid graph Y.

            // Get a list of points along the edge of the window.
            List<Vector3> pointsAlongEdge = GetPointsAlongEdge();

            // Randomly evaluate points along the edge of the window.
            while (pointsAlongEdge != null && pointsAlongEdge.Count > 0)
            {
                UnityEngine.Random.InitState((int)DateTime.Now.Ticks);
                int randomIndex = UnityEngine.Random.Range(0, pointsAlongEdge.Count);

                Vector3 targetPoint = pointsAlongEdge[randomIndex];
                pointsAlongEdge.RemoveAt(randomIndex);

                // Check if the user is too close to this position.
                if (VectorUtils.IsWithinDistanceXZ(targetPoint, userHead.position, nav.Genie.genieSense.personalSpace.BaseRadius))
                {
                    continue;
                }

                float maxStandingDistance = genie.genieBrain.genieBeliefs.maxWindowStandingDistance;
                float idealStandingDistance = genie.genieBrain.genieBeliefs.idealWindowStandingDistance;

                if (NavArrivalEvaluation.EvaluateIfPossible(NavArrivalEvaluationType.BestPointOnLineSegment, genie, targetPoint, 
                        maxStandingDistance, idealStandingDistance, transform.forward))
                {
                    position = targetPoint;
                    return true;
                }
            }

            position = default;
            return false;
        }

        private List<Vector3> GetPointsAlongEdge()
        {
            float width = transform.localScale.x - margins * 2f;
            float interval = AstarPath.active.data.gridGraph.nodeSize;
            int count = Mathf.FloorToInt(width / interval);

            Vector3[] points = new Vector3[count];

            for (int i = 0; i < count; i++)
            {
                points[i] = transform.position - transform.right * (width / 2f) + transform.right * (i * interval);
                points[i].y = _floorY;
            }

            return points.ToList<Vector3>();
        }

        private void Start ()
        {
            if (!_isTiedToARPlane)
            {
                // This means the window was manually placed in the scene by a developer for testing, or it was spawned
                // by the XRImageTrackingObjectManager. Because there's no ARPlane, we have call the Global Event here 
                // to alert the Genie.
                GlobalEventManager.Trigger(new GlobalEvents.NewWindowAppeared());
            }
        }
    }
}

