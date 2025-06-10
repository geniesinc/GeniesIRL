using System.Runtime.CompilerServices;
using UnityEngine;

namespace GeniesIRL 
{
    /// <summary>
    /// Attach this to a Particle System and it'll wait until that Particle System has finished playing before destroying itself.
    /// Additionally, if there's an AudioSource attached to the same GameObject, it'll wait until the AudioSource has finished playing before destroying itself.
    /// </summary>
    public class SingleShotFX : MonoBehaviour
    {
        private ParticleSystem _particleSystem;
        private AudioSource _audioSource;

        private bool _isParticleSystemDone; 
        private bool _isAudioSourceDone;

        private void Awake()
        {
            _particleSystem = GetComponent<ParticleSystem>();
            _audioSource = GetComponent<AudioSource>();

            if (_particleSystem != null)
            {
                if (_particleSystem.main.loop) Debug.LogError("ParticleSystem is set to loop. This script is intended for single-shot effects.");

                if (!_particleSystem.isPlaying) _particleSystem.Play();
            }
            else 
            {
                _isParticleSystemDone = true;
            }

            if (_audioSource != null)
            {
                 if (_audioSource.loop) Debug.LogError("AudioSource is set to loop. This script is intended for single-shot effects.");

                if (!_audioSource.isPlaying) _audioSource.Play();
            }
            else 
            {
                _isAudioSourceDone = true;
            }
        }

        private void Update()
        {
            if (_particleSystem != null)
            {
                // Wait for particle system to finish playing
                if (!_particleSystem.isPlaying)
                {
                    _isParticleSystemDone = true;
                }
            }

            if (_audioSource != null)
            {
                // Wait for audio source to finish playing
                if (!_audioSource.isPlaying)
                {
                    _isAudioSourceDone = true;
                }
            }

            if (_isParticleSystemDone && _isAudioSourceDone)
            {
                Destroy(gameObject);
            }
        }
    }
}
