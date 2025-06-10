using GeneisIRL;
using Unity.Behavior;
using UnityEngine;
using UnityEngine.Serialization;

namespace GeniesIRL 
{
    /// <summary>
    /// This class is responsible for making decisions.
    /// </summary>
    [System.Serializable]
    public class GenieBrain
    {
        public Genie Genie {get; private set;}

        public BrainInspector brainInspector {get; private set;}

        [Tooltip("The Behavior Graph Agent that will be used to make decisions.")]
        public BehaviorGraphAgent behaviorGraphAgent;
        [FormerlySerializedAs("goalDecider")]
        public PlanDecider planDecider;
        public GenieBeliefs genieBeliefs;
        private Transform userHead;

        public void OnStart(Genie genie)
        {
            Genie = genie;

            genieBeliefs.Initialize(this);
            planDecider.Initialize(this);

            brainInspector = new BrainInspector();
            brainInspector.Initialize(this);

            InitializeGraph();
        }

        public void OnUpdate()
        {
            planDecider.OnUpdate();

            UpdateBlackboard();
        }

        public void OnAppFocusRegained()
        {
            planDecider.OnAppFocusRegained();
        }

        /// <summary>
        /// Updates the Behavior Graph Blackboard with the latest data from the GenieBrain.
        /// </summary>
        public void UpdateBlackboard() 
        {
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Current Plan", planDecider.CurrentPlan);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Target Item", planDecider.TargetItem);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Target Seat", planDecider.TargetSeat);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Ideal Seat Arrival Distance", planDecider.IdealSeatArrivalDistance);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Max Seat Arrival Distance", planDecider.MaxSeatArrivalDistance);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Desired Item Placement", planDecider.DesiredItemPlacement);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Target Drawing Space", planDecider.TargetDrawingSpace);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Target Drawing Position", planDecider.TargetDrawingPosition);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Target Window", planDecider.TargetWindow);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Desired Window Standing Position", planDecider.DesiredWindowStandingPosition);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Desired Pencil Throwing Standing Position", planDecider.DesiredPencilThrowingStandingPosition);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Nav Target Line Segment Direction", planDecider.NavTargetLineSegmentDirection);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Ideal Drawing Distance", Genie.genieDraw.idealDrawingDistance);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Max Drawing Distance", Genie.genieDraw.maxDrawingDistance);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Ideal Window Standing Distance", Genie.genieBrain.genieBeliefs.idealWindowStandingDistance);
            behaviorGraphAgent.BlackboardReference.SetVariableValue("Max Window Standing Distance", Genie.genieBrain.genieBeliefs.maxWindowStandingDistance);
        }

        private void InitializeGraph()
        {
            // Set up the Blackboard with references the Behavior Graph will need.
            behaviorGraphAgent.BlackboardReference.SetVariableValue("My Genie", Genie);

            if (Genie.GenieManager == null) return; // This means the Genie wasn't spawned by a GenieManager, most likely because we are in an isolated dev environment. Stop here.
            
            userHead = Genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper.Head;
            behaviorGraphAgent.BlackboardReference.SetVariableValue("User", userHead.gameObject);

            XRInputWrapper xrInputWrapper = Genie.GenieManager.Bootstrapper.XRNode.xrInputWrapper;
            behaviorGraphAgent.BlackboardReference.SetVariableValue("User Input", xrInputWrapper);
        }
    }
}

