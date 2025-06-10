using UnityEngine;

namespace GeniesIRL 
{
    public class GumBubble : MonoBehaviour
    {
        public GameObject sphere;

        private float _maxSphereSize;

        private Animator _genieAnimator;


        private void Awake() 
        {
            _maxSphereSize = sphere.transform.localScale.x;

            
        }

        private void OnEnable() 
        {
            sphere.transform.localScale = Vector3.zero;
        }

        private void LateUpdate() 
        {
            if (_genieAnimator == null) 
            {
                Genie genie = GetComponentInParent<Genie>();
                Debug.Assert(genie != null, "GumBubble must be a child of a Genie.");
                _genieAnimator = genie.genieAnimation.Animator;
                Debug.Assert(_genieAnimator != null, "Genie must have an Animator component.");
            }

            // Grab a float property called "gumBubble" from the animator.
            float gumBubble = _genieAnimator.GetFloat("gumBubble");
            gumBubble = Mathf.Clamp(gumBubble, 0, Mathf.Infinity);

            // Use that to scale the sphere.
            sphere.transform.localScale = Vector3.one * gumBubble * _maxSphereSize;
        }
    }
}
