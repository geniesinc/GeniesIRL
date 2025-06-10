using System;
using System.Collections.Generic;
using GeniesIRL.GlobalEvents;
using UnityEngine;
using UnityEngine.Serialization;

namespace GeniesIRL 
{
    /// <summary>
    /// Keeps track of the Genie's Goals so they can be used in decision-making. 
    /// </summary>
    [Serializable]
    public class GenieGoals
    {
        public Goal GoalImCurrentlyTryingToSatisfy {get; private set;} = null;

        [FormerlySerializedAs("desires")]
        public Goal[] goals;

        [SerializeField]
        private bool verboseDebugLogging = false;

        private const int k_maxUrgencyForRepeatGoal = 30;

        public void Initialize()
        {
            foreach (Goal goal in goals)
            {
                goal.Initialize(this);
                goal.OnSatisfied += OnGoalFulfilled;
            }

            GlobalEventManager.Subscribe<NewWallAppeared>(OnNewWallAppeared);
            GlobalEventManager.Subscribe<NewSeatAppeared>(OnNewSeatAppeared);
            GlobalEventManager.Subscribe<NewCeilingAppeared>(OnNewCeilingAppeared);
            GlobalEventManager.Subscribe<NewWindowAppeared>(OnNewWindowAppeared);
            GlobalEventManager.Subscribe<NewTableAppeared>(OnNewTableAppeared);
        }

        private void OnGoalFulfilled(Goal goal)
        {
            bool areAllGoalsFulfilled = Array.TrueForAll(goals, d => d.HasBeenSatisfied);
            
            if (areAllGoalsFulfilled)
            {
                if (verboseDebugLogging)
                {
                    DebugLogStatus("[Goal] All goals have been fulfilled. Resetting all goal fulfillment statuses.");
                }
                
                // Reset all goals to not fulfilled.
                foreach (Goal d in goals)
                {
                    d.HasBeenSatisfied = false;
                }
            }
        }

        public void OnPlanSuccessfullyCompleted(GeniePlan plan)
        {
            foreach (Goal goal in goals)
            {
                goal.OnPlanSuccessfullyCompleted(plan);

            }

            ResetAnyGoalImCurrentlyTryingToSatisfy();

            if (verboseDebugLogging)
            {
                DebugLogStatus("[Goal] OnPlanSuccessful ()" + plan.ToString() + ")");
            }
        }

        public void OnPlanFailed(GeniePlan plan)
        {
            if (GoalImCurrentlyTryingToSatisfy != null)
            {
                GoalImCurrentlyTryingToSatisfy.OnAttemptToSatisfyGoalFailed();
            }

            ResetAnyGoalImCurrentlyTryingToSatisfy();

            if (verboseDebugLogging)
            {
                DebugLogStatus("[Goal] OnPlanFailed (" + plan.ToString() +")");
            }
        }

        public void OnPlanInterrupted(GeniePlan plan)
        {
            OnPlanFailed(plan); // Just treat it as a failure for now.
        }

        public Goal EvaluateMostUrgentGoal(List<Goal> goalsToExclude) 
        {
            List<Goal> topGoals = GetMostUrgentGoals(goalsToExclude);

            if (topGoals.Count == 0)
            {
                Debug.LogError("GenieGoals.EvaluateNextGoalToTryAndFulfill() was called, but there are no goals found.");
                return null;
            }

            // Pick a random goal from the top goals
            Goal randomGoal = topGoals[UnityEngine.Random.Range(0, topGoals.Count)];

            return randomGoal;
        }

        public void SetGoalImCurrentlyTryingToSatisfy(Goal goal)
        {
            GoalImCurrentlyTryingToSatisfy = goal;

            if (verboseDebugLogging)
            {
                DebugLogStatus("[Goal] I'm Currently Trying To Satisfy: " + (goal != null ? goal.name : "None"));
            }
        }

        public void ResetAnyGoalImCurrentlyTryingToSatisfy()
        {
            GoalImCurrentlyTryingToSatisfy = null;
        }

