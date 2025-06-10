using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace GeniesIRL
{
    /// <summary>
    /// Use this class instead of the NavigateToLocation action that is included in Behavior Graph, as it is specifically designed for Genies IRL.
    /// </summary>
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "NavigateToLocationIRL", story: "[Genie] navigates IRL to [Location]", category: "Action/GeniesIRL", id: "0be680e6dd567d11b47ab80ef5ee89e6")]
    public partial class NavigateToLocationIRLAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<Vector3> Location;
        
        [Header("Arrival Evaluation")]

        [Tooltip("The type of evaluation to use to determine if the Genie has arrived at the target. " + 
        "\n" + "ReachEndOfPath: The Genie is considered to have arrived at the target only if it has reached the end of its path, which is as close as they" +
        "\n" + "can get to the target. No checks are made to see if that path gets within any specific range of the target." +
        "\n" + "StopAtArrivalDistance: The Genie is considered to have arrived at the as soon as is within the basic arrival distance. If it cannot reach this distance, the Action will fail." +
        "\n" + "ClosestWithinArrivalDistance: The Genie will try to reach the exact destination, but if it can't, it will get as close as possible within the arrival distance. If it can't at least reach the arrival distance, it will fail." +
        "\n" + "GetAsCloseToIdealDistanceAsPossible: The Genie will try to reach the ideal arrival distance, but if it can't, it will get as close to this distance as possible. If it can't even reach the basic arrival distance, it will fail." +
        "\n" + "ReachableWithArms: The Genie will try to reach the target with their arms. This will take into account the Genie's arm reach length, as well as her ability to lean forward in certain situations." +
        "\n" + "BestPointOnLineSegment: Uses a line segment with an ideal point and an end point. The nav system will try to reach the ideal point exactly, but if it can't, it will scan along the line segment to find the closest point to the target that it can reach. If it can't reach the end point, it will fail. Dev must provide the direction of the line segment, a basic arrival distance, and, optionally, an ideal arrival distance.")]
        [SerializeReference] public BlackboardVariable<NavArrivalEvaluationType> ArrivalEvaluationType;
        
        [Tooltip("The basic distance to evaluate whether the Genie has arrived at the target. Used in ArrivalEvaluationTypes StopAtArrivalDistance, ClosestWithinArrivalDistance, and GetAsCloseToIdealDistanceAsPossible.")]
        [SerializeReference] public BlackboardVariable<float> BasicArrivalDistance = new BlackboardVariable<float>(0.2f);

        [Tooltip("Used in ArrivalEvaluationType GetAsCloseToIdealDistanceAsPossible and (optionally) BestPointOnLineSegment. The ideal distance that the Genie will try to reach. Must be less than or equal to BasicArrivalDistance.")]
        [SerializeReference] public BlackboardVariable<float> IdealArrivalDistance = new BlackboardVariable<float>(-1f);

        [Tooltip("Used in ArrivalEvaluationType BestPointOnLineSegment. This is the direction the line segment is pointing, from the target location to the last acceptable point.")]
        [SerializeReference] public BlackboardVariable<Vector3> LineSegmentDirection = new BlackboardVariable<Vector3>(Vector3.zero);

        private Genie _genie => Genie.Value;
        private Vector3 _location => Location.Value;

        protected override Status OnStart()
        {
            _genie.genieAnimation.Animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun); // The Anim controller dictates that you gotta be in first to Idle to walk/run.
            _genie.genieNavigation.StopNavigation();
            _genie.genieSense.personalSpace.SetRadiusOverride(PersonalSpace.kRadiusDuringNavigatingIrlActions);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            return NavigateToLocation();
        }

        protected override void OnEnd()
        {
            _genie.genieNavigation.StopNavigation();
            _genie.genieSense.personalSpace.ResetRadius();
        }

        private Status NavigateToLocation()
        {
            Status status =  NavArrivalEvaluation.Evaluate(ArrivalEvaluationType.Value, _genie, _location, out Vector3 processedTargetLocation, BasicArrivalDistance.Value, IdealArrivalDistance.Value, LineSegmentDirection.Value);

            if (status == Status.Failure)
            {
                Debug.Log("NavigateToLocationIRLAction: Action Failed because the location is unreachable.");
            }

            _genie.genieNavigation.SetAStarDestination(processedTargetLocation); 

            return status;
        }
    }
}
