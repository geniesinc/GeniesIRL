using System;
using System.Collections;
using UnityEngine;
using UnityEngine.XR;

namespace GeniesIRL 
{
    /// <summary>
    /// In charge of initialization UX, for example, instructional text, scan validation, etc.
    /// </summary>
    public class LaunchUX : GeniesIrlSubManager
    {
        public bool Complete { get; private set; }
        public event Action OnLaunchComplete;
        public LaunchValidation launchValidation;

        [SerializeField, Tooltip("The number of seconds to wait after the validation is complete before moving on.")]
        private float extraScanSeconds = 30f;

        [SerializeField, Tooltip("The duration to play the splash screen before moving on. The timeline of the splash screen is longer because we allow the music to continue and fade out. ")] 
        private float splashCoreDuration = 8f;

        public override void OnSceneBootstrapped(GeniesIrlBootstrapper bootstrapper)
        {
            base.OnSceneBootstrapped(bootstrapper);

            if (bootstrapper.DebugSkipLaunchUX)
            {
                Complete = true;
                OnLaunchComplete?.Invoke();
                return;
            }

            Bootstrapper.UIManager.EnableMenuSpawn = false; // Disable the menu spawn until we reach that part of the tutorial.

            StartCoroutine(RunLaunchSequence_C());
        }

        private IEnumerator RunLaunchSequence_C()
        {
            // Don't start at all until we have head tracking.
            yield return new WaitUntil(() => Bootstrapper.XRNode.HasAquiredTracking);

            yield return new WaitForSeconds(1f); // Wait a second to let the app/framerate settle before starting things.

            // Temporarily Hide spatial mesh occlusion so the user can see the tutorial.
            GlobalEventManager.Trigger(new GlobalEvents.DebugEnableSpatialMeshOcclusion(false));

            UIManager uiManager = Bootstrapper.UIManager;
            uiManager.SpawnSplashScreen(); // Show the splash screen.
            
            yield return new WaitForSeconds(splashCoreDuration); // Wait for the visual part of the splash screen to.

            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.Welcome));
            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.YouAreAbout));
            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.StartByLookingAround));

            // Allow the user to see the spatial mesh grid and occlusion.
            GlobalEventManager.Trigger(new GlobalEvents.DebugShowSpatialMesh(true));

            uiManager.HideTutorial();
            
            launchValidation.StartValidation(Bootstrapper);

            while (!launchValidation.IsValidationComplete) yield return null; // Wait while the validation completes.

            yield return new WaitForSeconds(extraScanSeconds); // Wait another few seconds for good measure.

            // Hide the spatial mesh grid and occlusion to see the spatial mesh in action.
            GlobalEventManager.Trigger(new GlobalEvents.DebugShowSpatialMesh(false));

            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.ThisIsAGreatStart));
            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.BeSureToKeepScanning));
            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.HowToScan));
            yield return StartCoroutine(uiManager.ShowTutorialSlide_C(TutorialSlide.SlideID.NowSummonTheMenu));

            // Enable the user to open the menu.
            uiManager.EnableMenuSpawn = true;

            // Wait for the user to open the menu.
            while (Bootstrapper.UIManager.Menu == null) yield return null;

            uiManager.HideTutorial();

            // Reactivate spatial mesh occlusion for the rest of the app.
            GlobalEventManager.Trigger(new GlobalEvents.DebugEnableSpatialMeshOcclusion(true));

            yield return new WaitForSeconds(1f); // Pause for a moment befrore moving on.

            Complete = true;

            OnLaunchComplete?.Invoke();
        }
    }
    
}