        private List<Goal> GetMostUrgentGoals(List<Goal> goalsToExclude = null)
        {
            List<Goal> topGoals = new List<Goal>();
            int maxUrgency = 0;

            foreach (Goal goal in goals)
            {
                if (goal.mute) continue; // Ignore muted goals.

                if (goalsToExclude != null && goalsToExclude.Contains(goal))
                {
                    continue; // Skip excluded goals.
                }

                if (goal.CurrentUrgency > maxUrgency)
                {
                    topGoals.Clear();
                    topGoals.Add(goal);
                    maxUrgency = goal.CurrentUrgency;
                }
                else if (goal.CurrentUrgency == maxUrgency)
                {
                    topGoals.Add(goal);
                }
            }

            return topGoals;
        }

        private void DebugLogStatus(string eventName)
        {
            string log = eventName + " - ";

            List<Goal> goalsSortedFromMostToLeastUrgent = new List<Goal>(goals);
            goalsSortedFromMostToLeastUrgent.Sort((a, b) => b.CurrentUrgency.CompareTo(a.CurrentUrgency));

            foreach (Goal goal in goalsSortedFromMostToLeastUrgent)
            {
                if (goal.mute) continue; // Ignore muted goals.
                
                log += goal.name + ": " + goal.CurrentUrgency + ", ";
            }

            log = "<color=cyan>" + log + "</color>";

            Debug.Log(log);
        }

        
        private void OnNewCeilingAppeared(NewCeilingAppeared appeared)
        {
            // Give a bonus to the "throw pencil" goal when a new ceiling plane appears.
            Goal ceilingGoal = GetFirstGoalByPlan(GeniePlan.ThrowPencilAtCeiling);

            if (ceilingGoal == null || ceilingGoal.HasBeenSatisfied) return;
            
            ceilingGoal.GiveSuperUrgency(); // If the Goal hasn't been fulfilled, give it super-urgency.
            
        }

        private void OnNewSeatAppeared(NewSeatAppeared appeared)
        {
            // Give a bonus to the "take a seat" goal when a new seat appears.
            Goal seatGoal = GetFirstGoalByPlan(GeniePlan.TakeASeat);

            if (seatGoal == null || seatGoal.HasBeenSatisfied) return;
            
            seatGoal.GiveSuperUrgency();
        }

        private void OnNewWallAppeared(NewWallAppeared appeared)
        {
            // Give a bonus to the "draw on wall" goal when a new wall plane appears.
            Goal wallGoal = GetFirstGoalByPlan(GeniePlan.DrawOnWall);
            
            if (wallGoal == null || wallGoal.HasBeenSatisfied) return; 
            
            wallGoal.GiveSuperUrgency(); // If the Goal hasn't been fulfilled, give it super-urgency.
        }

        private void OnNewWindowAppeared(NewWindowAppeared appeared)
        {
            // Give a bonus to the "admire window" goal when a new window plane appears.
            Goal windowGoal = GetFirstGoalByPlan(GeniePlan.AdmireWindow);
            
            if (windowGoal == null || windowGoal.HasBeenSatisfied) return;
            
            windowGoal.GiveSuperUrgency(); // If the Goal hasn't been fulfilled, give it super-urgency.
        }

        private void OnNewTableAppeared(NewTableAppeared appeared)
        {
            // Give a bonus to the "place item on table" goal when a new table plane appears.
            Goal tableGoal = GetFirstGoalByPlan(GeniePlan.PlaceItemOnSurface);
            
            if (tableGoal == null) return;
            
            tableGoal.GiveSuperUrgency();
        }

