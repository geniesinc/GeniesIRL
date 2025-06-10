using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Takes anim events from the genie Animatior and dispatches them so they can be handled elsewhere.
    /// </summary>
    public class GenieAnimEventDispatcher : MonoBehaviour
    {
        public event Action<FootstepType> OnFootstepAnimEvent;

        public event Action<SitType, bool> OnSitDownAnimEvent;

        public event Action OnPencilThrownAnimEvent;

        public event Action<EphemeralProp.ID> EphemeralPropAppear;

        public event Action EphemeralPropDisappear;

        public event Action OnHighFiveHit;

        public event Action OnSpawnItemForUser;
        public event Action OnTossItemBackwards;

        public enum FootstepType{ Walk, Run, Yaw}

        public enum SitType{ SitDown, StandUp}

        private Animator _animator;

        private void Awake() 
        {
            _animator = GetComponent<Animator>();
        }

        /// Called by the genie Animator when a footstep event is triggered.
        private void FootstepAnimEvent(FootstepType footstepType)
        {
            OnFootstepAnimEvent?.Invoke(footstepType);
        }

        /// Called by the genie Animator when a sit down event is triggered.
        private void SitOnLowSeatAnimEvent(SitType sitType)
        {
            OnSitDownAnimEvent?.Invoke(sitType, true);
        }

        private void SitOnHighSeatAnimEvent(SitType sitType)
        {
            OnSitDownAnimEvent?.Invoke(sitType, false);
        }

        // Called by the genie Animator when the pencil is thrown.
        private void ThrowPencilEvent() 
        {
            OnPencilThrownAnimEvent?.Invoke();
        }

        // Called by the Genie Animator when an ephemeral prop needs to appear.
        private void EphemeralPropAppearEvent(AnimationEvent evt)
        {
             if (IsAnimationTransitioningOut(evt)) return; // Don't handle if we're transitioning away from the animation
            // (presumably due to the action being cancelled)

            EphemeralProp.ID id = (EphemeralProp.ID)evt.intParameter;

            EphemeralPropAppear?.Invoke(id);
        }

        // Called by the Genie Animator when an ephemeral prop needs to disappear.
        private void EphemeralPropDisappearEvent(AnimationEvent e)
        {
            EphemeralPropDisappear?.Invoke();
        }

        private void HighFiveHit()
        {
            OnHighFiveHit?.Invoke();
        }

        private void SpawnItemForUser(AnimationEvent evt)
        {
            if (IsAnimationTransitioningOut(evt)) return; // Don't handle if we're transitioning away from the animation
            // (presumably due to the action being cancelled)

            OnSpawnItemForUser?.Invoke();
        }

        private void TossItemBackwards()
        {
            OnTossItemBackwards?.Invoke();
        }

        /// <summary>
        /// Checks if the Animation is on its way out. In many cases we would want to ignore animation events during this period.
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        private bool IsAnimationTransitioningOut(AnimationEvent e)
        {
            const int layer = 0;                        // or pass in via intParameter
            var clip = e.animatorClipInfo.clip;

            if (_animator.IsInTransition(layer))        // we're blending
            {
                // The outgoing state's clip list always contains the clip(s) that are fading OUT.
                foreach (var info in _animator.GetCurrentAnimatorClipInfo(layer))
                {
                    if (info.clip == clip)
                    {
                        // this event is from the state we're LEAVING
                        return true;
                    }
                }
            }

            return false;
        }
    }
}