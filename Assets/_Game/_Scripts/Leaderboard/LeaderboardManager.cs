using System;
using System.Threading.Tasks;
using Unity.Services.Leaderboards;
using UgsEntry = Unity.Services.Leaderboards.Models.LeaderboardEntry;
using UgsScoresPage = Unity.Services.Leaderboards.Models.LeaderboardScoresPage;
using UgsScores = Unity.Services.Leaderboards.Models.LeaderboardScores;
using Unity.Services.Leaderboards.Exceptions;

namespace UGSLeaderboardKit
{
    /// <summary>
    /// Thin, game-agnostic wrapper around the Unity Gaming Services Leaderboards API.
    ///
    /// Usage:
    ///   await UGSBootstrap.InitializeAsync();
    ///   await LeaderboardManager.AddScoreAsync(leaderboardId, score);
    ///   var all = await LeaderboardManager.GetAllScoresAsync(leaderboardId);
    ///
    /// The leaderboardId string must match a leaderboard
    /// you configured for the project in the Unity Cloud Dashboard — this
    /// package does not create leaderboards, only reads/writes to them.
    /// </summary>
    public static class LeaderboardManager
    {
        public const int DefaultMaxEntries = 1000;

        static void EnsureReady()
        {
            if (!UGSBootstrap.IsReady)
                throw new InvalidOperationException(
                    "[UGSLeaderboardKit] UGS is not initialized yet. Await UGSBootstrap.InitializeAsync() first.");
        }

        public static async Task AddScoreAsync(string leaderboardId, double score, string metadata = null)
        {
            EnsureReady();
            var options = metadata != null ? new AddPlayerScoreOptions { Metadata = metadata } : null;
            await LeaderboardsService.Instance.AddPlayerScoreAsync(leaderboardId, score, options);
        }

        public static async Task<LeaderboardPage> GetTopScoresAsync(string leaderboardId, int limit = 10, int offset = 0)
        {
            EnsureReady();
            var options = new GetScoresOptions { Limit = limit, Offset = offset };
            var response = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);
            return ToPage(response);
        }

        /// <summary>
        /// Fetches every entry on the leaderboard by paging internally in batches,
        /// so callers (e.g. a scroll view) get one flat list back instead of
        /// having to implement Next/Previous themselves.
        /// </summary>
        /// <param name="batchSize">How many entries to request per underlying call. UGS caps this server-side (100 max).</param>
        /// <param name="maxEntries">Hard stop so a very large leaderboard can't fetch forever.</param>
        public static async Task<LeaderboardPage> GetAllScoresAsync(string leaderboardId, int batchSize = 100, int maxEntries = DefaultMaxEntries)
        {
            EnsureReady();
            var all = new LeaderboardPage { Offset = 0 };
            int offset = 0;

            while (true)
            {
                var options = new GetScoresOptions { Limit = batchSize, Offset = offset };
                var response = await LeaderboardsService.Instance.GetScoresAsync(leaderboardId, options);

                all.TotalCount = response.Total;
                foreach (var r in response.Results)
                    all.Entries.Add(ToEntry(r));

                offset += response.Results.Count;

                bool gotFullBatch = response.Results.Count == batchSize;
                bool reachedServerTotal = offset >= response.Total;
                bool hitSafetyCap = offset >= maxEntries;

                if (!gotFullBatch || reachedServerTotal || hitSafetyCap)
                    break;
            }

            return all;
        }

        public static async Task<LeaderboardPage> GetScoresAroundPlayerAsync(string leaderboardId, int range = 5)
        {
            EnsureReady();
            var options = new GetPlayerRangeOptions { RangeLimit = range };
            var response = await LeaderboardsService.Instance.GetPlayerRangeAsync(leaderboardId, options);
            return ToPage(response);
        }

        public static async Task<LeaderboardEntry> GetPlayerScoreAsync(string leaderboardId)
        {
            EnsureReady();
            try
            {
                var entry = await LeaderboardsService.Instance.GetPlayerScoreAsync(leaderboardId);
                return ToEntry(entry);
            }
            catch (LeaderboardsException e) when (e.Reason == LeaderboardsExceptionReason.EntryNotFound)
            {
                return null;
            }
        }

        static LeaderboardPage ToPage(UgsScoresPage response)
        {
            var page = new LeaderboardPage
            {
                Offset = response.Offset,
                TotalCount = response.Total
            };
            foreach (var r in response.Results)
                page.Entries.Add(ToEntry(r));
            return page;
        }

        static LeaderboardPage ToPage(UgsScores response)
        {
            var page = new LeaderboardPage { TotalCount = -1 };
            if (response.Results != null)
                foreach (var r in response.Results)
                    page.Entries.Add(ToEntry(r));
            page.Offset = page.Entries.Count > 0 ? page.Entries[0].Rank : 0;
            return page;
        }

        static LeaderboardEntry ToEntry(UgsEntry r)
        {
            return new LeaderboardEntry
            {
                PlayerId = r.PlayerId,
                PlayerName = r.PlayerName,
                Score = r.Score,
                Rank = r.Rank,
                Metadata = r.Metadata
            };
        }
    }
}
