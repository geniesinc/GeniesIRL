using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GeniesIRL 
{
    public static class AnimatorExtensions
    {
        /// <summary>
        /// Retrieves an AnimationClip from the Animator that matches the given name.
        /// </summary>
        /// <param name="animator">The Animator to search.</param>
        /// <param name="clipName">The name of the AnimationClip to retrieve.</param>
        /// <returns>The AnimationClip with the specified name, or null if not found.</returns>
        public static AnimationClip GetAnimationClipByName(this Animator animator, string clipName)
        {
            if (animator == null)
            {
                Debug.LogError("Animator is null.");
                return null;
            }

            if (string.IsNullOrEmpty(clipName))
            {
                Debug.LogError("Clip name is null or empty.");
                return null;
            }

            // Retrieve all AnimationClips from the Animator's runtime animator controller
            var runtimeAnimatorController = animator.runtimeAnimatorController;
            if (runtimeAnimatorController == null)
            {
                Debug.LogError("Animator does not have a RuntimeAnimatorController.");
                return null;
            }

            AnimationClip clip = runtimeAnimatorController.animationClips.FirstOrDefault(clip => clip.name == clipName);

            if (clip == null)
            {
                Debug.LogError("Clip not found: " + clipName);
            }

            return clip;
        }

        /// <summary>
        /// If you have an Animator trigger that has the same name as its Animation clip, you can use this to fire the trigger and get the clip 
        /// in a single function call, which you can use to get the duration. (This is useful for waiting for an animation to finish, but you might run into
        /// difficulty if you've changed the speed of the animation state in the Animator!)
        /// </summary>
        /// <param name="animator"></param>
        /// <param name="triggerName"></param>
        /// <returns></returns>
        public static AnimationClip SetTriggerAndReturnClip(this Animator animator, string triggerName)
        {
            animator.SetTrigger(triggerName);
            return animator.GetAnimationClipByName(triggerName);
        }

        /// <summary>
        /// Checks if the animator is currently in the specified state on any layer.
        /// </summary>
        /// <param name="animator">The Animator component.</param>
        /// <param name="stateName">The name of the state to check.</param>
        /// <returns>True if the animator is in the specified state on any layer; otherwise, false.</returns>
        public static bool IsInState(this Animator animator, string stateName)
        {
            if (animator == null)
                throw new ArgumentNullException(nameof(animator));
            if (string.IsNullOrEmpty(stateName))
                throw new ArgumentException("State name cannot be null or empty.", nameof(stateName));

            // Loop through all layers in the Animator
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetCurrentAnimatorStateInfo(i).IsName(stateName))
                {
                    return true;
                }
            }
            return false;
        }

        public static bool IsInState(this Animator animator, List<string> stateNames)
        {
            for (int i = 0; i < animator.layerCount; i++)
            {
                foreach (string stateName in stateNames)
                {
                    if (animator.GetCurrentAnimatorStateInfo(i).IsName(stateName))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static bool IsStateClipComplete(this Animator animator, string stateName) 
        {
            if (animator == null)
                throw new ArgumentNullException(nameof(animator));
            if (string.IsNullOrEmpty(stateName))
                throw new ArgumentException("State name cannot be null or empty.", nameof(stateName));

            // Loop through all layers in the Animator
            for (int i = 0; i < animator.layerCount; i++)
            {
                if (animator.GetCurrentAnimatorStateInfo(i).IsName(stateName) && animator.GetCurrentAnimatorStateInfo(i).normalizedTime >= 0.99f)
                {
                    return true;
                }
            }
            return false;
        }
    }
}

