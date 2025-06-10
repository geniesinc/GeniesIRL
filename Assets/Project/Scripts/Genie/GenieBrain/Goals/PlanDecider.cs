using System;
using System.Collections;
using System.Collections.Generic;
using GeniesIRL;
using UnityEngine;
using UnityEngine.Serialization;

namespace GeneisIRL
{
    /// <summary>
    /// Responsible for priortizing plans and deciding which plans to pursue.
    /// </summary>
    [System.Serializable]
    public class PlanDecider
    {
        public enum PlanSuccessState  { None, Success, Failure}
        public event Action<GeniePlan> OnPlanChanged;
        public Item TargetItem {get; set;}
        public Seat TargetSeat {get; set;}
        public float MaxSeatArrivalDistance {get; set;}
        public float IdealSeatArrivalDistance {get; set;}
        public DrawingSpace TargetDrawingSpace {get; set;}
        public Vector3 TargetDrawingPosition {get; set;} 
        public Vector3 DesiredItemPlacement {get; set;}
        public Window TargetWindow {get; private set;}
        public Vector3 DesiredWindowStandingPosition {get; private set;}
        public Vector3 DesiredPencilThrowingStandingPosition {get; private set;}
        public GeniePlan CurrentPlan {
            get => _currentPlan;
            set 
            {
                if (_currentPlan != value) 
                {
                    _currentPlan = value;
                    OnPlanChanged?.Invoke(_currentPlan);
                }
            }
        }
        public Vector3 NavTargetLineSegmentDirection {get; private set;}
        [FormerlySerializedAs("OnFinishedGoalEventChannel")]
        public OnFinishedPlan OnFinishedPlanEventChannel;
        [FormerlySerializedAs("OnFailedGoalEventChannel")]
        public OnFailedPlan OnFailedPlanEventChannel;

        [FormerlySerializedAs("genieDesires")]
        public GenieGoals genieGoals;

        [Header("Debug")]
        [SerializeField, Tooltip("When ticked, press 'I' to interrupt the current Plan and Action.")]
        private bool debugInterruptions = false;

        [SerializeField, Tooltip("When set to anything other than 'None', this will force the Genie to pursue the given plan.")]  
        [FormerlySerializedAs("debugForceGoal")]
        private GeniePlan debugForcePlan = GeniePlan.None;

        [NonSerialized] // Gotta put this here or Unity w ill secretly serialize it and create a circular reference issue.
        private GenieBrain _genieBrain;

        private FloorManager _xRFloorManager;

        /// <summary>
        /// Please don't use this directly. Use the public property CurrentPlan instead, which is set up to fire events when the plan changes.
        /// </summary>
        private GeniePlan _currentPlan;

        // These are plans that the Genie should not initiate on its own -- they should only occur as a response to events, such
        // as those from the user.
        private List<GeniePlan> plansThatGenieShouldNotInitiate = new List<GeniePlan> {
            GeniePlan.MaintainPersonalSpace,
            GeniePlan.ReactToProjectileImpact,
            GeniePlan.AcceptItemBeingOffered,
            GeniePlan.RespondToUserHighFiveSolicitation,
        };
        
        public void Initialize(GenieBrain genieBrain)
        {
            _genieBrain = genieBrain;
            _xRFloorManager = genieBrain.Genie.GenieManager.Bootstrapper.XRNode.xrFloorManager;
            _genieBrain.genieBeliefs.OnItemImpact += OnItemImpact;
            _genieBrain.genieBeliefs.OnGenieNoticesItemOffered += OnGenieNoticesItemOffered;
            _genieBrain.genieBeliefs.OnGenieNoticesHighFiveSolicitation += OnGenieNoticesHighFiveSolicitationFromUser;

            OnFinishedPlanEventChannel.Event += OnFinishedPlan; 
            OnFailedPlanEventChannel.Event += OnFailedPlan;

            genieGoals.Initialize();

            CurrentPlan = EvaluateNextPlanAndPerformSetup(); // Kick things off with the first plan
        }

