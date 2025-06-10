using System;
using System.Collections.Generic;
using GeniesIRL.GlobalEvents;
using UnityEngine;
using UnityEngine.XR.ARFoundation;

namespace GeniesIRL 
{
    [RequireComponent(typeof(MultiPointNavTarget))]
    public class Seat : MonoBehaviour
    {
        public event Action<Seat> OnDestroyed;

         /// <summary>
        /// This gets set to true the moment the Seat is assigned to an AR Plane. If we're in a debug environment, where a dev
        /// has spawned the seat manually, this will be false. We use this to determine whether to destroy the Seat if the assigned
        /// AR Plane gets destroyed.
        /// </summary>
        public bool IsTiedToARPlane {get; private set;}

        /// <summary>
        /// The AR Plane this Seat is assigned to. If the ARPlane is destroyed after having been assigned, the SeatProcessor
        /// will destroy the Seat and the Genie may have to cancel sitting on any of its seats.
        /// </summary>
        public ARPlane ARPlane { get; private set; }

        /// <summary>
        /// The exact direction the seat group is facing, based on raycasting. This may not exactly line up with the Seat's rectangular orientation.
        /// </summary>
        public Vector3 ExactSeatingDirection { get; private set; }

        /// <summary>
        /// Processes the ExactSeatingDirection to face in one of the cardinal directions of the rectangle.
        /// </summary>
        public Vector3 CardinalSeatingDirection { get; private set; }

        public MultiPointNavTarget MultiPointNavTarget {
            get {
                if (_multiPointNavTarget == null)
                    _multiPointNavTarget = GetComponent<MultiPointNavTarget>();

                return _multiPointNavTarget;}}

        public SeatValidation.SeatType SeatDirectionalityType { get; private set; }

        /// <summary>
        /// Each seat can have multiple SeatingPositions, which are the actual points where the Genie can sit. The radius here defines the radius of a single
        /// seating position, which is important for pathfinding and determinining if a Genie can get close enough to a Seat to sit down.
        /// </summary>
        public float seatingPositionRadius = 0.075f;

        public SeatValidation seatValidation;

        [Tooltip("The inner cube is scaled to encapsulate the ARPlane. It's used for debugging.")]
        public GameObject innerCube;

        [Header("Debug")]
        public bool enableDebugVisualization = false;
        public GameObject debugArrow;
        public GameObject debugX;
        public Renderer debugCubeRenderer;

        private float _seatHeightOffset;

        private MultiPointNavTarget _multiPointNavTarget;

        public void AssignToARPlane(ARPlane arPlane, float seatHeightOffset)
        {
            ARPlane = arPlane;
            IsTiedToARPlane = true;
            _seatHeightOffset = seatHeightOffset;
            UpdateToMatchPlane();
            
            GlobalEventManager.Subscribe<GeniesIRL.GlobalEvents.DebugShowSeatDebuggers>(DebugOnShowSeatDebuggersBtnClicked);

            UpdateDebugVisualization();
        }

        /// <summary>
        /// The Genie calls this value just before it's about to sit down.
        /// </summary>
        /// <returns></returns>
        public Vector3 EvaluateSeatingDirection(Vector3 seatingPoint)
        {
            if (SeatDirectionalityType == SeatValidation.SeatType.Directional)
            {
                // We've already determined a seating direction.
                return CardinalSeatingDirection;
            }
            else if (SeatDirectionalityType == SeatValidation.SeatType.NonDirectional)
            {
                // The seat group is non-directional, so we're going to figure out the best direction for the Genie to face based on the cardinal
                // direction of the seat rectangle.
                // To do this, get our seating point and determine which edge of the rectangle it's closest to.

                Vector3 localSeatingPoint = transform.InverseTransformPoint(seatingPoint);
                Vector3 halfSize = innerCube.transform.localScale / 2f;

                float distanceToFront = Mathf.Abs(localSeatingPoint.z - halfSize.z);
                float distanceToBack = Mathf.Abs(localSeatingPoint.z + halfSize.z);
                float distanceToLeft = Mathf.Abs(localSeatingPoint.x + halfSize.x);
                float distanceToRight = Mathf.Abs(localSeatingPoint.x - halfSize.x);

                float minDistance = Mathf.Min(distanceToFront, distanceToBack, distanceToLeft, distanceToRight);

                if (Mathf.Approximately(minDistance,distanceToFront))
                {
                    return transform.forward;
                }
                else if (Mathf.Approximately(minDistance,distanceToBack))
                {
                    return -transform.forward;
                }
                else if (Mathf.Approximately(minDistance, distanceToLeft))
                {
                    return -transform.right;
                }
                else if (Mathf.Approximately(minDistance, distanceToRight))
                {
                    return transform.right;
                }
            }

            return transform.forward; // Default to facing forward (this should never happen).
        }

        private void Start() 
        {
            GlobalEventManager.Trigger<NewSeatAppeared>(new NewSeatAppeared());
        }

