using UnityEngine;

public class TeleportParticlesController : MonoBehaviour
{
    [SerializeField] ParticleBurstController[] particleBurstPrefabs;

    private AudioSource audioSource; // Optionally, there can be an audio source.

    private int activeParticleBurstCount = 0;
    void Start()
    {
        // instantiate all particle bursts
        for(int i = 0; i < particleBurstPrefabs.Length; i++)
        {
            // instantiate the particle burst prefab beneath this game object
            var burst = Instantiate(particleBurstPrefabs[i], transform);
            burst.transform.localPosition = Vector3.zero;
            burst.transform.localRotation = Quaternion.identity;
            // We don't need to unsubscribe this because we are destroying the object
            burst.OnParticleBurstDestroyed += DecrementParticleBurstCount;
        }

        // track so we can destroy ourselves
        activeParticleBurstCount = particleBurstPrefabs.Length;

        audioSource = GetComponent<AudioSource>(); // Optionally, there can be an audio source.

        if (audioSource != null)
        {
            audioSource.Play();
        }
    }

    void DecrementParticleBurstCount()
    {
        activeParticleBurstCount--;
    }

    private void Update()
    {
        bool areParticlesDone = activeParticleBurstCount <= 0;

        bool isAudioDone = audioSource == null || !audioSource.isPlaying;

        if (areParticlesDone && isAudioDone)
        {
            Destroy(gameObject);
        }
    }
}
