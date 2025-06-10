using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Handles things like footsteps, run/jump sounds, and clothes shuffling.
    /// </summary>
    [System.Serializable]
    public class GenieLocomotionAudio
    {
        public GenieAnimEventDispatcher footstepAnimEventDispatcher;

        [Header("Footsteps")]
        public AudioSource runFootstepAudioSource;
        public AudioSource walkFootstepAudioSource;
        public AudioSource yawFootstepAudioSource;

        public AudioClip[] runningFootstepClips;
        public AudioClip[] walkingFootstepClips;
        public AudioClip[] yawFootstepClips;

        [NonSerialized]
        private Genie _genie;

        private float _lastFootstepTimestamp;

        public void OnStart(Genie genie)
        {
            _genie = genie;

            // Setup footstep system. These will play on each footstep event triggered by the Animator.
            footstepAnimEventDispatcher.OnFootstepAnimEvent += OnFootstep;
        }

        private void OnFootstep(GenieAnimEventDispatcher.FootstepType footstepType)
        {
            if (Time.time - _lastFootstepTimestamp < 0.15f) return; // Don't play a footstep sound if we've just played one. (This is to prevent double footsteps when the animator triggers the event twice in a row).
            
            switch (footstepType)
            {
                case GenieAnimEventDispatcher.FootstepType.Run:
                    PlayRandomClip(runningFootstepClips, runFootstepAudioSource);
                    break;
                case GenieAnimEventDispatcher.FootstepType.Walk:
                    PlayRandomClip(walkingFootstepClips, walkFootstepAudioSource);
                    break;
                case GenieAnimEventDispatcher.FootstepType.Yaw:
                    PlayRandomClip(yawFootstepClips, yawFootstepAudioSource);
                    break;
            }
            
            _lastFootstepTimestamp = Time.time;
        }

        private void PlayRandomClip(AudioClip[] footstepsClips, AudioSource audioSource)
        {
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            AudioClip clip = footstepsClips[UnityEngine.Random.Range(0, footstepsClips.Length)];
            audioSource.PlayOneShot(clip);
        }

        
    }
}

