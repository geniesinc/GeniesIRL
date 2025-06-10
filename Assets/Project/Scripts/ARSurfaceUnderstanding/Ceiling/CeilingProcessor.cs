using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.XR.ARFoundation;
using UnityEngine.XR.ARSubsystems;

namespace GeniesIRL 
{
    [System.Serializable]
    public class CeilingProcessor
    {
        private ARPlaneManager _arPlaneManager;

        public void OnSceneBootstrapped(ARPlaneManager arPlaneManager)
        {
            _arPlaneManager = arPlaneManager;
        }

        /// <summary>
        /// Written with the "pencil" action in mind, finds a point on an IRL that the Genie can stand directly underneath, to throw a pencil at.
        /// </summary>
        /// <returns></returns>
        public List<Vector3> FindRandCeilingTargets(int maxTries = 10, float minDistBetween = 0.5f)
        {
            List<Vector3> randCeilingTargets = new List<Vector3>();

            List<ARPlane> ceilingPlanes = FindCeilingPlanes();

            int tries = 0;

            while (tries < maxTries && ceilingPlanes.Count > 0)
            {
                tries++;
                
                // Select a random ceiling plane.
                ARPlane ceilingPlane = ceilingPlanes[UnityEngine.Random.Range(0, ceilingPlanes.Count)];

                Vector3 randPoint = ceilingPlane.GetRandomPointOnARPlane();

                // Make sure the point is far enough from other points.
                bool isFarEnough = true;

                foreach (Vector3 point in randCeilingTargets)
                {
                    if ((randPoint - point).sqrMagnitude < minDistBetween * minDistBetween)
                    {
                        isFarEnough = false;
                        break;
                    }
                }

                if (!isFarEnough)
                {
                    continue;
                }

                randCeilingTargets.Add(randPoint);
            }

            return randCeilingTargets;
        }

        private List<ARPlane> FindCeilingPlanes() 
        {
            List<ARPlane> ceilingPlanes = new List<ARPlane>();

            foreach (ARPlane plane in _arPlaneManager.trackables)
            {
                if (IsCeiling(plane))
                {
                    ceilingPlanes.Add(plane);
                }
            }

            return ceilingPlanes;
        }

        private bool IsCeiling(ARPlane plane)
        {
            if (GeniesIRL.App.XR.IsPolySpatialEnabled) 
            {
                return plane.classifications == PlaneClassifications.Ceiling;  
            }
            
            // At the time of writing, we can't do plane classifications in the Editor unless we're using 
            // PlayToDevice. In other words, we cannot use PlaneClassifications unless Polyspatial is enabled.

            // Make sure the plane is mostly facing down.
            float absDot = Vector3.Dot(plane.normal, Vector3.down);
            return absDot > 0.25f; // (We're going to be pretty lenient here and keep this threshold low).
        }
    }
}

