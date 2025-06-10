using System;
using UnityEngine;


namespace GeniesIRL 
{
    public class ItemAudio : MonoBehaviour
    {
        public AudioSource audioSource;
        public AudioClip grabClip;
        public float grabVolume = 1f;

        [Header("Impacts")]

        public bool enableImpact = true;
        public AudioClip[] softImpactClips;
        public float softImpactVolume = 0.1f;
        public float softImpactThreshold = 0.2f;
         public AudioClip[] mediumImpactClips;
        public float mediumImpactVolume = 0.3f;
        public float mediumImpactThreshold = 3f;
        public AudioClip[] hardImpactClips;
        public float hardImpactVolume = .6f;
        public float hardImpactThreshold = 5f;
        public float pitchMin = 0.8f;
        public float pitchMax = 1.2f;
    
        private float _latestImpactTimestamp = -1f;

        private float _timeStampAtAwake = -1f;

        private bool hasItemBeenReleased = false;

        private void Awake()
        {
            _timeStampAtAwake = Time.time;
            Item item = GetComponentInParent<Item>();
            item.OnGrabbed += OnGrabbed;
            item.OnReleased += OnReleased;
            item.OnPhysicalImpact += OnPhysicalImpact;
        }

        private void OnGrabbed(Item item)
        {
            if (Time.time - _timeStampAtAwake < 0.1f) return; // Don't play a "grab" sound if the item was spawned and grabbed at the same time. (We have a separate "spawn" sound for that).
            audioSource.pitch = 1f;
            audioSource.PlayOneShot(grabClip, grabVolume);
        }

        private void OnReleased(Item item)
        {
            hasItemBeenReleased = true;
        }

        private void OnPhysicalImpact(Item item, float force)
        {
            if (!enableImpact) return;
            
            if (Time.time - _timeStampAtAwake < 1f) return; // Don't play an impact sound if we've just spawned the item.

            if (Time.time - _latestImpactTimestamp < 0.15f) return; // Don't play an impact sound if we've just played one.
            
            // Don't play a sound if it hasn't been grabbed in a while (or never). This is to prevent changes in the AR environment from triggering an "impact" sound.
            if (!hasItemBeenReleased || Time.time - item.TimeSinceLastReleased > 5f) return; 

            // Randomize Clip
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            AudioClip clip = null;
            string str = "Impact force: " + force;

            float volume = 0f;
            if (force >= hardImpactThreshold)
            {
                str += " (HARD)";
                clip = hardImpactClips[UnityEngine.Random.Range(0, hardImpactClips.Length)];
                volume = hardImpactVolume;
            }
            else if (force >= mediumImpactThreshold)
            {
                str += " (MEDIUM)";
                clip = mediumImpactClips[UnityEngine.Random.Range(0, mediumImpactClips.Length)];
                volume = mediumImpactVolume;
            }
            else if (force >= softImpactThreshold)
            {
                str += " (SOFT)";
                clip = softImpactClips[UnityEngine.Random.Range(0, softImpactClips.Length)];
                volume = softImpactVolume;
            }

            if (clip == null) return; // No impact sound to play.

             // Randomize Pitch
            UnityEngine.Random.InitState((int)System.DateTime.Now.Ticks);
            float randPitch = UnityEngine.Random.Range(pitchMin, pitchMax);
            audioSource.pitch = randPitch;

            // Play the sound.
            audioSource.PlayOneShot(clip, volume);
            _latestImpactTimestamp = Time.time;

            Debug.Log(str);
        }
    }
}

