using System;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.Core;
using UnityEngine;

namespace UGSLeaderboardKit
{
    /// <summary>
    /// One-time UGS initialization + anonymous sign-in, shared by every game
    /// that imports this package. Safe to call InitializeAsync multiple times;
    /// it no-ops after the first successful call.
    /// </summary>
    public static class UGSBootstrap
    {
        public static bool IsReady { get; private set; }
        public static event Action OnReady;
        public static event Action<Exception> OnFailed;

        static Task _inFlight;

        /// <summary>
        /// Initializes Unity Services and signs the player in anonymously.
        /// Call this once at game start (e.g. from a bootstrapper scene)
        /// before using LeaderboardManager.
        /// </summary>
        public static async Task InitializeAsync()
        {
            if (IsReady) return;
            if (_inFlight != null) { await _inFlight; return; }

            _inFlight = InitializeInternalAsync();
            await _inFlight;
        }

        static async Task InitializeInternalAsync()
        {
            try
            {
                if (UnityServices.State != ServicesInitializationState.Initialized)
                    await UnityServices.InitializeAsync();

                if (!AuthenticationService.Instance.IsSignedIn)
                    await AuthenticationService.Instance.SignInAnonymouslyAsync();

                IsReady = true;
                OnReady?.Invoke();
            }
            catch (Exception e)
            {
                Debug.LogError($"[UGSLeaderboardKit] Initialization failed: {e}");
                OnFailed?.Invoke(e);
                throw;
            }
            finally
            {
                _inFlight = null;
            }
        }

        /// <summary>Current signed-in player's UGS id, or null if not signed in.</summary>
        public static string PlayerId =>
            AuthenticationService.Instance.IsSignedIn ? AuthenticationService.Instance.PlayerId : null;
    }
}
