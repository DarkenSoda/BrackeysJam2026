using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardEntryUI : MonoBehaviour
{
    [SerializeField] private TMP_Text rankText;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text scoreText;

    [Header("Current Player Highlight")]
    [SerializeField] private Image background;
    [SerializeField] private Color normalColor = Color.white;
    [SerializeField] private Color currentPlayerColor = new Color(1f, 0.92f, 0.6f);

    public string PlayerId { get; private set; }
    public void Setup(string rank, string playerName, string score, string playerId, bool isCurrentPlayer)
    {
        rankText.text = rank;
        nameText.text = ExtractDisplayName(playerName);
        scoreText.text = score;
        PlayerId = playerId;

        if (background != null)
            background.color = isCurrentPlayer ? currentPlayerColor : normalColor;
    }

    static string ExtractDisplayName(string playerName)
    {
        if (string.IsNullOrEmpty(playerName)) return "Player";
        int hashIndex = playerName.IndexOf('#');
        return hashIndex >= 0 ? playerName.Substring(0, hashIndex) : playerName;
    }
}
