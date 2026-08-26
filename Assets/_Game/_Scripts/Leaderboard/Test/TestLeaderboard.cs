using UnityEngine;

public class TestLeaderboard : MonoBehaviour
{
   public LeaderboardUIController leaderboardUIController;

    public async void SubmitScore()
    {
        int score = Random.Range(0, 1000);
        await leaderboardUIController.SubmitScoreAsync(score);
    }
}
