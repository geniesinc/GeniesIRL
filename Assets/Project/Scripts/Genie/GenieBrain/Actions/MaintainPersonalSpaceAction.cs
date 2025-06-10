using System;
using Unity.Behavior;
using UnityEngine;
using Action = Unity.Behavior.Action;
using Unity.Properties;
using UnityEngine.Categorization;
using System.Collections;

namespace GeniesIRL 
{
    [Serializable, GeneratePropertyBag]
    [NodeDescription(name: "MaintainPersonalSpace", story: "[Genie] maintains personal space from [User]", category: "Action/GeniesIRL", id: "e85b01ed43ee03d28d3dfbcb21b113fc")]
    public partial class MaintainPersonalSpaceAction : Action
    {
        [SerializeReference] public BlackboardVariable<Genie> Genie;
        [SerializeReference] public BlackboardVariable<XRInputWrapper> User;

        /// <summary>
        /// For pathfinding purposes, we need to add a little extra padding to the personal space radius. It makes the threshold
        /// a little higher for *leaving* personal space over entering it.
        /// </summary>
        public const float extraPersonalSpacePadding = 0.2f;

        private Genie _genie => Genie.Value;

        private XRInputWrapper _user => User.Value;

        private GeniePlacementValidation _placementValidation;

        private GeniePlacementValidation.GeniePlacementValidationSettings _placementValidationSettings;

        private Vector3? _targetLocation = null;

        private Coroutine _yawCoroutine;
        private Coroutine _nestedCoroutine;

        protected override Status OnStart()
        {
            _genie.genieAnimation.StartMaintainPersonalSpace();

            _placementValidationSettings = new GeniePlacementValidation.GeniePlacementValidationSettings()
            {
                fieldOfView = 180f,
                distanceScoreWeight = 50,
                directionScoreWeight = 50
            };

            _placementValidation = new GeniePlacementValidation(_genie.GenieManager.Bootstrapper.ARNavigation);

            _targetLocation = FindNewTargetLocation();

            _yawCoroutine = _genie.StartCoroutine(ContinouslyLookAtTargetCoroutine_C());
            _genie.genieLookAndYaw.eyeballAimer.TrackTarget(_user.Head);

            return Status.Running;
        }

        protected override Status OnUpdate()
        {
            if (IsUserWellOutsideOfPersonalSpace()) 
            {
                Debug.Log("User is no longer within personal space. Stopping action.");
                return Status.Success; // Note: Should there be a duration of time before declaring success?
            }

            //Debug.Log("Dist from user: " + VectorUtils.GetDistanceXZ(_genie.transform.position, _user.Head.position));

            if (!ValidateLocation(_targetLocation)) 
            {
                _targetLocation = FindNewTargetLocation(); // Get a new location.
            }

            if (_targetLocation.HasValue)
            {
                //float distXZ = VectorUtils.GetDistanceXZ(_genie.transform.position, _targetLocation.Value);
                //Debug.Log("Navigating to target location. Genie is currently " + distXZ + " units away.");
                //_genie.genieNavigation.EnableAStarRotation(false);
                _genie.genieNavigation.SetAStarDestination(_targetLocation.Value, false);
                _genie.genieAnimation.UpdateMaintainPersonalSpace();
            }
            else 
            {
                //Debug.Log("No valid target location found. Stopping navigation.");
                _genie.genieNavigation.StopNavigation();
            }

            return Status.Running;
        }

        private Vector3? FindNewTargetLocation()
        {
            // Update the min distance radius to match the personal space radius, in case it's changed.
            _placementValidationSettings.minDistanceFromUser = _genie.genieSense.personalSpace.Radius + extraPersonalSpacePadding;
            _placementValidationSettings.maxDistanceFromUser = _placementValidationSettings.minDistanceFromUser + 3f;

            // Try to find a valid placement point in front of the user. (Note: will need to also check if each point can be pathed to.)
            if (_placementValidation.TryFindValidAvoidancePlacement(_user.Head, _genie, _placementValidationSettings, out Vector3 placementPoint))
            {
                // Q: Will this work if the Genie is not on a walkable node? (Which can happen if they are standing on the user's nav mesh obstacle. Perhaps for this, the
                // obstacle needs to be ignored?)
                return placementPoint;
            }

            return null; // No valid placement point was found.
        }

        /// <summary>
        /// Checks to make sure the location is valid, can be pathed to, and that we haven't yet arrived at it.
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        private bool ValidateLocation(Vector3? location)
        {
            if (!location.HasValue)
            {
                return false;
            }

            if (_genie.genieNavigation.AIPath.reachedEndOfPath) return false;

            if (VectorUtils.IsWithinDistanceXZ(_genie.transform.position, location.Value, extraPersonalSpacePadding))
            {
                return false; // We've essentially arrived.
            } 

            return _genie.genieNavigation.IsPathReachable(location.Value, 0.01f);
        }

        private bool IsUserWellOutsideOfPersonalSpace()
        {
            float personalSpaceRadius = _genie.genieSense.personalSpace.Radius + extraPersonalSpacePadding;
            return VectorUtils.IsGreaterThanDistanceXZ(_genie.transform.position, _user.Head.position, personalSpaceRadius);
        }

        protected override void OnEnd()
        {
            _targetLocation = null;

            _genie.genieNavigation.StopNavigation();

            //_genie.genieNavigation.EnableAStarRotation(true);

            if (_yawCoroutine != null)
            {
                _genie.StopCoroutine(_yawCoroutine);
                _yawCoroutine = null;
            }

            if (_nestedCoroutine != null)
            {
                _genie.StopCoroutine(_nestedCoroutine);
                _nestedCoroutine = null;
            }

            _genie.genieLookAndYaw.eyeballAimer.StopTrackingTarget();
        }

        private IEnumerator ContinouslyLookAtTargetCoroutine_C() 
        {
            while (true)
            {
                _nestedCoroutine = _genie.StartCoroutine(_genie.genieLookAndYaw.YawTowards_C(_user.Head, 30f, 4f));
                yield return _nestedCoroutine;
            }
        }
    }
}


