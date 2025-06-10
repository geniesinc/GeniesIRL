using GeneisIRL;
using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using System.Collections;
using Unity.AppUI.UI;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "SitOnSeat", story: "[Genie] sits on [Seat]", category: "Action/GeniesIRL", id: "1c430919481d7684a8e2051022e1dd74")]
    public partial class SitOnSeatAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<Seat> Seat;

        private Genie _genie => Genie.Value;
        private Seat _seat => Seat.Value;
        private GenieSitAndStand _genieSitAndStand => _genie.genieSitAndStand;

        [SerializeReference] public BlackboardVariable<float> MinSeatedTime = new BlackboardVariable<float>(3f);
        [SerializeReference] public BlackboardVariable<float> MaxSeatedTime = new BlackboardVariable<float>(7f);

        private Coroutine _coroutine;

        private Coroutine _nestedCoroutine;

        private Vector3 _seatingPosition;

        protected override Status OnStart()
        {
            _nestedCoroutine = null;
            
            int seatingPointIdx = _seat.MultiPointNavTarget.LatestSelectedPointIndex;

            if (seatingPointIdx < 0 || seatingPointIdx >= _seat.MultiPointNavTarget.Points.Length)
            {
                Debug.LogError("SitOnSeatAction: The seat's MultiPointNavTarget does not have a valid LatestSelectedPointIndex. Defaulting to middle of the seat.");
                _seatingPosition = _seat.transform.position;
            }
            else 
            {
                _seatingPosition = _seat.MultiPointNavTarget.Points[seatingPointIdx];
            }

            _coroutine = _genie.StartCoroutine(SitOnSeat_C());

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            while (_coroutine != null) 
            {
                return Status.Running;
            }

            return Status.Success;
        }

        protected override void OnEnd()
        {
            if (_coroutine != null) 
            {
                _genie.StopCoroutine(_coroutine);
            }

            _coroutine = null;

            if (CurrentStatus.IsCompleted()) return;

            // If we've reached this point, it means that the action was externally canceled. We'll need to clean things up and make sure the Genie is standing up.
            
            // Stop the nested coroutines
            if (_nestedCoroutine != null) 
            {
                _genie.StopCoroutine(_nestedCoroutine);
            }

            _genie.genieSitAndStand.CancelSittingAndStandUpQuickly();
        }

        private IEnumerator SitOnSeat_C()
        {
            // Determine the direction we should be facing when we sit down.
            Vector3 seatingDirection = _seat.EvaluateSeatingDirection(_seatingPosition);
            seatingDirection.y = 0;
            seatingDirection.Normalize();
            Quaternion seatingRotation = Quaternion.LookRotation(seatingDirection, Vector3.up);

            // We have two different seating types. Type 1 is the original version, where the Genie sits down like a normal human would. Type 2
            // is Animal-Crossing inspired, where the Genie jumps and twirls into place. 
            bool useJumpAndTwirl = UseJumpAndTwirlAnimationType(_seatingPosition);

            if (!useJumpAndTwirl)
            {
                // Face in the direction we'll be facing when we sit down.
                 _genie.genieAnimation.Animator.SetTrigger(GenieAnimation.Triggers.IdleWalkRun); // The anim controller dictates that you gotta be in first to Idle to walk/run.
                _nestedCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(_genie.transform.position + seatingDirection, 0f, 2.25f));
                yield return _nestedCoroutine;
            }

            // Sit down
            _nestedCoroutine = _genie.StartCoroutine(_genieSitAndStand.Sit_C(_seatingPosition, seatingRotation, useJumpAndTwirl));
            yield return _nestedCoroutine;
            
            // Stay sitting idly for a moment.
            yield return new WaitForSeconds(UnityEngine.Random.Range(MinSeatedTime.Value, MaxSeatedTime.Value));
            //yield return new WaitForSeconds(2f);

            // Stand up.
            _nestedCoroutine = _genie.StartCoroutine(_genieSitAndStand.StandUp_C(useJumpAndTwirl));
            yield return _nestedCoroutine;

            // Wrap Action.
            _coroutine = null;
            _nestedCoroutine = null;
        }

        private bool UseJumpAndTwirlAnimationType(Vector3 seatingPosition) 
        {
            // We're going to behave slightly differently depending on how high the seat is. If the seat isn't too high, we'll first orient ourselves in the seating direction
            // and slide into place. If it is high, we'll jump and twirl into place, Animal Crossing-style.
            float seatHeightThreshold = 0.5f;
            return _seatingPosition.y - _genie.transform.position.y > seatHeightThreshold;
        }
    }
}


