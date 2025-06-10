using UnityEngine;
using UnityEngine.Animations;

namespace GeniesIRL 
{
    /// <summary>
    /// An ephemeral prop appears and disappears to support the Genie's animations. For example, for her "draw on wall" animation,
    /// a Sharpie will appear in her hand.
    /// </summary>
    public class EphemeralProp : MonoBehaviour
    {
        public enum ID {None, Smartphone, Sharpie, GumBubble}
        public ID myID;
        public HumanBodyBones boneToAttachTo = HumanBodyBones.LeftHand;
        public Vector3 localOffset;
        public Vector3 localRotationalOffset;
        private ParentConstraint _parentConstraint;

        public void Appear(Animator characterAnimator)
        {
            gameObject.SetActive(true);

            if (characterAnimator == null)
            {
                Debug.LogError("Animator not assigned!");
                return;
            }

            // Get the transform of the specified bone.
            Transform boneTransform = characterAnimator.GetBoneTransform(boneToAttachTo);
            if (boneTransform != null)
            {
                // Check if we already have a ParentConstraint. If not, add one.
                _parentConstraint = GetComponent<ParentConstraint>();
                if (_parentConstraint == null)
                {
                    _parentConstraint = gameObject.AddComponent<ParentConstraint>();
                }
                
                // Reset any existing sources to start fresh.
                _parentConstraint.RemoveAllSources();

                // Create and add the constraint source.
                ConstraintSource source = new ConstraintSource
                {
                    sourceTransform = boneTransform,
                    weight = 1f
                };
                _parentConstraint.AddSource(source);

                // Activate the constraint.
                _parentConstraint.constraintActive = true;

                // Apply initial offsets.
                _parentConstraint.SetTranslationOffset(0, localOffset);
                _parentConstraint.SetRotationOffset(0, localRotationalOffset);
            }
            else
            {
                Debug.LogWarning("Could not find the bone transform for " + boneToAttachTo);
            }
        }

        public void Disappear()
        {
            gameObject.SetActive(false);
        }

        void Update()
        {
            // In Update(), continually apply the offset values so changes in the Editor are reflected immediately.
            if (_parentConstraint != null && _parentConstraint.sourceCount > 0)
            {
                _parentConstraint.SetTranslationOffset(0, localOffset);
                _parentConstraint.SetRotationOffset(0, localRotationalOffset);
            }
        }
    }
}