        private void Update()
        {
            if (IsTiedToARPlane) 
            {
                // If the assigned plane was destroyed or reclassified as a non-seat, destroy this gameObject.
                if (ARPlane == null || !ARPlane.IsSittable())
                {
                    Destroy(gameObject);
                    return;
                }
            }

            UpdateToMatchPlane();

            UpdateDebugVisualization();
        }

        private void UpdateToMatchPlane() 
        {
            if (IsTiedToARPlane) // Normally all seats are tied to an AR Plane, but when debugging, they may have been dragged into the scene manually.
            {
                 // Modify the Seat Group to encapsulate the AR Plane's shape.
                SeatFitter.FitSeatToARPlane(ARPlane, this);
            }
           
            // Next, perform a directional pass to determine which way the Seat Group is facing.
            UpdateSeatDirectionality();

            // Spawn the actual seats that the Genie can sit on.
            List<Vector3> seatingPositions = GenerateSeatingPositions();
            MultiPointNavTarget.SetPoints(seatingPositions.ToArray());
        }

        private void UpdateSeatDirectionality()
        {
            seatValidation.Center = transform.position + Vector3.up * _seatHeightOffset;

            // If the width and length of the inner cube are roughly the same (within 15%), we'll use the larger of the two as the casting radius. Otherwise,
            // we'll use the smaller of the two.
            Vector3 innerCubeScale = innerCube.transform.localScale;
            float width = innerCubeScale.x;
            float length = innerCubeScale.z;

            if (Mathf.Abs(width - length) / Mathf.Max(width, length) <= 0.15f)
            {
                seatValidation.Radius = Mathf.Max(width, length) / 2f;
            }
            else
            {
                seatValidation.Radius = Mathf.Min(width, length) / 2f;
            }

            seatValidation.Radius += 0.2f; // Add a little extra padding to the radius, to increase the odds of hitting something.

            SeatDirectionalityType = seatValidation.Validate(out Vector3 seatingDirection);
            ExactSeatingDirection = seatingDirection;
            CardinalSeatingDirection = GetNearestCardinalDirection(seatingDirection);
        }

        private void UpdateDebugVisualization() 
        {
            if (!IsTiedToARPlane) 
            {
                enableDebugVisualization = true;
            }

            debugCubeRenderer.enabled = enableDebugVisualization;

            debugArrow.SetActive(enableDebugVisualization && SeatDirectionalityType == SeatValidation.SeatType.Directional);
            debugX.SetActive(enableDebugVisualization && SeatDirectionalityType == SeatValidation.SeatType.Unsittable);

            if (!enableDebugVisualization) return;

            if (SeatDirectionalityType == SeatValidation.SeatType.Directional)
            {
                debugArrow.transform.rotation = Quaternion.LookRotation(CardinalSeatingDirection, transform.up);
            }
        }

         private void DebugOnShowSeatDebuggersBtnClicked(DebugShowSeatDebuggers args)
        {
            enableDebugVisualization = args.Show;
        }

        private List<Vector3> GenerateSeatingPositions()
        {
            List<Vector3> seatingPositions = new List<Vector3>();

            if (SeatDirectionalityType == SeatValidation.SeatType.Unsittable)
            {
                return seatingPositions; // This SeatGroup is unsittable -- there's no valid direction for the Genie to face, and so we shouldn't spawn any seats here.
            }

            float seatDiameter = seatingPositionRadius * 2f;

            // In the Editor (without polyspatial), cull tiny little planes that are too small to sit on. On-Device, we'll be more lenient because we want the
            // Genie to be able to sit on even tiny planes, so long as visionOS says it's a seat.
            if (!GeniesIRL.App.XR.IsPolySpatialEnabled) 
            {
                // Before we do anything, make sure the seat has enough room to sit at least one seat.
                if (innerCube.transform.localScale.x < seatDiameter || innerCube.transform.localScale.z < seatDiameter)
                {
                    // It's too small.
                    return seatingPositions;
                }
            }
            
            // Seats get spawned on the edge of the rectangle. If the SeatGroup has directionality, then we'll only spawn on the edge that corresponds to that direction.
            // Otherwise, we'll spawn seats on all edges.

            List<SeatEdge> seatEdges = new List<SeatEdge>();

            if (SeatDirectionalityType == SeatValidation.SeatType.Directional)
            {
                // For directional seats, we'll only need to define one edge of the rectangle to spawn seats along.
                // Now we'll find the edge of the rectangle that most aligns with our previously-calculated seating direction.
                SeatEdge seatEdge = GetSeatEdge(ExactSeatingDirection);
                seatEdges.Add(seatEdge);
            }
            else
            {
                // If we're here, we're a non-directional seat group. We'll spawn seats on all edges of the rectangle.
                seatEdges.Add(GetSeatEdge(transform.forward));
                seatEdges.Add(GetSeatEdge(-transform.forward));
                seatEdges.Add(GetSeatEdge(transform.right));
                seatEdges.Add(GetSeatEdge(-transform.right));
            }

            foreach (var edge in seatEdges)
            {
                Debug.DrawLine(edge.start, edge.end, Color.red);
            }

            foreach (var edge in seatEdges)
            {
                Vector3 edgeDirection = (edge.end - edge.start).normalized;
                float edgeLength = Vector3.Distance(edge.start, edge.end);
                int seatCount = Mathf.FloorToInt(edgeLength / seatDiameter);

                if (seatCount > 0)
                {
                    float totalSeatingLength = seatCount * seatDiameter;
                    float margin = (edgeLength - totalSeatingLength) / 2f;

                    for (int i = 0; i < seatCount; i++)
                    {
                        float distanceFromStart = margin + (i * seatDiameter) + (seatDiameter / 2f);
                        Vector3 seatingPosition = edge.start + edgeDirection * distanceFromStart;

                        // Translate the seating position inside the seat group
                        seatingPosition -= edge.directionFromSeatCenter.normalized * (seatDiameter / 2f);

                        if (IsTiedToARPlane) // In most cases this will be true, but in debug environments, it may not be.
                        {
                            // Peform an extra check to make sure the center of the seating position is at least inside the plane shape.
                            // (This is more likely to be a problem if the seating position is near the corner of the rectangle.)
                            if (!IsSeatingPositionInsideARPlaneShape(seatingPosition)) continue;
                        }
                        
                        seatingPositions.Add(seatingPosition);
                    }
                }
            }

            foreach (var seatingPosition in seatingPositions)
            {
                DebugUtils.DrawPolygon(seatingPosition, seatDiameter / 2f, 8, Color.green);
            }

            return seatingPositions;
        }

