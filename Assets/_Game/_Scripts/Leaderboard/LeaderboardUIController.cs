using System;
using System.Threading.Tasks;
using TMPro;
using UGSLeaderboardKit;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUIController : MonoBehaviour
{
    [SerializeField] private string leaderboardId = "TestLeaderboard";

    [Header("Leaderboard UI")]
    [SerializeField] private GameObject leaderboardPanel;
    [SerializeField] private Transform scrollContentContainer;
    [SerializeField] private GameObject entryPrefab;
    [SerializeField] private Button refreshButton;

    [Header("Current Player Banner")]
    [SerializeField] private TMP_Text currentPlayerRankText;
    [SerializeField] private TMP_Text currentPlayerNameText;
    [SerializeField] private TMP_Text currentPlayerScoreText;

    private void Start()
    {

        if (refreshButton != null)
            refreshButton.onClick.AddListener(HandleRefreshButtonClicked);

        Show();
    }

    private void OnDestroy()
    {
        if (refreshButton != null)
            refreshButton.onClick.RemoveListener(HandleRefreshButtonClicked);
    }

    void HandleRefreshButtonClicked() => _ = RefreshAsync();

    public async void Show()
    {
        await UGSBootstrap.InitializeAsync();
        leaderboardPanel.SetActive(true);
        await RefreshAsync();
    }

    public async Task SubmitScoreAsync(double score)
    {
        try
        {
            await UGSBootstrap.InitializeAsync();
            await LeaderboardManager.AddScoreAsync(leaderboardId, score);
            if (leaderboardPanel.activeSelf)
                await RefreshAsync();
        }
        catch (Exception e)
        {
            Debug.LogError("Error submitting score: " + e.Message);
        }
    }

    public async Task RefreshAsync()
    {
        if (refreshButton != null)
            refreshButton.interactable = false;

        try
        {
            var page = await LeaderboardManager.GetAllScoresAsync(leaderboardId);
            RenderEntries(page);
            await RefreshPlayerBanner();
        }
        catch (Exception e)
        {
            Debug.LogError("Error fetching leaderboard: " + e.Message);
        }
        finally
        {
            if (refreshButton != null)
                refreshButton.interactable = true;
        }
    }

    async Task RefreshPlayerBanner()
    {
        var mine = await LeaderboardManager.GetPlayerScoreAsync(leaderboardId);
        if (mine == null)
        {
            currentPlayerRankText.text = "—";
            currentPlayerNameText.text = ExtractDisplayName(AuthenticationService.Instance.PlayerName);
            currentPlayerScoreText.text = "0";
            return;
        }

        currentPlayerRankText.text = (mine.Rank + 1).ToString();
        currentPlayerNameText.text = ExtractDisplayName(mine.PlayerName);
        currentPlayerScoreText.text = mine.Score.ToString();
    }

    void RenderEntries(LeaderboardPage page)
    {
        foreach (Transform child in scrollContentContainer)
            Destroy(child.gameObject);

        string myId = AuthenticationService.Instance.PlayerId;

        foreach (var entry in page.Entries)
        {
            GameObject obj = Instantiate(entryPrefab, scrollContentContainer);
            var ui = obj.GetComponent<LeaderboardEntryUI>();
            if (ui == null)
            {
                Debug.LogError("entryPrefab is missing a LeaderboardEntryUI component.");
                continue;
            }

            bool isCurrentPlayer = entry.PlayerId == myId;
            ui.Setup((entry.Rank + 1).ToString(), entry.PlayerName, entry.Score.ToString(), entry.PlayerId, isCurrentPlayer);
        }
    }

    static string ExtractDisplayName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return "Player";
        int hashIndex = playerName.IndexOf('#');
        return hashIndex >= 0 ? playerName.Substring(0, hashIndex) : playerName;
    }
}