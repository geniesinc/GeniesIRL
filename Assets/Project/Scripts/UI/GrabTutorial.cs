using System;
using UnityEngine;
using UnityEngine.Animations;

namespace GeniesIRL 
{
    public class GrabTutorial : MonoBehaviour
    {
        public Animator animator;

        public ParentConstraint parentConstraint;

        public void Expand()
        {
            animator.SetBool("Expanded", true);
        }

        public void CollapseAndSelfDestruct()
        {
            animator.SetBool("Expanded", false);
            Destroy(gameObject, 1.0f);
        }

        public void Collapse()
        {
            animator.SetBool("Expanded", false);
        }

        public void AttachTo(Transform newParent)
        {
            if (parentConstraint != null)
            {
                parentConstraint.AddSource(new ConstraintSource
                {
                    sourceTransform = newParent,
                    weight = 1.0f
                });
                parentConstraint.constraintActive = true;
            }
            else
            {
                Debug.LogWarning("ParentConstraint is not assigned.");
            }
        }
    }
}