using System;
using System.Collections.Generic;
using GeneisIRL;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Used by the GenieBrain to keep track of what the Genie believes about the world. This will likely be informed 
    /// by a script called GenieSense at some point in the future.
    /// </summary>
    [System.Serializable]
    public class GenieBeliefs
    {
        public Transform UserHead {get; private set;}

        /// <summary>
        /// Fires when the GenieSense senses an impact from an item, with the bool parameter being true if the impact came from the front of the Genie. (In theory we could just have the GenieArbiter listen for this event directly in GenieSense, but I 
        /// like the idea of Beliefs being based upon Senses, and the Arbiter using that knowledge to actually decide what to do. We'll see how this goes...)
        /// </summary>
        public event Action<Item, bool> OnItemImpact;

         /// <summary>
        /// Fires the moment a new item is offered AND in the Genie's view. (In theory we could just have the GenieArbiter listen for this event directly in GenieSense, but I 
        /// like the idea of Beliefs being based upon Senses, and the Arbiter using that knowledge to actually decide what to do. We'll see how this goes...)
        /// </summary>
        public event Action<Item> OnGenieNoticesItemOffered;

        /// <summary>
        /// Fires the moment the Genie notices the user offering a high five. The argument is the user's hand transform.
        /// </summary>
        public event Action<Transform> OnGenieNoticesHighFiveSolicitation;

        /// <summary>
        /// Gets set every time the Genie is struck by a user projectile. True means the impact was from the front, false means behind.
        /// </summary>
        public bool IsLatestUserProjectileImpactFromFront {get; private set;}

        [Tooltip("The extra distance to add to the radius of a seat when determining if the Genie has arrived at the seat.")]
        public float seatArrivalThreshold = 0.5f;
        [Range(0.25f, 2f)]
        public float pencilThrowingArrivalThreshold = 0.25f;

        [Tooltip("Offsets the box away from the window in the z-direction. Should always be less than the maxWindowStandingDistance.")]
        [Range(0f, 2f)]
        public float idealWindowStandingDistance = 0.17f;

        [Tooltip("Determines the space in front of the window that the Genie is allowed to stand in when looking out. Should always be greater than the idealWindowStandingDistance.")]
        [Range(0f, 2f)]
        public float maxWindowStandingDistance = 0.37f;

        [NonSerialized] // Gotta put this here or Unity will secretly serialize it and create a circular reference issue.
        private GenieBrain _genieBrain;
        [NonSerialized] GenieSense _genieSense;
        [NonSerialized]
        private ARSurfaceUnderstanding _arSurfaceUnderstanding;
        private List<Seat> _seatsISatOn = new List<Seat>();
        private List<Window> _windowsIAdmired = new List<Window>();

        public void Initialize(GenieBrain genieBrain)
        {
            _genieBrain = genieBrain;
            _arSurfaceUnderstanding = _genieBrain.Genie.GenieManager.Bootstrapper.ARSurfaceUnderstanding;
            
            UserHead = _genieBrain.Genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper.Head;
            _genieSense = _genieBrain.Genie.genieSense;
            _genieSense.detectImpactFromUserProjectile.OnItemImpact += OnItemImpactHandler;
            _genieSense.detectUserOfferingItem.OnGenieNoticesItemOffered += OnGenieNoticesItemOfferedHandler;
            _genieSense.detectUserSolicitingHighFive.OnGenieNoticesHighFiveSolicitation += OnGenieNoticesHighFiveSolicitationHandler;

            Debug.Log("GenieBeliefs initialized. ARSurfaceUnderstanding is " + _arSurfaceUnderstanding);
        }

        /// <summary>
        /// Checks if there's a seat in the environment that the Genie can sit on and returns one. In the future, this may 
        /// include some additional processing based on distance, etc.
        /// </summary>
        /// <returns></returns>
        public bool IsThereASeatToSitOn(out Seat seat) 
        {
            if (_arSurfaceUnderstanding == null) 
            {
                Debug.LogError("ARSurfaceUnderstanding is null.");
            }

            List<Seat> seats = _arSurfaceUnderstanding.seatProcessor.FindSeats();
                    
            if (seats == null || seats.Count == 0)
            {
                seat = null;
                return false;
            }

            seats = SortSeatsByBasedOnSeatingHistory(seats);

            // Scan for seats that are reachable and not too close to the user.
            for (int i=0; i <seats.Count; i++)
            {
                 // If this seat is not on the same level (i.e. floor) as the Genie, skip it.
                if (!IsPointOnSameLevelAsMe(seats[i].transform.position)) continue;

                // Each seat potentially has multiple points that the Genie can navigate to.
                Vector3[] points = seats[i].MultiPointNavTarget.Points;

                for (int j=0; j < points.Length; j++) 
                {
                     // If a user is sitting on the seat (or too close to it), skip it.
                    if (IsUserWithinPersonalSpaceOfPoint(points[j], PersonalSpace.PersonalSpaceType.Base))
                    {
                        Debug.Log("Detected user sitting on seat position.");
                        continue;
                    }
                    
                    // Check to see if it's pathable.
                    float arrivalDist = seats[i].seatingPositionRadius + seatArrivalThreshold; // Radius of the seat, plus an extra threshold.

                    if (_genieBrain.Genie.genieNavigation.IsPathReachable(points[j], arrivalDist)) 
                    {
                        Debug.Log("Path to seat reachable.");
                        seat = seats[i];
                        return true;
                    }
                }
            }

            seat = null;
            return false;
        }

        // Checks for drawing spaces on walls in the environment that the Genie can draw on. Filters drawing spaces that are 
        // too close to the user, not reachable, or are too close to another drawing.
        public bool IsThereASpaceToDrawOn(out DrawingSpace drawingSpace) 
        {
            DrawingSpace[] drawingSpacesAsArray = _arSurfaceUnderstanding.wallProcessor.GenerateAndFindAvailableDrawingSpaces();
            List<DrawingSpace> drawingSpaces = new List<DrawingSpace>(drawingSpacesAsArray);

            if (drawingSpaces == null || drawingSpaces.Count == 0)
            {
                drawingSpace = null;
                return false;
            }

            Debug.Log("Drawing spaces found: " + drawingSpaces.Count);

            float maxDistFromUser = Mathf.Max(0.5f, _genieBrain.Genie.genieSense.personalSpace.BaseRadius);

            while (drawingSpaces != null && drawingSpaces.Count > 0)
            {
                 // Pick a random Drawing Space to evaluate.
                drawingSpace = drawingSpaces[UnityEngine.Random.Range(0, drawingSpaces.Count)];
                
                if (!drawingSpace.Evaluate())
                {
                    // Drawing space invalid. Remove it from the list and try again.
                    drawingSpaces.Remove(drawingSpace);
                    continue;
                }

                // Get the position/rotation that the actual drawing would go.
                Pose drawingPose = drawingSpace.DrawingPose;

                // If the drawing space is not on the same level as the Genie, skip it.
                if (!IsPointOnSameLevelAsMe(drawingPose.position))
                {
                    drawingSpaces.Remove(drawingSpace);
                    continue;
                }

                // Is the user too close to the drawing space? (We don't want them to pick a space that the user is standing right next to)
                if (VectorUtils.IsWithinDistanceXZ(UserHead.position, drawingPose.position, maxDistFromUser))
                {
                    // Too close. Remove it from the list and try again.
                    drawingSpaces.Remove(drawingSpace);
                    continue;
                }
                
                // Can the genie reach this point?
                if (!NavArrivalEvaluation.EvaluateIfPossible(NavArrivalEvaluationType.BestPointOnLineSegment, _genieBrain.Genie, 
                    drawingPose.position, _genieBrain.Genie.genieDraw.maxDrawingDistance, _genieBrain.Genie.genieDraw.idealDrawingDistance, drawingPose.forward))
                {
                    // Unreachable space. Remove it from the list and try again.
                    drawingSpaces.Remove(drawingSpace);
                    continue;
                }
                
                // If we've made it here, the drawing space is valid and we can use it.
                return true;
            }

            // We've run out of drawing spaces to evaluate. There's nothing to draw on.
            drawingSpace = null;
            return false;
        }

        public bool IsThereAWindowToLookOutFrom(out Window window, out Vector3 standingPosition)
        {
            if (_arSurfaceUnderstanding == null) 
            {
                Debug.LogError("ARSurfaceUnderstanding is null.");
            }

            List<Window> windows = _arSurfaceUnderstanding.windowProcessor.FindWindows();

            if (windows == null || windows.Count == 0)
            {
                window = null;
                standingPosition = default;
                return false;
            }

            Debug.Log("Found " + windows.Count + " windows.");

            windows = SortWindowsByBasedOnAdmirationHistory(windows);

            for (int i=0; i < windows.Count; i++)
            {
                window = windows[i];

                // If this window is not on the same level (i.e. floor) as the Genie, skip it.
                if (!IsPointOnSameLevelAsMe(window.transform.position)) continue;

                // Evaluate the standing position.
                if (window.EvaluateStandingPosition(_genieBrain.Genie, UserHead, out standingPosition))
                {
                    return true;
                }
            }
            
            // Nothing valid found.
            window = null;
            standingPosition = default;
            return false;
        }

        /// <summary>
        /// Returns true if the user is within the Genie's personal space. The reason GenieBeliefs 'forwards' this from GenieSense is to uphold a philosophy
        /// of 'Sense informing Beliefs' rather than GenieBrain referencing GenieSense directly. I'm not sure if this is the best way to do it though.
        /// </summary>
        /// <param name="extraBufferSpace">Expands the personal space to help with some calculations.</param>
        /// <returns></returns>
        public bool IsUserWithinPersonalSpace() 
        {
            return _genieSense.personalSpace.IsUserTooClose();
        }

        /// <summary>
        /// Imagines the Genie at the given point, and returns true if the User would be within their personal space. Useful for preventing the Genie from
        /// moving to a point that would put the User too close.
        /// </summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public bool IsUserWithinPersonalSpaceOfPoint(Vector3 point, PersonalSpace.PersonalSpaceType personalSpaceType) 
        {
            if (personalSpaceType == PersonalSpace.PersonalSpaceType.None)
            {
                Debug.LogError("Invalid personal space type. You must specify a personal space type other than 'None'.");
                return false;
            }

            float distanceThreshold = _genieSense.personalSpace.BaseRadius;

            if (personalSpaceType == PersonalSpace.PersonalSpaceType.IntentionalPhysicalContact)
            {
                distanceThreshold = PersonalSpace.kRadiusDuringIntentionalPhysicalContact;
            }
            else if (personalSpaceType == PersonalSpace.PersonalSpaceType.NavigatingIrl)
            {
                distanceThreshold = PersonalSpace.kRadiusDuringNavigatingIrlActions;
            }

            return VectorUtils.IsWithinDistanceXZ(UserHead.position, point, distanceThreshold);
        }

        /// <summary>
        /// Scans the world for an item that is:
        ///     - Not currently held by the User or Genie
        ///     - Is Reachable by the Genie
        ///     - Has the GenieGrabbable component
        /// </summary>
        /// <param name="item"></param>
        /// <returns></returns>
        public bool IsThereAnItemICanGrab(out Item item)
        {
            Item[] itemsAsArray = GameObject.FindObjectsByType<Item>(FindObjectsSortMode.None);

            if (itemsAsArray == null || itemsAsArray.Length == 0)
            {
                item = null;
                return false;
            }

            List<Item> items = new List<Item>(itemsAsArray);

            while (items.Count > 0)
            {
                // Pick a random item to evaluate.
                int idx = UnityEngine.Random.Range(0, items.Count);

                if (!items[idx].IsGrabbableByGenie) 
                {
                    items.RemoveAt(idx);
                    continue; // Ignore items that Genie aren't allowed to grab.
                }

                Item.ItemState state = items[idx].state;

                // Ensure it's in one of the valid states.
                bool isValid = state == Item.ItemState.PlacedByAutoSpawner
                    || state == Item.ItemState.DroppedByGenie
                    || state == Item.ItemState.DroppedByUserAndAtRest;

                if (!isValid) 
                {
                    items.RemoveAt(idx);
                    continue;
                }

                // Ensure that the user isn't too close to this item.
                if (VectorUtils.IsWithinDistanceXZ(items[idx].transform.position, UserHead.position, _genieBrain.Genie.genieSense.personalSpace.BaseRadius))
                {
                    items.RemoveAt(idx);
                    continue;
                }

                // Ensure that the item is on the same level as the Genie.
                if (!IsPointOnSameLevelAsMe(items[idx].transform.position))
                {
                    items.RemoveAt(idx);
                    continue;
                }
                
                // Can the Genie reach this item?
                float reachDist = _genieBrain.Genie.genieGrabber.grabReach;

                //if (!_genieBrain.Genie.genieNavigation.IsPathReachable(items[idx].transform.position, reachDist))
                if (!NavArrivalEvaluation.EvaluateIfPossible(NavArrivalEvaluationType.ReachableWithArms, _genieBrain.Genie, items[idx].transform.position))
                {
                    items.RemoveAt(idx);
                    continue;
                }

                // We've found our item!
                item = items[idx];
                return true;
            }
            
            // Valid item not found.
            item = null;
            return false;
        }
        
        /// <summary>
        /// Scans for celing planes and tries to find a place where the Genie can throw a pencil and have it stick to the ceiling.
        /// </summary>
        /// <param name="pencilTargetPosition"></param>
        public bool IsThereAPlaceOnTheCeilingToThrowAPencil(out Vector3 pencilTarget)
        {
            List<Vector3> potentialTargets = _arSurfaceUnderstanding.ceilingProcessor.FindRandCeilingTargets();
            pencilTarget = default;

            if (potentialTargets == null || potentialTargets.Count == 0)
            {
                return false;
            }

            LayerMask spatialMeshLayerMask = LayerMask.GetMask("SpatialMesh");

            while (potentialTargets.Count > 0)
            {
                // Pick a random target to evaluate.
                int idx = UnityEngine.Random.Range(0, potentialTargets.Count);

                // Make sure that this point is definitely above us, but not SO high that it could be a ceiling on another floor.
                if (potentialTargets[idx].y < UserHead.position.y || potentialTargets[idx].y > UserHead.position.y + 3f)
                {
                    potentialTargets.RemoveAt(idx);
                    continue;
                }

                // Get the point where the genie must stand to throw the pencil.
                Vector3 walkTarget = potentialTargets[idx];
                walkTarget.y = _genieBrain.Genie.GenieManager.Bootstrapper.XRNode.xrFloorManager.FloorY;

                // Is the user too close to to this walk target (We don't want them to pick a space that the user is standing right next to)
                if (VectorUtils.IsWithinDistanceXZ(UserHead.position, walkTarget, _genieSense.personalSpace.BaseRadius))
                {
                    // Too close. Remove it from the list and try again.
                    potentialTargets.RemoveAt(idx);
                    continue;
                }

                // Can the Genie reach this point?
                if (!_genieBrain.Genie.genieNavigation.IsPathReachable(walkTarget, pencilThrowingArrivalThreshold))
                {
                    potentialTargets.RemoveAt(idx);
                    continue;
                }

                // Verify that there is, indeed ceiling geometry to hit.

                Ray ray = new Ray(potentialTargets[idx] - Vector3.up * 0.1f, Vector3.up);
                
                if (!Physics.Raycast(ray, 5f, spatialMeshLayerMask))
                {
                    potentialTargets.RemoveAt(idx);// No spatial mesh hit. Remove it from the list and try again.
                    continue;
                }

                // We've found a target!
                pencilTarget = potentialTargets[idx];
                return true;
            }

            // Valid target not found.
            return false;
        }

        /// <summary>
        /// Fires when the Genie sits on a seat. Used to track the Genie's recent seating history.
        /// </summary>
        /// <param name="seat"></param>
        public void OnSatOnSeat(Seat seat)
        {
            if (_seatsISatOn.Contains(seat))
            {
                _seatsISatOn.Remove(seat);
            }

            _seatsISatOn.Add(seat);
        }

        /// <summary>
        /// Fires when the Genie admires a window. Used to track the Genie's recent window admiration history.
        /// </summary>
        /// <param name="window"></param>
        public void OnAdmiredWindow(Window window)
        {
            if (_windowsIAdmired.Contains(window))
            {
                _windowsIAdmired.Remove(window);
            }

            _windowsIAdmired.Add(window);
        }

        /// <summary>
        /// Called by the PlanDecider when a Plan succeeds, before WrapUp is called. This allows us to access details about the Plan that was accomplished, so we
        /// can remember it and use it for further decision-making.
        /// </summary>
        /// <param name="plan"></param>
        /// <param name="planDecider"></param>
        public void OnPlanSucceeded(GeniePlan plan, PlanDecider planDecider)
        {
            switch (plan){
                case GeniePlan.TakeASeat:
                    OnSatOnSeat(planDecider.TargetSeat);
                    break;
                case GeniePlan.AdmireWindow:
                    OnAdmiredWindow(planDecider.TargetWindow);
                    break;
            }
        }

        /// <summary>
        /// // In the case of multi-floor buildings, we need to check the Y position of the point to see if it's on the same flooor.
        // For example, We don't want the Genie to try to sit on a seat that's above over below them.
        /// </summary>
        /// <param name="point"></param>
        /// <param name="maxYOffFloor"></param>
        /// <returns></returns>
        public bool IsPointOnSameLevelAsMe(Vector3 point, float maxYOffFloor = 2f) 
        {
            // Get the Y position of the floor.
            float floorY = _genieBrain.Genie.GenieManager.Bootstrapper.XRNode.xrFloorManager.FloorY;

            if (point.y < floorY || point.y > floorY + maxYOffFloor)
            {
                return false; // Point is below this floor, or above the max Y
            }

            return true; // Point is on the same level as the Genie.

        }

        private void OnItemImpactHandler(Item item, bool isFront)
        {
            IsLatestUserProjectileImpactFromFront = isFront;
            OnItemImpact?.Invoke(item, isFront);
        }

        private void OnGenieNoticesItemOfferedHandler(Item item)
        {
            Debug.Log("Genie sees item offered.");
            OnGenieNoticesItemOffered?.Invoke(item);
        }

        private void OnGenieNoticesHighFiveSolicitationHandler(Transform userHandTransform)
        {
            Debug.Log("Genie sees high five solicitation.");
            OnGenieNoticesHighFiveSolicitation?.Invoke(userHandTransform);
        }

        private List<Seat> SortSeatsByBasedOnSeatingHistory(List<Seat> seats)
        {
            // Start by pruning all seats from _seatsISatOn that are no longer in the scene.
            _seatsISatOn.RemoveAll(s => s == null);

            // Initialize the list of seats to return.
            List<Seat> sortedSeats = new List<Seat>();

            // First include any seats that we haven't yet sat on.
            foreach (Seat seat in seats) 
            {
                if (seat == null) continue; // This shouldn't ever be null, but just in case, skip it.

                if (!_seatsISatOn.Contains(seat))
                {
                    sortedSeats.Add(seat);
                }
            }

            sortedSeats.AddRange(_seatsISatOn); // Add seats we've already sat on, from least to most recent

            return sortedSeats;
        }

        private List<Window> SortWindowsByBasedOnAdmirationHistory(List<Window> windows)
        {
            // Start by pruning any windows from _windowsIAdmired that are no longer in the scene.
            _windowsIAdmired.RemoveAll(w => w == null);
            
            // Initialize the list of windows to return.
            List<Window> sortedWindows = new List<Window>();

            // First include any windows that we haven't yet admired.
            foreach (Window window in windows) 
            {
                if (window == null) continue; // This shouldn't ever be null, but just in case, skip it.

                if (!_windowsIAdmired.Contains(window))
                {
                    sortedWindows.Add(window);
                }
            }

            sortedWindows.AddRange(_windowsIAdmired); // Add windows we've already admired, from least to most recent

            return sortedWindows;
        }
        
    }
}
