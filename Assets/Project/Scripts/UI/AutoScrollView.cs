using UnityEngine;
using UnityEngine.UI;
using System.Collections;

namespace GeniesIRL 
{
    /// <summary>
    /// Attach this component to a ScrollView (on the same GameObject as the ScrollRect).
    /// When the object is enabled it resets the scroll position to the top, waits for a
    /// configurable delay, then continuously auto-scrolls to the bottom and back up at
    /// the same pace, pausing for the same delay each time it changes direction.
    /// </summary>
    [RequireComponent(typeof(ScrollRect))]
    public class AutoScrollView : MonoBehaviour
    {
        [Tooltip("Seconds to wait before each auto‑scroll leg begins (including the first).")]
        [SerializeField] private float delayBeforeStart = 3f;

        [Tooltip("Time (in seconds) for a full scroll from top to bottom, or bottom to top.")]
        [SerializeField] private float scrollDuration = 10f;

        private ScrollRect scrollRect;
        private Coroutine scrollRoutine;
        private bool scrollingDown = true; // start by scrolling down after the initial delay

        private void Awake()
        {
            scrollRect = GetComponent<ScrollRect>();
            // Ensure vertical scrolling is enabled; horizontal will be ignored.
            scrollRect.horizontal = false;
        }

        private void OnEnable()
        {
            ResetToTop();

            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
            }
            scrollRoutine = StartCoroutine(AutoScrollLoop());
        }

        private void OnDisable()
        {
            if (scrollRoutine != null)
            {
                StopCoroutine(scrollRoutine);
            }
        }

        /// <summary>
        /// Instantly sets the scroll position to the very top of the content.
        /// </summary>
        private void ResetToTop()
        {
            scrollRect.verticalNormalizedPosition = 1f; // 1 = top, 0 = bottom
        }

        /// <summary>
        /// Loops indefinitely: waits <see cref="delayBeforeStart"/>, scrolls to the target edge over
        /// <see cref="scrollDuration"/>, flips direction, then repeats.
        /// </summary>
        private IEnumerator AutoScrollLoop()
        {
            while (true)
            {
                // Pause before starting each movement (including the very first)
                yield return new WaitForSeconds(delayBeforeStart);

                // Determine start and end positions for this leg of the scroll.
                float startPos = scrollRect.verticalNormalizedPosition;
                float endPos = scrollingDown ? 0f : 1f;
                float elapsed = 0f;

                // Scroll over time.
                while (elapsed < scrollDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.Clamp01(elapsed / scrollDuration);
                    scrollRect.verticalNormalizedPosition = Mathf.Lerp(startPos, endPos, t);
                    yield return null;
                }

                // Snap exactly to the end position to avoid precision drift.
                scrollRect.verticalNormalizedPosition = endPos;

                // Reverse direction for the next loop.
                scrollingDown = !scrollingDown;
            }
        }
    }
}