        public void OnUpdate()
        {
            // When debugInterruptions is enabled, you can press 'I' to interrupt the current plan and action.
            if (debugInterruptions)
            {
                DebugInterruptions();
            }

            UpdatePersonalSpaceMaintenance();

            // In case we somehow are without an active plan, do an evaluation now.
            if (CurrentPlan == GeniePlan.None)
            {
                CurrentPlan = EvaluateNextPlanAndPerformSetup(GeniePlan.LookAtAndTrackUser);
            }
        }

        public void OnGenieTeleported() 
        {
            // After a teleportation event, we need to cancel whatever plan the Genie had been pursuing.
            InterruptWithSomethingSimple();
        }

        private void DebugInterruptions()
        {
            if (Input.GetKeyDown(KeyCode.I)) 
            {
                Debug.Log("Interrupting current Plan.");

                InterruptWithSomethingSimple();
            }
        }

        // At present, you cannot cancel a plan without replacing it with a different plan. (I.e., you cannot restart
        // the same plan.) So, if we want to interrupt the current plan with something simple, we'll switch to either LookAtAndTrackUser
        // or IdleZoneOut -- whichever is *not* the current plan.
        private void InterruptWithSomethingSimple() 
        {
            WrapUpCurrentPlan();

            if (CurrentPlan != GeniePlan.LookAtAndTrackUser) 
            {
                CurrentPlan = EvaluateNextPlanAndPerformSetup(GeniePlan.LookAtAndTrackUser);
            }
            else 
            {
                CurrentPlan = EvaluateNextPlanAndPerformSetup(GeniePlan.IdleZoneOut);
            }
        }

        // Fires when a Plan finishes.
        private void OnFinishedPlan(GeniePlan plan)
        {
            Debug.Log("Plan successfully completed: " + plan);

            _genieBrain.genieBeliefs.OnPlanSucceeded(plan, this);

            genieGoals.OnPlanSuccessfullyCompleted(plan); // Update the Genie's goals based on the plan that was completed.

            // Fires from the event manager when a plan has been successfully completed. 
            // (Some plans, like LookAtAndTrackUser, don't complete on their own and need to be manually aborted.)

            CurrentPlan = EvaluateNextPlanAndPerformSetup(PlanSuccessState.Success);

            Debug.Log("New Plan: " + CurrentPlan);
        }

        // Fires when a Plan has failed.
        private void OnFailedPlan(GeniePlan plan) 
        {
            Debug.Log("Plan failed: " + plan);

            genieGoals.OnPlanFailed(plan);

            CurrentPlan = EvaluateNextPlanAndPerformSetup(PlanSuccessState.Failure);

            Debug.Log("New Plan: " + CurrentPlan);
        }

        private void WrapUpCurrentPlan() 
        {
            if (TargetItem != null) 
            {
                TargetItem.GenieGrabbable.MarkTargetedByGenie(false);
            }

            // Temporary solution in case we're still holding an item. This can happen if the previous plan was interrupted because,
            // for example, it failed.
            if (_genieBrain.Genie.genieGrabber.IsHoldingItem) 
            {
                _genieBrain.Genie.genieGrabber.InstantReleaseHeldItem();
            }

            genieGoals.ResetAnyGoalImCurrentlyTryingToSatisfy();

            TargetItem = null;
            TargetSeat = null;
            TargetDrawingSpace = null;
            TargetDrawingPosition = (default);
            DesiredItemPlacement = default(Vector3);
            TargetWindow = null;
            DesiredWindowStandingPosition = default(Vector3);
            DesiredPencilThrowingStandingPosition = default(Vector3);
            NavTargetLineSegmentDirection = default(Vector3);
        }

        /// <summary>
        /// Evaluates the current plan and sets up the necessary properties. Uses 'None' the Plan Success State for situations where it's not relevant.
        /// </summary>
        /// <param name="useThisPlanIsnteadOfPickingFromQueue"></param>
        /// <returns></returns>
        private GeniePlan EvaluateNextPlanAndPerformSetup(GeniePlan useThisPlanIsnteadOfPickingFromQueue = GeniePlan.None) 
        {
            return EvaluateNextPlanAndPerformSetup(PlanSuccessState.None, useThisPlanIsnteadOfPickingFromQueue);
        }

