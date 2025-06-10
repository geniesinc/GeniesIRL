using System.Threading.Tasks;
using GeniesIRL;
using GeniesIRL.Utilities;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class DebugToggleAction
{
    public string Label;
    public bool DefaultState;
    public UnityAction<bool> Action;

    public DebugToggleAction(string label, bool defaultState, UnityAction<bool> action)
    {
        Label = label;
        DefaultState = defaultState;
        Action = action;
    }
}

public class MainMenu : MonoBehaviour
{
    /// <summary>
    /// A reference to the UI Manager that spawned this Menu. You can use this to access the GeniesIrlBootstrapper.
    /// Note that if you're testing the as a scene GameObject Menu in an isolated test scene, this field will be null.
    /// </summary>
    public UIManager UIManager { get; private set; }

    [SerializeField] private SpatialButton _closeBtn;
    [SerializeField] private SpatialButton _teleportBtn;
    [SerializeField] private SpatialButton _celebrateBtn;
    [SerializeField] private SpatialButton _celebrateBtn_modalDialog;
    [SerializeField] private SpatialButton _backBtn;
    [SerializeField] private SpatialButton _backBtn_modalDialog;
    [SerializeField] private SpatialButton _debugMenuBtnPrefab;
    [SerializeField] private MenuHandle _menuHandle;
    [SerializeField] private SmoothLookAt _menuHandleSmoothLookAt;
    [SerializeField] private RectTransform _canvasMain;
    [SerializeField] private Vector2 _initialCanvasMainScale;
    [SerializeField] private Vector2 _expandedCanvasMainScale;
    [SerializeField] private Transform _menuBackplate;
    [SerializeField] private float _canvasDepth = -0.04f;
    [SerializeField] private float _initialMenuHandleOffsetY = -0.25f;
    [SerializeField] private Vector3 _initialMenuBackplateScale;

    [SerializeField] private GameObject _menuOpenedFX;
    [SerializeField] private Celebration _celebrationPrefab;
    [SerializeField] private GameObject _toDoSubmenu;
    [SerializeField] private GameObject _debugSubmenu;
    [SerializeField] private GameObject _debugSubmenuList;
    [SerializeField] private GameObject _celebrateModalDialog;
    [SerializeField] AudioSource _recordScratchAudioSource;
    [SerializeField] private TextMeshProUGUI _menuTitleText;
    [SerializeField] private ModalMenu _infoModalDialog;
    [SerializeField] private ToDoElement _toDoElementPrefab;
    [SerializeField] private DebugButton _debugButtonPrefab;
    [SerializeField] private bool _debugShowCelebrationBtnImmediately;
    [SerializeField] private Texture2D[] _brainTaskScreencaps;
    [SerializeField] private SpatialButton _quitBtn;
    [SerializeField] private SpatialButton _aboutButton;
    [SerializeField] private SpatialButton _backBtn_aboutModalDialog;
    [SerializeField] private GameObject _aboutModalDialog;

    // Constants
    const string TODO_SUBMENU_NAME = "Things Genie Can Do";
    const string DEBUG_SUBMENU_NAME = "Debug Menu";
    const float UI_SCALE_FACTOR = 0.5f; // where 1 is 1 meter, how big do we want the UI?

    // Class variables
    private Transform[] _toDoElementCols;
    private Genie _genie;
    private bool _areAllTasksCompleted {
        get {
            return _genie == null ?
                            false :
                            _completedTaskCount >= _genie.genieBrain.brainInspector.BrainTaskStatuses.Length; }
    }

    private static bool _hasTeleportButtonBeenPressed = false; 
    private int _completedTaskCount = 0;
    private static bool _didShowCelebrationModal = false;
    private Vector3 _expandedMenuBackplateScale;
    // Static since we destroy the MainMenu when we close it, but we want to keep the Celebration object around
    private static Celebration _currCelebration;

    /// <summary>
    /// Gets called by the UI Manager after it spawns the Menu. Note that this Menu was designed to work on its own, without
    /// having been spawned, so if this is a test scene, this won't get called and UIManager will be null.
    /// </summary>
    /// <param name="uiManager"></param>
    public void OnSpawnedByUIManager(UIManager uiManager)
    {
        UIManager = uiManager;

        // Show/hide default state of all icons and master menu scale
        SetupInitialState();

        if (uiManager.Bootstrapper.DebugSkipLaunchUX || _hasTeleportButtonBeenPressed)
        {
            // Normally we'd skip wait for the user to press the Teleport button, but when we use DebugSkipLaunchUX, the Genie
            // gets automatically spawned, so we need to set things up now.
            _hasTeleportButtonBeenPressed = true;
            TransitionFromInitialStateToMainState();
        }
    }

