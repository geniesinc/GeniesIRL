using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "NavigateToTargetIRL", story: "[Genie] navigates IRL to [Target]", category: "Action/GeniesIRL", id: "316d1aedbf16939d1f8334819500cb96")]
    public partial class NavigateToTargetIRLAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;

        [Tooltip("The target that the Genie will navigate to. The gameobject can optionally have a MultiPointNavTarget component attached to it, in which case " +
        "the Genie will try to navigate to one of the points in the MultiPointNavTarget.")]
        [SerializeReference] public BlackboardVariable<GameObject> Target;

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

        [Tooltip("If set to a value other than None, the Genie will check for personal space violations during navigation.")]
        [SerializeReference] public BlackboardVariable<PersonalSpace.PersonalSpaceType> PersonalSpaceCheck = new BlackboardVariable<PersonalSpace.PersonalSpaceType>(PersonalSpace.PersonalSpaceType.None);

        private Genie _genie => Genie.Value;
        private GameObject _target => Target.Value;

        private MultiPointNavTarget _multiPointNavTarget;

        protected override Status OnStart()
        {
            Debug.Log("NavigateToTargetIRLAction.OnStart");

            if (_target != null) 
            {
                _multiPointNavTarget = _target.GetComponent<MultiPointNavTarget>();
            }

            _genie.genieAnimation.Animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun); // The anim controller dictates that you gotta be in first to Idle to walk/run.
            _genie.genieNavigation.StopNavigation();
            _genie.genieSense.personalSpace.SetRadiusOverride(PersonalSpace.kRadiusDuringNavigatingIrlActions);
            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (_target == null)
            {
                Debug.Log("NavigateToTargetIRLAction: Target is null. Action failed.");
                return Status.Failure;
            }

            return NavigateToTarget();
        }

        // This is important because it allows us to tie up any loose ends if the Action is externally interrupted.
        protected override void OnEnd()
        {
            _genie.genieNavigation.StopNavigation();
            _genie.genieSense.personalSpace.ResetRadius();
        }

        private Status NavigateToTarget()
        {
            Status status;
            Vector3 processedTargetLocation = Vector3.zero;

            if (_multiPointNavTarget == null)
            {
                // Navigate to single-point nav target.
                Vector3 targetPosition = _target.transform.position;

                // Check if the user would be in the Genie's personal space if they were to arrive at the target.
                if (PersonalSpaceCheck.Value != PersonalSpace.PersonalSpaceType.None && _genie.genieBrain.genieBeliefs.IsUserWithinPersonalSpaceOfPoint(targetPosition, PersonalSpaceCheck.Value))
                {
                    Debug.Log("NavigateToTargetIRLAction: Action Failed because the user is too close to destination.");
                    return Status.Failure;
                }

                status =  NavArrivalEvaluation.Evaluate(ArrivalEvaluationType.Value, _genie, targetPosition, out processedTargetLocation, BasicArrivalDistance.Value, IdealArrivalDistance.Value, LineSegmentDirection.Value);
            }
            else 
            {
                // Navigate to multi-point nav target.
                status = Status.Failure; // Assume failure until proven otherwise.
                
                Vector3[] points = _multiPointNavTarget.Points;

                for (int i=0; i<points.Length; i++)
                {
                    Vector3 point = points[i];

                    // Check if the user would be in the Genie's personal space if they were to arrive at the target.
                    if (PersonalSpaceCheck.Value != PersonalSpace.PersonalSpaceType.None && _genie.genieBrain.genieBeliefs.IsUserWithinPersonalSpaceOfPoint(point, PersonalSpaceCheck.Value))
                    {
                        Debug.Log("NavigateToTargetIRLAction: Skipped point because the user is too close to destination.");
                        continue;
                    }

                    status = NavArrivalEvaluation.Evaluate(ArrivalEvaluationType.Value, _genie, point, out processedTargetLocation, BasicArrivalDistance.Value, IdealArrivalDistance.Value, LineSegmentDirection.Value);
                    
                    if (status != Status.Failure)
                    {
                        _multiPointNavTarget.SetLatestSelectedPointIndex(i);
                        break;
                    }
                }
            }

            if (status == Status.Failure)
            {
                Debug.Log("NavigateToTargetIRLAction: Action Failed because the target is unreachable.");
                return Status.Failure;
            }

            _genie.genieNavigation.SetAStarDestination(processedTargetLocation);

            return status;
        }
    }
}


