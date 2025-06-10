using System.Collections;
using TMPro;
using UnityEngine;

namespace GeniesIRL 
{
    public class TutorialSlide : MonoBehaviour
    {
        public enum SlideID { 
            None,
            Welcome,
            YouAreAbout,
            StartByLookingAround,
            ThisIsAGreatStart,
            BeSureToKeepScanning, 
            HowToScan,
            NowSummonTheMenu
        }

        public bool IsAllTextRevealed => text.maxVisibleCharacters == text.textInfo.characterCount;

        public bool WasTextRevealedManually { get; private set; }

        public bool IsFullyHidden => !gameObject.activeSelf;

        public SlideID slideID;
        public TextMeshProUGUI text;

        [Tooltip("How long to hold after the crawl has finished before automatically moving onto the next slide. (Note that" + 
         " this is only relevant if the text was revealed automatically. Otherwise, the slide will wait for manual input before proceeding.)")]
        public float holdDurationBeforeAutoContinue = 2f;

        public void RevealCharacterByCharacter(float crawlTimePerCharacter)
        {
            // Reset flag.
            WasTextRevealedManually = false;

            // Stop any running reveal so we don’t overlap coroutines.
            StopAllCoroutines();

            gameObject.SetActive(true);
            StartCoroutine(RevealTextCoroutine_C(crawlTimePerCharacter));
        }

        private IEnumerator RevealTextCoroutine_C(float crawlTimePerCharacter)
        {
            // Make sure the text layout is up-to-date with the full text.
            text.ForceMeshUpdate();
            int totalCharacters = text.textInfo.characterCount;
            int visibleCount = 0;

            // Gradually increase the visible characters.
            while (visibleCount <= totalCharacters)
            {
                // By setting maxVisibleCharacters, the text is laid out as a whole
                // so that word-wrapping remains fixed. Only the visible characters are rendered.
                text.maxVisibleCharacters = visibleCount;
                visibleCount++;
                yield return new WaitForSeconds(crawlTimePerCharacter);
            }

            // Ensure the text is fully revealed.
            text.maxVisibleCharacters = totalCharacters;
        }

        public void RevealAll() 
        {
            // In case the object is somehow inactive, make sure it is.
            gameObject.SetActive(true); 

            // Set flag so we know not to automatically reveal the next slide.
            WasTextRevealedManually = true;

            // Stop any ongoing reveal.
            StopAllCoroutines();

            // Update the mesh to account for the full text.
            text.ForceMeshUpdate();
            int totalCharacters = text.textInfo.characterCount;
            text.maxVisibleCharacters = totalCharacters;
        }

        public void HideInstantly() 
        {
            StopAllCoroutines();
            gameObject.SetActive(false);
        }
        
        public void FadeOutAndHide() 
        {
            if (!gameObject.activeSelf) return; // If we're already hidden, don't do anything.
            StartCoroutine(FadeOutAndHide_C());
        }
        private IEnumerator FadeOutAndHide_C() 
        {
            float fadeTime = 0.5f;
            text.CrossFadeAlpha(0, fadeTime, false);
            yield return new WaitForSeconds(fadeTime);
            gameObject.SetActive(false);
        }
    }
}

