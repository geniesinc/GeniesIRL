using System;
using System.Collections;
using UnityEngine;

namespace GeniesIRL
{
    /// <summary>
    /// Oversees objects relating to UI. Here, we attempt to separate UI logic
    /// from the systems that drive it, and visa-versa.
    /// </summary>
    public class UIManager : GeniesIrlSubManager
    {
        public MainMenu Menu { get; private set; }

        /// <summary>
        /// Set externally by LaunchUX to prevent the Menu from spawning prematurely during the tutorial.
        /// </summary>
        public bool EnableMenuSpawn { get; set; } = true;
        [SerializeField] private MainMenu menuPrefab;
        [SerializeField] private GeniesPrespawnUI geniesPrespawnUiPrefab;
        [SerializeField] private GeniesIrlTutorial tutorialPrefab;
        private GeniesPrespawnUI _currentGeniesPrespawnUi;
        private GenieManager.GenieManagerState _prevGenieManagerState;
        private GeniesIrlTutorial _tutorial;

         [Header("Splash")]
        [SerializeField] private GameObject splashPrefab;
        [SerializeField] private float splashDistFromCamera = 1.5f;

        public IEnumerator ShowTutorialSlide_C(TutorialSlide.SlideID slideID)
        {
            if (_tutorial == null)
            {
                _tutorial = Instantiate(tutorialPrefab);
            }

            yield return _tutorial.ShowSlide_C(slideID);
        }

        public void HideTutorial()
        {
            if (_tutorial == null) return;

            Destroy(_tutorial.gameObject);
        }

        public void SpawnSplashScreen()
        {
            // Create the splash screen.
            Transform head = Bootstrapper.XRNode.xrInputWrapper.Head;
            Vector3 forward = head.forward;
            forward.y = 0f; // Ignore the y component of the forward vector.
            forward.Normalize(); // Normalize the vector to get a direction.
            Splash splash = Instantiate(splashPrefab, head.position + forward * splashDistFromCamera, Quaternion.identity).GetComponent<Splash>();
            splash.transform.LookAt(head.position);
        }
        
        private void Start()
        {
            ListenForGestureEvents();
        }

        private void Update()
        {
            if (Bootstrapper.LaunchUX.Complete) 
            {
                UpdateGeniesPrespawnUI();
            }
        }

        private void UpdateGeniesPrespawnUI()
        {
            // Observe the GenieManager State
            GenieManager.GenieManagerState genieManagerState = Bootstrapper.GenieManager.state;

            bool didStateChange = genieManagerState != _prevGenieManagerState;

            if (didStateChange)
            {
                // Dispose of any current prespawn UI to respond to state change.
                if (_currentGeniesPrespawnUi != null)
                {
                    _currentGeniesPrespawnUi.Die();
                    _currentGeniesPrespawnUi = null;
                }
            }

            bool needPrespawnUI = genieManagerState == GenieManager.GenieManagerState.TryingToSpawnGenie ||
                                  genieManagerState == GenieManager.GenieManagerState.TryingToTeleportGenie;

            if (needPrespawnUI)
            {
                // We need the Prespawn UI. If we don't have one, spawn it now.
                if (_currentGeniesPrespawnUi == null)
                {
                    Debug.Log("Spawning Genies Prespawn UI");
                    _currentGeniesPrespawnUi = Instantiate(geniesPrespawnUiPrefab);
                    _currentGeniesPrespawnUi.OnSpawned(genieManagerState);
                }
            }

            // If the prespawn UI is active, we want to move the Menu to the side so the user can see the prespawn UI and Genie once it spawns.
            if (didStateChange && Menu != null)
            {
                Debug.Log("Keeping out of the way of the user.");
                Menu.KeepOutOfTheWayOfTheUser(needPrespawnUI,
                                              Bootstrapper.XRNode.xrOrigin.Camera.transform);
            }

            _prevGenieManagerState = genieManagerState;
        }

        private void ListenForGestureEvents()
        {
            UserHandGesture[] gestures = Bootstrapper.XRNode.xrInputWrapper.hands.xrHandGestureManager.GetHandGesture(HandGestures.ThumbsUp, InputHand.Both);

            foreach (UserHandGesture gesture in gestures)
            {
                gesture.OnGesturePerformed += OnGesturePerformed;
            }

            if (Application.isEditor) 
            {
                StartCoroutine(CheckOpenPalmGestureInEditor_C());
            }
        }

        private IEnumerator CheckOpenPalmGestureInEditor_C()
        {
           // In the Editor, you can use the middle mouse button or space key to spawn/recall the menu.
           while (true) 
           {
                if (Input.GetMouseButtonDown(2) || Input.GetKeyDown(KeyCode.Space))
                {
                    OnGesturePerformed(InputHand.Undefined);
                }

                yield return null;
           }
        }

        private void OnGesturePerformed(InputHand inputHand)
        {
            if (!EnableMenuSpawn) return;

            Vector3 userHandPosition;
            Vector3 userHeadPosition;

            Transform userHead = GeniesIrlBootstrapper.Instance.XRNode.xrOrigin.Camera.transform;
            userHeadPosition = userHead.position;

            // Without PolySpatial, the user doesn't have hands. Just place the menu in front of the user's face.
            if (inputHand == InputHand.Undefined) 
            {
                Vector3 forward = userHead.forward;
                forward.x = 0; // Disregard head pitch.
                forward.Normalize();

                userHandPosition  = userHeadPosition + userHead.forward * 0.5f;
            }
            else if (inputHand == InputHand.Right || inputHand == InputHand.Both)
            {
                userHandPosition = Bootstrapper.XRNode.xrInputWrapper.hands.RightHandPalm.position;
            }
            else
            {
                userHandPosition = Bootstrapper.XRNode.xrInputWrapper.hands.LeftHandPalm.position;
            }

            // If menu hasn't been spawned, spawn it.
            if (Menu == null)
            {
                Menu = Instantiate(menuPrefab);
                Menu.OnSpawnedByUIManager(this);
            }
            
            // Otherwise, teleport to user.
            Menu.AppearInFrontOfUser(userHeadPosition, userHandPosition, true);
        }
    }
}

