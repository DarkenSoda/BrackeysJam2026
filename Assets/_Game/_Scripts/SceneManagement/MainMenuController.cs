using UGSLeaderboardKit;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Main Menu scene controller.
///
/// Attach to: A "MainMenuManager" GameObject in the MainMenu scene.
///
/// Inspector wiring:
///   - playButton        → Play / Start button
///   - leaderboardButton → Leaderboard button
///   - settingsButton    → Settings button (opens the audio panel)
///   - quitButton        → Quit button (can hide this on mobile)
///   - settingsPanel     → The SettingsPanelController GameObject (starts hidden)
///
/// Scene setup:
///   1. Canvas with a background image (night city art).
///   2. Four Buttons: Play, Leaderboard, Settings, Quit.
///   3. A hidden panel child of the Canvas with SettingsPanelController attached.
///   4. Drag everything into this component's Inspector fields.
///
/// Behaviour:
///   Play        → returning player goes straight to gameplay; new player goes to SignIn.
///   Leaderboard → loads the Leaderboard scene (UGS already pre-warmed).
///   Settings    → toggles the Settings audio panel overlay.
///   Quit        → Application.Quit() (no-op in Editor).
/// </summary>
public class MainMenuController : MonoBehaviour
{
    [Header("Menu Container")]
    [Tooltip("The GameObject containing the main menu UI (buttons, logo, etc.) to hide when settings are open.")]
    [SerializeField] private GameObject menuContainer;

    [Header("Navigation Buttons")]
    [SerializeField] private Button playButton;
    [SerializeField] private Button leaderboardButton;
    [SerializeField] private Button settingsButton;
    [SerializeField] private Button quitButton;

    [Header("Settings Panel")]
    [SerializeField] private SettingsPanelController settingsPanel;

    [Header("Audio")]
    [Tooltip("Music to play in the main menu (optional).")]
    [SerializeField] private AudioClip menuMusic;

    private void Awake()
    {
        // Hide the settings panel here — MainMenuController is always active so
        // this Awake is guaranteed to run, unlike SettingsPanelController.Awake()
        // which is skipped when the panel starts inactive in the scene.
        if (settingsPanel != null)
        {
            settingsPanel.gameObject.SetActive(false);
            settingsPanel.OnClosed += OnSettingsClosed;
        }
    }

    private void Start()
    {
        if (playButton        != null) playButton.onClick.AddListener(OnPlayClicked);
        if (leaderboardButton != null) leaderboardButton.onClick.AddListener(OnLeaderboardClicked);
        if (settingsButton    != null) settingsButton.onClick.AddListener(OnSettingsClicked);
        if (quitButton        != null) quitButton.onClick.AddListener(OnQuitClicked);

        // Start menu music & ensure ambient sounds from gameplay are stopped
        if (AudioManager.Instance != null)
        {
            if (menuMusic != null) AudioManager.Instance.PlayMusic(menuMusic);
            AudioManager.Instance.StopAmbient();
        }

        // Silently pre-warm UGS so the Leaderboard/SignIn scenes load instantly.
        _ = UGSBootstrap.InitializeAsync();
    }

    private void OnDestroy()
    {
        if (playButton        != null) playButton.onClick.RemoveListener(OnPlayClicked);
        if (leaderboardButton != null) leaderboardButton.onClick.RemoveListener(OnLeaderboardClicked);
        if (settingsButton    != null) settingsButton.onClick.RemoveListener(OnSettingsClicked);
        if (quitButton        != null) quitButton.onClick.RemoveListener(OnQuitClicked);

        if (settingsPanel != null)
            settingsPanel.OnClosed -= OnSettingsClosed;
    }

    private void OnSettingsClosed()
    {
        if (menuContainer != null)
            menuContainer.SetActive(true);
    }

    // ── Button handlers ───────────────────────────────────────────────────

    private void OnPlayClicked()
    {
        // Skip sign-in for returning players; send new players to enter a display name.
        SceneLoader.LoadGameplay();
    }

    private void OnLeaderboardClicked()
    {
        SceneLoader.LoadLeaderboard();
    }

    private void OnSettingsClicked()
    {
        if (settingsPanel == null)
        {
            Debug.LogWarning("[MainMenuController] settingsPanel is not assigned in the Inspector.");
            return;
        }

        // Toggle: if the panel is already visible, close it; otherwise open it.
        if (settingsPanel.gameObject.activeSelf)
        {
            settingsPanel.Hide();
        }
        else
        {
            if (menuContainer != null) menuContainer.SetActive(false);
            settingsPanel.Show();
        }
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }
}