        /// <summary>
        /// Evaluates the next plan to pursue and sets up the necessary properties. By default it will pick the next plan based on priority, however you can override this if you provide
        /// a plan in the useThisPlanInsteadOfPickingFromQueue parameter.
        /// </summary>
        /// <param name="prevPlanSuccessState">When applicable, this will reveal whether the previous plan was a success or failure. At the time of writing, this is ignored, however it could be factored
        // into the Genie's determination of which plan to use next. </param>
        /// <param name="useThisPlanInsteadOfPickingFromGoals">Leave as default ('GeniePlan.None') to pick from the Queue. Otherwise you can push a desired plan that isn't necessarily in the queue at all.</param>
        /// <returns></returns>
        private GeniePlan EvaluateNextPlanAndPerformSetup(PlanSuccessState prevPlanSuccessState, GeniePlan useThisPlanInsteadOfPickingFromGoals = GeniePlan.None)
        {
            // For debugging purposes, we can force the Genie to pursue a specific plan without regard to prerequisites.
            if (debugForcePlan != GeniePlan.None)
            {
                return debugForcePlan;
            }

            GeniePlan newCandidatePlan;

            bool trySpecificPlan = useThisPlanInsteadOfPickingFromGoals != GeniePlan.None;

            // In some cases, we want to ignore goal and just try a specific plan.
            if (trySpecificPlan)
            {
                newCandidatePlan = useThisPlanInsteadOfPickingFromGoals;

                if (DoesPlanPassPrerequisites(newCandidatePlan)) 
                {
                    return newCandidatePlan; // It passes, so just go with it.
                }
            }            
           
            // If we're here, we need to pick a new plan based on goals.
            GenieGoals.Goal goal = null;

            // Keep track of the goals we've tried this frame. If we keep trying the same goals again and again, we need
            // to pause the effort and let the game refresh, otherwise we might block the main thread and effectively crash
            // the app or Editor.
            List<GenieGoals.Goal> goalsTriedThisFrame = new List<GenieGoals.Goal>();

            while (true)
            {
                // Find the most urgent goal to pursue (excluding the ones we've already tried this frame).
                goal = genieGoals.EvaluateMostUrgentGoal(goalsTriedThisFrame);

                if (goal == null)
                {
                    Debug.LogError("No Goal found to pursue -- defaulting to None now so we can try again in the next frame.");
                    genieGoals.SetGoalImCurrentlyTryingToSatisfy(null);
                    return GeniePlan.None;
                }

                // Add this goal to the list of goals we've tried this frame so we don't try it again.
                goalsTriedThisFrame.Add(goal);

                // Get the plans that can fulfill this goal.
                GeniePlan[] plans = goal.satsifyingPlans;

                if (plans == null || plans.Length == 0)
                {
                    Debug.LogError("Goal " + goal.name + " has no satisfying plans set.");
                    goal.OnAttemptToSatisfyGoalFailed();
                    continue;
                }

                if (goal.AttemptPlans == GenieGoals.Goal.PlanAttemptStrategy.Randomly)
                {
                    List<GeniePlan> plansAsList = new List<GeniePlan>(plans);

                    while (plansAsList.Count > 0)
                    {
                        UnityEngine.Random.InitState((int)DateTime.Now.Ticks);

                        int randomIndex = UnityEngine.Random.Range(0, plansAsList.Count);

                        GeniePlan randomPlan = plansAsList[randomIndex];

                        if (plansThatGenieShouldNotInitiate.Contains(randomPlan))
                        {
                            // This plan is not something the Genie should initiate on its own. Remove it from the list and try another.
                            plansAsList.RemoveAt(randomIndex);
                            continue;
                        }

                        if (DoesPlanPassPrerequisites(randomPlan))
                        {
                            genieGoals.SetGoalImCurrentlyTryingToSatisfy(goal);
                            return randomPlan;
                        }

                        // This plan didn't pass the prerequisites. Remove it from the list and try another.
                        plansAsList.RemoveAt(randomIndex);
                    }
                }
                else
                {
                    // Sequentially: the Goal is set to attempt plans in order.
                    foreach (GeniePlan plan in plans)
                    {
                        if (plansThatGenieShouldNotInitiate.Contains(plan))
                        {
                            // This plan is not something the Genie should initiate on its own. Skip it.
                            continue;
                        }

                        if (DoesPlanPassPrerequisites(plan))
                        {
                            genieGoals.SetGoalImCurrentlyTryingToSatisfy(goal);
                            return plan;
                        }
                    }
                }

                // If we're here, we couldn't find a plan that passed the prerequisites.

                goal.OnAttemptToSatisfyGoalFailed(); // Let the associated goal know that we couldn't satisfy it, so it gets de-prioritized
            }
        }

