using System.Collections.Generic;

namespace UGSLeaderboardKit
{
    /// <summary>
    /// Engine/game-agnostic representation of a single leaderboard row.
    /// Kept separate from Unity.Services.Leaderboards.Models so that if the
    /// UGS SDK's internal types change, only LeaderboardManager needs updating —
    /// not every game that consumes this package.
    /// </summary>
    [System.Serializable]
    public class LeaderboardEntry
    {
        public string PlayerId;
        public string PlayerName;
        public double Score;
        public int Rank;
        public string Metadata;

        public override string ToString() =>
            $"#{Rank} {PlayerName ?? PlayerId} — {Score}";
    }

    /// <summary>
    /// A page of results plus paging info, mirroring what UGS returns
    /// but decoupled from its SDK types.
    /// </summary>
    [System.Serializable]
    public class LeaderboardPage
    {
        public List<LeaderboardEntry> Entries = new List<LeaderboardEntry>();
        public int Offset;
        public int TotalCount;
    }
}
