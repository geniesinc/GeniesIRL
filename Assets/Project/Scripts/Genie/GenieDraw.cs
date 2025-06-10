using System;
using System.Collections;
using GeniesIRL;
using UnityEngine;


namespace GeneisIRL 
{
    /// <summary>
    /// Facilitates the Genie's ability to draw on walls.
    /// </summary>
    [System.Serializable]
    public class GenieDraw
    {
        public WallDrawing drawingPrefab;

        [Range(0.1f, 2f)]
        [Tooltip("Ideal drawing distance for drawing on walls. Must be less than the max drawing distance.")]
        public float idealDrawingDistance = 0.35f;
        [Range(0.1f, 2f)]
        [Tooltip("Max drawing distance for drawing on walls. Must be greater than the ideal drawing distance.")]
        public float maxDrawingDistance = 0.75f;

        [Header("Audio")]
        public AudioSource drawingAudio;

        [NonSerialized]
        private Genie _genie;

        private Coroutine _coroutine;

        private WallDrawing _latestDrawing;

        public void OnStart(Genie genie)
        {
            _genie = genie;
        }

        /// <summary>
        /// Performs the drawing animation and spawns a drawing at the given drawing pose.
        /// </summary>
        /// <param name="drawingPose"></param>
        /// <returns></returns>
        public void Draw(Pose drawingPose, Action finishedAction = null)
        {
            if (_coroutine != null)
            {
                _genie.StopCoroutine(_coroutine);
            }
           
            _coroutine = _genie.StartCoroutine(Draw_C(drawingPose, finishedAction));
        }

        private IEnumerator Draw_C(Pose drawingPose, Action finishedAction = null)
        {
            if (drawingPrefab != null)
            {
                 // Instantiate the drawing prefab at the Genie's hand position
                _latestDrawing = GameObject.Instantiate<WallDrawing>(drawingPrefab, drawingPose.position, drawingPose.rotation);
            }
            else 
            {
                Debug.LogError("No drawing prefab assigned to GenieDraw. Cannot draw spawn drawing.");
            }

            drawingAudio.Play();

            string triggerAndStateName = "DrawOnWall";

            Animator genieAnimator = _genie.genieAnimation.Animator;
            genieAnimator.SetTrigger(triggerAndStateName);
            
            yield return new WaitUntil(() => genieAnimator.IsInState(triggerAndStateName)); // wait for the animation to start.

            yield return new WaitUntil(() => genieAnimator.IsStateClipComplete(triggerAndStateName)); // wait for the animation to finish.
            
            _genie.genieEphemeralProps.DisableAllProps(); // Hide the sharpie

            finishedAction?.Invoke();
        }

        /// <summary>
        /// If the DrawOnWall Action is externally interrupted, this method can be called to cancel the drawing.
        /// </summary>
        public void ExternallyCancelDraw()
        {
            if (_coroutine != null)
            {
                _genie.StopCoroutine(_coroutine);
            }

            if (_latestDrawing != null)
            {
               _latestDrawing.myAnimation.PauseCurrentlyPlayingAnimation();
            }

            drawingAudio.Stop();

            _genie.genieEphemeralProps.DisableAllProps(); // Hide the sharpie
        }
    }
}
