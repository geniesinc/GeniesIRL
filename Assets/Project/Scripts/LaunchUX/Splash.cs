using UnityEngine;
using UnityEngine.Playables;

namespace GeniesIRL 
{
    public class Splash : MonoBehaviour
    {
        [SerializeField] private PlayableDirector playableDirector;

        public void Play()
        {
            playableDirector.Play();
            GameObject.Destroy(gameObject, (float)playableDirector.duration);
        }
    }
}