    public void AppearInFrontOfUser(Vector3 userHeadPosition, Vector3 userHandPosition, bool summonedByUserGesture = false)
    {
        Vector3 userHandDirection = userHandPosition - userHeadPosition;
        userHandDirection.y = 0f; // Ignore the y component of the forward vector.
        userHandDirection.Normalize(); // Normalize the vector to get a direction.
        Vector3 offset = userHandDirection * 0.1f; // 0.1m beyond user's hand
        offset += Vector3.up * 0.1f; // Move it up a bit

        _menuHandle.transform.position = userHandPosition + offset;

        if (summonedByUserGesture) 
        {
            _menuHandle.KeepOutOfTheWayOfTheUser(false);
        }

        _menuHandleSmoothLookAt.SnapToLookAt(userHeadPosition);

        Instantiate(_menuOpenedFX, _menuHandle.transform.position, Quaternion.identity);
    }
    
    /// <summary>
    /// Used by the UI Manager when the Prespawn UI is enabled, so that the user can see it and, eventually, the Genie when it it spawns.
    /// </summary>
    /// <returns></returns>
    public void KeepOutOfTheWayOfTheUser(bool enable, Transform userHead)
    {
        _menuHandle.KeepOutOfTheWayOfTheUser(enable, userHead);
    }