        private Goal GetFirstGoalByPlan(GeniePlan plan)
        {
            foreach (Goal goal in goals)
            {
                foreach (GeniePlan fulfillingPlan in goal.satsifyingPlans)
                {
                    if (fulfillingPlan == plan)
                    {
                        return goal;
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// A Goal that the Genie has, which can be fulfilled by completing one or more plans.
        /// </summary>
        [Serializable]
        public class Goal
        {
            public int CurrentUrgency {get; private set;}

            public bool HasBeenSatisfied {get; set;} = false;

            public event Action<Goal> OnSatisfied;

            [Tooltip("Random means the genie will try a fulfilling plan at random, whereas sequentially means she will try the first plan" +
            "in the list, then the second, etc.")]
            [FormerlySerializedAs("AttemptGoals")]
            public PlanAttemptStrategy AttemptPlans = PlanAttemptStrategy.Randomly;

            public enum PlanAttemptStrategy {Randomly, Sequentially}

            [Tooltip("An array of plans that satisfy/acomplish this goal when completed (i.e. reduce urgency to 0).")]
            [FormerlySerializedAs("fulfillingGoals")]
            [FormerlySerializedAs("fulfillingPlans")]
            public GeniePlan[] satsifyingPlans;

            [Tooltip("The type or category of goal, which can be satisfy by completion of a plan.")]
            public string name = "Name";

            [Tooltip("If true, the Genie will not attempt to satisfy this goal. (At present, this is only used for debugging.)")]
            public bool mute = false;

            [SerializeField, Range(0, 100)]
            [Tooltip("The urgency of this goal when it is first created. (100 means it has max urgency)")]
            private int startingUrgency = 100;

            [SerializeField, Range(0, 100)]
            [Tooltip("Default should be 10. The amount at which urgency increases each time a non-satisfying plan is successfully completed.")]
            private int urgencyIncreaseRate = 10;

            [SerializeField, Range(0, 100)]
            [Tooltip("The amount at which urgency decreases when a related plan fails, or fails to meet its prerequisites.")]
            private int urgencyDecreaseWhenSatisfyingPlanFails = 30;

            [NonSerialized] 
            private GenieGoals _genieGoals; // A reference to the GenieGoals that manages this Goal.

            public void Initialize(GenieGoals genieGoals) 
            {
                _genieGoals = genieGoals; // Store a reference to the manager class.
                CurrentUrgency = startingUrgency;
            }

            /// <summary>
            /// Called when the Genie successfully completes any plan.
            /// </summary>
            public void OnPlanSuccessfullyCompleted(GeniePlan plan)
            {
                bool satisfiesGoal = Array.Exists(satsifyingPlans, g => g == plan);
                
                if (satisfiesGoal)
                {
                     // Goal is fulfilled
                    CurrentUrgency = 0; // Reset urgency
                    HasBeenSatisfied = true;
                    OnSatisfied?.Invoke(this);
                }
                else 
                {
                    // This goal is not satisfied by this particular plan, meaning urgency generally increases

                    if (CurrentUrgency >= 100)
                    {
                        return; // Don't increase urgency if it's already past the 100 mark (if it's greater than 100, keep it there -- that means it has super-urgency)
                    }
                   
                    CurrentUrgency += (int)(urgencyIncreaseRate); // Increase by the increase rate.

                    // Cap urgency based on this goal's satsifaction status. If it has been satisfied, cap it at 30. Otherwise, cap it at 100.
                    int maxUrgency = HasBeenSatisfied ? k_maxUrgencyForRepeatGoal : 100;

                    CurrentUrgency = Mathf.Clamp(CurrentUrgency, 0, maxUrgency); // Clamp to 100 under normal circumstances.
                }
            }

            /// <summary>
            /// Called when the Genie makes an attempt to satisfy a goal, but fails. This can happen if the goal's satisfying plans don't pass the prerequisites,
            /// or if the Plan fails while in action.
            /// </summary>
            public void OnAttemptToSatisfyGoalFailed() 
            {
                // Sharply decrease urgency so we don't keep trying to fulfill this goal.
                CurrentUrgency -= urgencyDecreaseWhenSatisfyingPlanFails;
                CurrentUrgency = Mathf.Clamp(CurrentUrgency, 0, 100);
            }

            /// <summary>
            /// Sets the urgency to 101 to bump it to the top of the list. Typically, urgency maxes out at 100.
            /// </summary>
            public void GiveSuperUrgency()
            {
                CurrentUrgency = 101;
            }
        }
    }
}


