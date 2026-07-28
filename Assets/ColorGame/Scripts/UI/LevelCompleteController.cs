using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Reacts to PaintCoverageTracker.Completed by freezing gameplay input and showing the level-complete
// panel. PaintCoverageTracker stays unaware this script exists — it only ever raises Completed and
// never reaches into UI or scene-loading. Replay and Next Level intentionally perform the identical
// action for this prototype (both just reload the current scene).
public class LevelCompleteController : MonoBehaviour
{
    [Header("Tracker")]
    [SerializeField] private PaintCoverageTracker tracker;

    [Header("Panel")]
    [SerializeField] private GameObject levelCompletePanelRoot;
    [SerializeField] private Button replayButton;
    [SerializeField] private Button nextLevelButton;

    [Header("Gameplay Locks (optional)")]
    [SerializeField] private PlayerMovementController playerMovementController;
    [SerializeField] private MobileInputReader mobileInputReader;
    [SerializeField] private ThirdPersonOrbitCamera orbitCamera;
    [SerializeField] private PaintFillController fillController;
    [SerializeField] private PaintGunFireController fireController;
    [SerializeField] private ContextualActionUI contextualActionUI;

    private bool reloadRequested;

    public bool IsLevelComplete { get; private set; }

    private void Awake()
    {
        if (tracker == null)
            Debug.LogWarning($"{nameof(LevelCompleteController)} on '{name}' has no {nameof(tracker)} assigned.", this);

        if (levelCompletePanelRoot == null)
            Debug.LogWarning($"{nameof(LevelCompleteController)} on '{name}' has no {nameof(levelCompletePanelRoot)} assigned.", this);

        // Hidden until Completed fires, regardless of whatever active state the saved scene had.
        if (levelCompletePanelRoot != null)
            levelCompletePanelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (tracker != null)
            tracker.Completed += HandleTargetCompleted;

        if (replayButton != null)
            replayButton.onClick.AddListener(ReloadCurrentScene);

        if (nextLevelButton != null)
            nextLevelButton.onClick.AddListener(ReloadCurrentScene);
    }

    private void OnDisable()
    {
        if (tracker != null)
            tracker.Completed -= HandleTargetCompleted;

        if (replayButton != null)
            replayButton.onClick.RemoveListener(ReloadCurrentScene);

        if (nextLevelButton != null)
            nextLevelButton.onClick.RemoveListener(ReloadCurrentScene);
    }

    private void HandleTargetCompleted()
    {
        // Completed already latches internally on the tracker, but this guards independently against
        // any duplicate invocation reaching this listener.
        if (IsLevelComplete)
            return;

        IsLevelComplete = true;

        SetGameplayEnabled(false);

        if (levelCompletePanelRoot != null)
        {
            // Re-assert top-of-Canvas ordering at show time rather than trusting hierarchy order alone.
            levelCompletePanelRoot.transform.SetAsLastSibling();
            levelCompletePanelRoot.SetActive(true);
        }
    }

    private void SetGameplayEnabled(bool enabled)
    {
        playerMovementController?.SetMovementEnabled(enabled);
        mobileInputReader?.SetMovementInputEnabled(enabled);
        orbitCamera?.SetCameraInputEnabled(enabled);

        // Fill/Fire/ContextualActionUI already stop their own active effects safely from OnDisable
        // (StopFilling/StopFiring/ForceRelease) — disabling them is sufficient, no duplicate logic here.
        if (fillController != null)
            fillController.enabled = enabled;

        if (fireController != null)
            fireController.enabled = enabled;

        if (contextualActionUI != null)
            contextualActionUI.enabled = enabled;
    }

    public void ReloadCurrentScene()
    {
        if (reloadRequested)
            return;

        reloadRequested = true;

        if (replayButton != null)
            replayButton.interactable = false;

        if (nextLevelButton != null)
            nextLevelButton.interactable = false;

        Scene activeScene = SceneManager.GetActiveScene();

        if (activeScene.buildIndex < 0)
        {
            Debug.LogError(
                $"{nameof(LevelCompleteController)}: active scene '{activeScene.name}' has no valid build index — it is missing from Build Settings. Reloading by name instead.",
                this);
            SceneManager.LoadScene(activeScene.name);
            return;
        }

        SceneManager.LoadScene(activeScene.buildIndex);
    }
}