        // Checks if the plan passes the prerequisites. If it does, it sets up the necessary properties.
        // because the properties are set up based what happens during evaluation. Maybe this is the best approach, but it might
        // be kind of confusing for devs.
        private bool DoesPlanPassPrerequisites(GeniePlan newCandidatePlan) 
        {
            WrapUpCurrentPlan();

            switch (newCandidatePlan) 
            {
                case GeniePlan.TakeASeat:
                    // Is there a seat to sit on?
                    if (_genieBrain.genieBeliefs.IsThereASeatToSitOn(out Seat seat)) 
                    {
                        // If we found a seat, go sit on it.
                        TargetSeat = seat;
                        // Cache the radius of the seat plus the threshold for the Genie to arrive at the seat, so the Genie knows how close it needs to be able to
                        // get to the seat to sit down. (As of writing, this should actually be the same for every seat, so maybe this can be simplified...)
                        IdealSeatArrivalDistance = seat.seatingPositionRadius;
                        MaxSeatArrivalDistance = seat.seatingPositionRadius + _genieBrain.genieBeliefs.seatArrivalThreshold;
                        Debug.Log("Trying to sit on seat: " + seat.name);
                        return true;
                    }
                    else 
                    {
                        Debug.Log("No seat found to sit on.");
                        return false;
                    }
                case GeniePlan.DrawOnWall:
                    // Is there a space to draw on?
                    if (_genieBrain.genieBeliefs.IsThereASpaceToDrawOn(out DrawingSpace drawingSpace))
                    {
                        TargetDrawingSpace = drawingSpace;
                        TargetDrawingPosition = drawingSpace.DrawingPose.position;
                        NavTargetLineSegmentDirection = drawingSpace.DrawingPose.forward;
                        return true;
                    }
                    else 
                    {
                        Debug.Log("No space found to draw on.");
                        return false;
                    }
                    
                case GeniePlan.AdmireWindow:
                    if (_genieBrain.genieBeliefs.IsThereAWindowToLookOutFrom(out Window window, out Vector3 standingPosition)) 
                    {
                        TargetWindow = window;
                        DesiredWindowStandingPosition = standingPosition;
                        NavTargetLineSegmentDirection = window.transform.forward;

                        Vector3 idealPoint = standingPosition + window.transform.forward * _genieBrain.genieBeliefs.idealWindowStandingDistance;
                        Vector3 maxPoint = standingPosition + window.transform.forward * _genieBrain.genieBeliefs.maxWindowStandingDistance;

                        Debug.DrawLine(idealPoint, maxPoint, Color.red, 5f);

                        return true;
                    }
                    else 
                    {
                        Debug.Log("No window found to admire, or no valid point found to stand at.");
                        return false;
                    }
                case GeniePlan.FetchAndOfferItemToUser:
                    if (_genieBrain.genieBeliefs.IsThereAnItemICanGrab(out Item item))
                    {
                        TargetItem = item;
                        TargetItem.GenieGrabbable.MarkTargetedByGenie(true);
                        return true;
                    }
                    else 
                    {
                        Debug.Log("No item found to offer to user.");
                        return false;
                    }
                case GeniePlan.PlaceItemOnSurface:
                    // Is there an item to grab?
                    bool isThereAnItemToGrab = _genieBrain.genieBeliefs.IsThereAnItemICanGrab(out Item item2);

                    if (!isThereAnItemToGrab) 
                    {
                        Debug.Log("No item found to grab");
                        return false;
                    }

                    // Is there at least, in theory, a place to put the item? (Just check if there are potential placements -- don't use resources yet to find a specific one that works.)
                    Bounds itemBounds = item2.BoundsWhenNotRotated;

                    bool isThereAPlaceToPutTheItem = _genieBrain.Genie.GenieManager.Bootstrapper.ARSurfaceUnderstanding.itemPlacementOnHorizontalSurfaces.FindPotentialItemPlacements(itemBounds.size).Count > 0;
                    
                    if (!isThereAPlaceToPutTheItem) 
                    {
                        Debug.Log("No place found to put the item");
                        return false;
                    }

                    TargetItem = item2;
                    TargetItem.GenieGrabbable.MarkTargetedByGenie(true);
                    return true;
                case GeniePlan.ThrowPencilAtCeiling:
                    // Is there a pencil to throw?
                    if (_genieBrain.genieBeliefs.IsThereAPlaceOnTheCeilingToThrowAPencil(out Vector3 pencilTarget)) 
                    {
                        // We want the Genie to stand directly under the pencil target.
                        DesiredPencilThrowingStandingPosition = new Vector3(pencilTarget.x, _xRFloorManager.FloorY, pencilTarget.z);
                        return true;
                    }
                    else 
                    {
                        Debug.Log("No valid place to throw pencil.");
                        return false;
                    }
                default:
                    return true;
            }
        }