    private async Task SetupMainState()
    {
        // Poll until genie is found
        while (true)
        {
            _genie = FindFirstObjectByType<Genie>();
            if (_genie != null)
            {
                break;
            }
            await Task.Yield();
        }

        Debug.Log("Found Genie: " + _genie.name);

        // Get the direct children of the _toDoSubmenu object
        _toDoElementCols = new Transform[_toDoSubmenu.transform.childCount];
        for (int i = 0; i < _toDoElementCols.Length; i++)
        {
            _toDoElementCols[i] = _toDoSubmenu.transform.GetChild(i);
        }

        // Main menu buttons
        _aboutButton.OnPressButton.AddListener(() => _aboutModalDialog.gameObject.SetActive(true));
        _backBtn_aboutModalDialog.OnPressButton.AddListener(() => _aboutModalDialog.gameObject.SetActive(false));
        _celebrateBtn.OnPressButton.AddListener(OnPressCelebrateBtn);
        _celebrateBtn_modalDialog.OnPressButton.AddListener(OnPressCelebrateBtn_modalDialog);
        _backBtn.OnPressButton.AddListener(OnPressBackBtn);
        _backBtn_modalDialog.OnPressButton.AddListener(OnPressBackBtn_modalDialog);
        
        // Destroy on close
        _closeBtn.OnPressButton.AddListener(() => 
        {
            Destroy(gameObject);
            Destroy(_menuHandle.gameObject);
        });

        // Column of Debug Buttons
        DebugToggleAction[] debugToggleActions = new DebugToggleAction[]
        {
            new DebugToggleAction(
                label: "Show Spatial Mesh Grid",
                defaultState: false,
                action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugShowSpatialMesh(enabled));}),
            new DebugToggleAction(
                label: "Show Nav Grid",
                defaultState: false,
                action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugShowNavGrid(enabled));}),
            new DebugToggleAction(
                label: "Show AR Planes",
                defaultState: false,
                action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugVisualizeARPlanes(enabled));}),    
            new DebugToggleAction(
                label: "Show Seat Debuggers",
                defaultState: false,
                action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugShowSeatDebuggers(enabled));}),
            // new DebugToggleAction(
            //     label: "Enable Spatial Mesh Scanning",
            //     defaultState: true,
            //     action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugScanForNewSpatialMeshes(enabled));}),
            new DebugToggleAction(
                label: "Enable Spatial Mesh Occlusion",
                defaultState: true,
                action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugEnableSpatialMeshOcclusion(enabled));}),
            // new DebugToggleAction(
            //     label: "Enable Nav Mesh Updates",
            //     defaultState: true,
            //     action: (enabled) => {GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.DebugEnableNavMeshUpdates(enabled));})
        };

        // Setup the debug buttons
        for (int i = 0; i < Mathf.Min(debugToggleActions.Length); i++)
        {
            var debugBtn = Instantiate(_debugButtonPrefab, _debugSubmenuList.transform);
            var dta = debugToggleActions[i];
            debugBtn.Initialize(dta.DefaultState, dta.Label, dta.Action);

            // Stylize the mesh based on if it needs a rounded top or bottom
            if(i==0)
            {
                debugBtn.SetButtonStyle(DebugButtonStyle.Top);
            }
            else if(i == debugToggleActions.Length - 1)
            {
                debugBtn.SetButtonStyle(DebugButtonStyle.Bottom);
            }
            else
            {
                debugBtn.SetButtonStyle(DebugButtonStyle.None);
            }
        }

        int totalTaskCount = _genie.genieBrain.brainInspector.BrainTaskStatuses.Length;
        int screencapCount = _brainTaskScreencaps.Length;
        if(totalTaskCount != screencapCount)
        {
            Debug.LogError($"BrainTaskStatus count {totalTaskCount} != screencap count {screencapCount}");
        }

        Debug.Log($"Initializing ToDo List. Total task count: {totalTaskCount}");
        // Put half of the tasks in each column
        int btnsPerCol = Mathf.CeilToInt((float)totalTaskCount / _toDoElementCols.Length);
        for (int colIdx = 0; colIdx < _toDoElementCols.Length; colIdx++)
        {
            Transform column = _toDoElementCols[colIdx];
            var columnBtsCount = Mathf.Min(btnsPerCol, totalTaskCount - (colIdx * btnsPerCol));
            for (int j = 0; j < columnBtsCount; j++)
            {
                // Create the prefab
                int taskIndex = colIdx * btnsPerCol + j;
                var taskStatus = _genie.genieBrain.brainInspector.BrainTaskStatuses[taskIndex];
                var toDoElement = Instantiate(_toDoElementPrefab, _toDoSubmenu.transform.GetChild(colIdx));
                Debug.Log($"Creating ToDoElement for task: {taskStatus.taskName} at index {taskIndex} in column {colIdx}, row {j}");

                // Stylize the mesh based on if it's a corner piece
                if (colIdx == 0 && j == 0)
                {
                    toDoElement.SetButtonStyle(ToDoButtonStyle.TopLeft);
                }
                else if (colIdx == _toDoElementCols.Length - 1 && j == 0)
                {
                    toDoElement.SetButtonStyle(ToDoButtonStyle.TopRight);
                }
                else if (colIdx == 0 && j == btnsPerCol - 1)
                {
                    toDoElement.SetButtonStyle(ToDoButtonStyle.BottomLeft);
                }
                else if (colIdx == _toDoElementCols.Length - 1 && j == btnsPerCol - 1)
                {
                    toDoElement.SetButtonStyle(ToDoButtonStyle.BottomRight);
                }
                else
                {
                    toDoElement.SetButtonStyle(ToDoButtonStyle.None);
                }

                // Initialize the button
                Debug.Log($"Initializing ToDoElement for task: {taskStatus.taskName} at index {taskIndex} in column {colIdx}, row {j}");
                var spatialBtn = toDoElement.Initialize(taskStatus);
                spatialBtn.OnPressButton.AddListener(() => 
                {
                    OnDescriptionRequested(taskStatus, _brainTaskScreencaps[taskIndex]);
                });
                taskStatus.onTaskAccomplished += OnTaskAccomplished;
                
                if (taskStatus.IsAccomplished) 
                {
                    OnTaskAccomplished(taskStatus); 
                }
            }
        }
        
        // Reset the debug button to the bottom of the last column
        var debugMenuBtn = Instantiate(_debugMenuBtnPrefab, _toDoSubmenu.transform.GetChild(_toDoElementCols.Length - 1));
        debugMenuBtn.OnPressButton.AddListener(OnPressDebugBtn);
    }

    void OnDestroy()
    {
        // Unsubscribe from all tasks
        foreach (var taskStatus in _genie.genieBrain.brainInspector.BrainTaskStatuses)
        {
            taskStatus.onTaskAccomplished -= OnTaskAccomplished;
        }
    }

    void Update()
    {
        if(Input.GetKeyDown(KeyCode.C))
        {
            while(!_areAllTasksCompleted)
            {
                _completedTaskCount++;
            }
            OnTaskAccomplished(null);
        }
    }

    private void OnDescriptionRequested(BrainInspector.BrainTaskStatus bts, Texture2D screencap)
    {
        _infoModalDialog.Initialize(bts.taskName, screencap, bts.taskDescr);
        _infoModalDialog.gameObject.SetActive(true);
    }

    private void OnTaskAccomplished(BrainInspector.BrainTaskStatus status)
    {
        _completedTaskCount++;

        Debug.Log($"Task accomplished: {status.taskName}");
        Debug.Log($"Completed task count: {_completedTaskCount} out of {_genie.genieBrain.brainInspector.BrainTaskStatuses.Length}");

        if (_areAllTasksCompleted)
        {
            OnAllTasksCompleted();
        }
    }

    private void OnAllTasksCompleted()
    {
        Debug.Log("All tasks completed");

        if (_didShowCelebrationModal)
        {
            return;
        }

        Debug.Log("Showing celebration modal");
        _celebrateBtn.gameObject.SetActive(true);
        _celebrateModalDialog.gameObject.SetActive(true);
        _didShowCelebrationModal = true;

        // Make sure other modals are off.
        _infoModalDialog.gameObject.SetActive(false);
        _aboutModalDialog.gameObject.SetActive(false);
    }   

    private void OnPressDebugBtn()
    {
        ShowDebugMenu();
    }

    private void OnPressBackBtn_modalDialog()
    {
        _celebrateModalDialog.gameObject.SetActive(false);
    }

    private void OnPressBackBtn()
    {
        ShowTodoMenu();
    }

    private void OnPressCelebrateBtn_modalDialog()
    {
        _celebrateModalDialog.gameObject.SetActive(false);
        OnPressCelebrateBtn();
    }

    private async void OnPressCelebrateBtn()
    {
        if (_currCelebration != null)
        {
            Debug.Log("Destroying current celebration");
            Destroy(_currCelebration.gameObject);
            _recordScratchAudioSource.Play();
            await Task.Delay(700);
        }

        _currCelebration = Instantiate(_celebrationPrefab,
                                              _genie.transform.position,
                                              Quaternion.identity).GetComponent<Celebration>();
        _currCelebration.Celebrate();
    }

    private void OnPressTeleportBtn()
    {
        GlobalEventManager.Trigger(new GeniesIRL.GlobalEvents.GenieTeleportHereBtnPressed());

        if (!_hasTeleportButtonBeenPressed)
        {
            TransitionFromInitialStateToMainState();
        }
        
        KeepOutOfTheWayOfTheUser(true, UIManager.Bootstrapper.XRNode.xrOrigin.Camera.transform); // Kind of a hack, but we need to this
        // in case the Prespawn logic in UIManager gets skipped, which can happen if we go from "TryingToSpawn" or TryingToTeleport" 
        // to "NormalPlay" in the same frame.

        _hasTeleportButtonBeenPressed = true;
    }

    private void SetupInitialState()
    {
        // Capture the initial state of the menu backplate
        _expandedMenuBackplateScale = _menuBackplate.localScale;

        // Menu is scaled to 1m, so we will make it human-scale here.
        transform.localScale *= UI_SCALE_FACTOR;
        // Swap parent/child relationship with our modular menu handle
        _menuHandle.Initialize();
        // Set initial ui state (what's on/off) and plate scale
        _menuBackplate.localScale = _initialMenuBackplateScale;
        _canvasMain.sizeDelta = _initialCanvasMainScale;
        _infoModalDialog.gameObject.SetActive(false);
        _celebrateModalDialog.SetActive(false);
        _aboutButton.gameObject.SetActive(false);
        _aboutModalDialog.SetActive(false);
        _menuTitleText.gameObject.SetActive(false);
        _celebrateBtn.gameObject.SetActive(false);
        _teleportBtn.gameObject.SetActive(true);
        _backBtn.gameObject.SetActive(false);
        _quitBtn.gameObject.SetActive(false);
        _toDoSubmenu.SetActive(false);
        _debugSubmenu.SetActive(false);
        _closeBtn.gameObject.SetActive(false);
        // Move these down towards the handle
        _canvasMain.transform.localPosition = new Vector3(0, _initialMenuHandleOffsetY, _canvasDepth);
        _menuBackplate.transform.localPosition = Vector3.up * _initialMenuHandleOffsetY;
        // The only visible button rn
        _teleportBtn.OnPressButton.AddListener(OnPressTeleportBtn);
    }

    private void TransitionFromInitialStateToMainState()
    {
        // Initialize all the things that are shared between the Debug Menu
        // and the Main Menu.
        _menuBackplate.localScale = _expandedMenuBackplateScale;
        _canvasMain.sizeDelta = _expandedCanvasMainScale;
        _canvasMain.transform.localPosition = new Vector3(0, 0, _canvasDepth);
        _menuBackplate.transform.localPosition = Vector3.zero;
        _menuTitleText.gameObject.SetActive(true);
        _teleportBtn.gameObject.SetActive(true);
        _celebrateBtn.gameObject.SetActive(_areAllTasksCompleted || _debugShowCelebrationBtnImmediately);
        _closeBtn.gameObject.SetActive(true);
        
        // Turn various stuff on
        ShowTodoMenu();

        // Hook up tons of stuff
        Debug.Log("Setting up main state");
        _ = SetupMainState();
    }

    private void ShowTodoMenu()
    {
        ToggleMenuMode(isTodoMenu: true);
    }

    private void ShowDebugMenu()
    {
        ToggleMenuMode(isTodoMenu: false);
    }

    private void ToggleMenuMode(bool isTodoMenu)
    {
        Debug.Log($"Toggling isToDo menu mode to {isTodoMenu}");

        _menuTitleText.text = isTodoMenu ? TODO_SUBMENU_NAME : DEBUG_SUBMENU_NAME;
        _toDoSubmenu.SetActive(isTodoMenu);
        _debugSubmenu.SetActive(!isTodoMenu);
        _backBtn.gameObject.SetActive(!isTodoMenu);
        _quitBtn.gameObject.SetActive(!isTodoMenu);
        _aboutButton.gameObject.SetActive(isTodoMenu);
    }
}
