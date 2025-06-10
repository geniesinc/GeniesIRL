using System;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// High-level object that manages many of the the Genies SFX. Some sounds, such as the High Five Slap, are associated with the High Five FX prefab.
    /// </summary>
    [System.Serializable]
    public class GenieAudio
    {
        public GenieLocomotionAudio locomotionAudio;

        [Tooltip("Audio source for general single-shot sounds.")]
        public AudioSource singleShotGeneral;

        public GenieAnimEventDispatcher animEventDispatcher;

        [Header("Sit/Stand")]
        public AudioClip sitDownOnLowSeat;
        public float sitDownFromLowSeatVolume = .5f;
        public AudioClip standUpFromLowSeat;
        public float standUpFromLowSeatVolume = .5f;
        public AudioClip sitDownOnHighSeat;
        public float sitDownFromHighSeatVolume = .5f;
        public AudioClip standUpFromHighSeat;
        public float standUpFromHighSeatVolume = .5f;
        
        public void OnStart(Genie genie)
        {
            locomotionAudio.OnStart(genie);

            animEventDispatcher.OnSitDownAnimEvent += OnSitDownAnimEvent;
        }

        public void PlayGeneralSingleShotSound(AudioClip clip, float volume = 1f)
        {
            singleShotGeneral.PlayOneShot(clip, volume);
        }

        private void OnSitDownAnimEvent(GenieAnimEventDispatcher.SitType sitType, bool lowSeat)
        {
            AudioClip clip;
            float volume;
            if (lowSeat)
            {
                clip = sitType == GenieAnimEventDispatcher.SitType.SitDown ? sitDownOnLowSeat : standUpFromLowSeat;
                volume = sitType == GenieAnimEventDispatcher.SitType.SitDown ? sitDownFromLowSeatVolume : standUpFromLowSeatVolume;
            }
            else
            {
                clip = sitType == GenieAnimEventDispatcher.SitType.SitDown ? sitDownOnHighSeat : standUpFromHighSeat;
                volume = sitType == GenieAnimEventDispatcher.SitType.SitDown ? sitDownFromHighSeatVolume : standUpFromHighSeatVolume;
            }
            
            singleShotGeneral.PlayOneShot(clip, volume);
        }

    }
}