        // Fires when the user throws an item that hits the Genie.
        private void OnItemImpact(Item item, bool isFromFront)
        {
            if (CurrentPlan == GeniePlan.MaintainPersonalSpace) return; // Don't do anything if we're maintaining personal space. That's more important.

            if (CurrentPlan == GeniePlan.ReactToProjectileImpact) return; // Don't do anything if we're already performing the same Plan.
            
            Debug.Log("Interrupting current plan to respond to impact.");

            // If we're not already reacting to an impact, we'll interrupt the current plan and react to the impact.
            InterruptWithNewPlan(GeniePlan.ReactToProjectileImpact);
        }

        // Fires when the Genie sees an item being offered by the user.
        private void OnGenieNoticesItemOffered(Item item)
        {
            if (CurrentPlan == GeniePlan.MaintainPersonalSpace) return; // Don't do anything if we're maintaining personal space. That's more important.

            if (CurrentPlan == GeniePlan.AcceptItemBeingOffered) return; // Don't do anything if we're already performing the same Plan.

            InterruptWithNewPlan(GeniePlan.AcceptItemBeingOffered);
            TargetItem = item;
            TargetItem.GenieGrabbable.MarkTargetedByGenie(true);
        }

        private void OnGenieNoticesHighFiveSolicitationFromUser(Transform userHandTransform)
        {
            if (CurrentPlan == GeniePlan.MaintainPersonalSpace) return; // Don't do anything if we're maintaining personal space. That's more important.

            if (CurrentPlan == GeniePlan.RespondToUserHighFiveSolicitation) return; // Don't do anything if we're already performing the same Plan.
            
            if (CurrentPlan == GeniePlan.SolicitAndPerformHighFive) return; // Don't do anything if we're performing a similar plan.

            InterruptWithNewPlan(GeniePlan.RespondToUserHighFiveSolicitation);
        }
        
        // Checks if the user is within the Genie's personal space and decides whether to switch to the "MaintainPersonalSpace" plan.
        private void UpdatePersonalSpaceMaintenance()
        {
            if (CurrentPlan == GeniePlan.MaintainPersonalSpace) return;  // Already maintaining personal space.

            if (!_genieBrain.genieBeliefs.IsUserWithinPersonalSpace()) return; // User is not within personal space.
            
            Debug.Log("User is too close. Switching to MaintainPersonalSpace plan.");
            InterruptWithNewPlan(GeniePlan.MaintainPersonalSpace);
        }

        private void InterruptWithNewPlan(GeniePlan newPlan)
        {
            genieGoals.OnPlanInterrupted(CurrentPlan);
            CurrentPlan = EvaluateNextPlanAndPerformSetup(newPlan);
        }

        /// <summary>
        /// Called when the app regains focus from a task-switch, for example.
        /// </summary>
        public void OnAppFocusRegained()
        {
            _genieBrain.Genie.StartCoroutine(OnAppFocusRegained_C());
        }

        private IEnumerator OnAppFocusRegained_C()
        {
            CurrentPlan = GeniePlan.None;

            yield return new WaitForSeconds(1f);

            CurrentPlan = GeniePlan.WaveAtUser;
        }
    }
}
 