using System;
using System.Collections;
using UnityEngine;

namespace GeniesIRL 
{
    public class GeniesIrlTutorial : MonoBehaviour
    {   
        [SerializeField, Tooltip("The duration at which each character is revealed.")]
        private float crawlTimePerCharacter = 0.05f;

        [SerializeField, Tooltip("Users can skip slides by gaze-and-pinching the button.")]
        private SpatialButton button;

        [SerializeField] private GameObject pinchIcon;

        [SerializeField, Tooltip("If ticked, the last slide will be unskippable and will not auto-hide." +
        " Useful to ensure the user takes a specific action before proceeding.")] 
        private bool showFinalSlideIndefinitely = true;

        [Header("Audio")]
        [SerializeField] GameObject openFxPrefab;

        [Header("Audio")]
        [SerializeField] GameObject closeFxPrefab;

        private TutorialSlide[] tutorialSlides;

        private TutorialSlide currentSlide = null;

        public IEnumerator ShowSlide_C(TutorialSlide.SlideID slideID)
        {
            DisableAllSlides(); // Set all slides inactive so we can start clean.

            pinchIcon.SetActive(false); // Hide the pinch icon while we reveal text (user can still pinch to show all text, but we hide the icon to avoid confusing them).

            currentSlide = GetSlideData(slideID);

            if (currentSlide == null) yield break;

            currentSlide.RevealCharacterByCharacter(crawlTimePerCharacter);
            
            while (!currentSlide.IsAllTextRevealed) yield return null; // Wait until the slide is fully revealed.
            
             // If the last slide is set to be shown indefinitely, and this is the last slide, we have slightly different behavior.
            if (showFinalSlideIndefinitely && IsLastSlide(currentSlide)) 
            {
                button.gameObject.SetActive(false); // Disable the button if the last slide is set to be shown indefinitely.
                pinchIcon.SetActive(false); // Disable the pinch icon too.
                yield break; // We don't need to go on.
            }

            pinchIcon.SetActive(true); // Show the pinch icon to indicate the user can skip the slide.

            // If the text was revealed automatically, wait for the hold duration before automatically moving on.
            if (!currentSlide.WasTextRevealedManually)
            {
                // Wait for the hold duration before moving on, (but also allow the player to skip)
                float endTime = Time.time + currentSlide.holdDurationBeforeAutoContinue;

                while (Time.time < endTime && !currentSlide.IsFullyHidden) yield return null;
            
                currentSlide.FadeOutAndHide(); // If it's not already hidden, do a soft fade to hide the slide.
            }

            while (!currentSlide.IsFullyHidden) yield return null; // Wait until the slide is fully hidden.
        }

        private void Awake()
        {
            tutorialSlides = GetComponentsInChildren<TutorialSlide>(true);

            DisableAllSlides();

            button.OnPressButton.AddListener(OnSkipAction);

            Instantiate(openFxPrefab, transform.position, Quaternion.identity);
        }

        private void OnDestroy()
        {
            Instantiate(closeFxPrefab, transform.position, Quaternion.identity);
        }

        private void DisableAllSlides()
        {
            foreach (TutorialSlide slide in tutorialSlides)
            {
                slide.HideInstantly();
            }
        }

        private TutorialSlide GetSlideData(TutorialSlide.SlideID slideID)
        {
            foreach (TutorialSlide slide in tutorialSlides)
            {
                if (slide.slideID == slideID)
                {
                    return slide;
                }
            }

            Debug.LogError("No slide data found for " + slideID);
            return null;
        }

        private void Update()
        {
            if (Application.isEditor && Input.GetKeyDown(KeyCode.Return))
            {
                OnSkipAction();
            }
        }

        private void OnSkipAction()
        {
            if (currentSlide == null) return; // Nothing to skip.

            if (!currentSlide.IsAllTextRevealed)
            {
                currentSlide.RevealAll();
                return;
            }
            
            if (showFinalSlideIndefinitely && IsLastSlide(currentSlide))
            {
                return; // Don't skip the last slide if it's set to be shown indefinitely.
            }

            currentSlide.HideInstantly();
            
        }

        private bool IsLastSlide(TutorialSlide slide)
        {
            return slide == tutorialSlides[tutorialSlides.Length - 1];
        }
    }
}