        private bool IsSeatingPositionInsideARPlaneShape(Vector3 seatingPosition)
        {
            seatingPosition = ARPlane.transform.InverseTransformPoint(seatingPosition);
            Vector2 seatingPosition2D = new Vector2(seatingPosition.x, seatingPosition.z);

            Vector2[] arPlanePoints = new Vector2[ARPlane.boundary.Length];

            for (int i = 0; i < ARPlane.boundary.Length; i++)
            {
                arPlanePoints[i] = new Vector2(ARPlane.boundary[i].x, ARPlane.boundary[i].y);
            }

            return PolygonTester.IsPointInConvexPolygon(arPlanePoints, seatingPosition2D);
        }

        private SeatEdge GetSeatEdge(Vector3 direction)
        {
            Vector3 cardinalDirection = GetNearestCardinalDirection(direction);

            Vector3 center = transform.position;
            Vector3 halfSize = innerCube.transform.localScale / 2f;

            Vector3 startPoint = Vector3.zero;
            Vector3 endPoint = Vector3.zero;

            if (VectorUtils.Approximately(cardinalDirection,transform.forward))
            {
                startPoint = center + transform.rotation * new Vector3(-halfSize.x, 0, halfSize.z);
                endPoint = center + transform.rotation * new Vector3(halfSize.x, 0, halfSize.z);
                
            }
            else if (VectorUtils.Approximately(cardinalDirection, -transform.forward))
            {
                startPoint = center + transform.rotation * new Vector3(halfSize.x, 0, -halfSize.z);
                endPoint = center + transform.rotation * new Vector3(-halfSize.x, 0, -halfSize.z);
            }
            else if (VectorUtils.Approximately(cardinalDirection,transform.right))
            {
                startPoint = center + transform.rotation * new Vector3(halfSize.x, 0, halfSize.z);
                endPoint = center + transform.rotation * new Vector3(halfSize.x, 0, -halfSize.z);
            }
            else if (VectorUtils.Approximately(cardinalDirection,-transform.right))
            {
                startPoint = center + transform.rotation * new Vector3(-halfSize.x, 0, -halfSize.z);
                endPoint = center + transform.rotation * new Vector3(-halfSize.x, 0, halfSize.z);
            }
            else
            {
                Debug.LogError("Invalid cardinal direction.");
            }

            return new SeatEdge(startPoint, endPoint, cardinalDirection);
        }

        private Vector3 GetNearestCardinalDirection(Vector3 exactDirection)
        {
            Vector3[] directions = { transform.forward, -transform.forward, transform.right, -transform.right };
            float maxDot = float.MinValue;
            Vector3 bestDirection = Vector3.forward;

            foreach (var direction in directions)
            {
                float dot = Vector3.Dot(exactDirection, direction);
                if (dot > maxDot)
                {
                    maxDot = dot;
                    bestDirection = direction;
                }
            }

            return bestDirection;
        }

        private struct SeatEdge
        {
            public Vector3 start;
            public Vector3 end;
            public Vector3 directionFromSeatCenter;

            public SeatEdge(Vector3 start, Vector3 end, Vector3 directionFromSeatGroupCenter)
            {
                this.start = start;
                this.end = end;
                this.directionFromSeatCenter = directionFromSeatGroupCenter;
            }
        }
    }

}
