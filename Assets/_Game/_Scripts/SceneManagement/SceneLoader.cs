using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Single source of truth for all scene names in Night Shift Taxi.
/// Use the static constants when loading scenes so a typo never causes a
/// silent runtime crash.
///
/// Setup:
///   Add every scene listed here to File > Build Settings > Scenes In Build.
///   Recommended order: SignIn (0), MainMenu (1), Driving (2), Leaderboard (3).
/// </summary>
public static class SceneLoader
{
    // ── Scene name constants ──────────────────────────────────────────────
    public const string MainMenu = "MainMenu";
    public const string SignIn = "SignIn";
    public const string Gameplay = "Driving";     // ← main gameplay scene
    public const string Leaderboard = "Leaderboard";

    // ── Convenience loaders ───────────────────────────────────────────────

    public static void LoadMainMenu() => Load(MainMenu);
    public static void LoadSignIn() => Load(SignIn);
    public static void LoadGameplay() => Load(Gameplay);
    public static void LoadLeaderboard() => Load(Leaderboard);


    // ── Generic load with optional delay ─────────────────────────────────

    public static void Load(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>Loads a scene after a short delay (useful for end-of-game pauses).</summary>
    public static IEnumerator LoadAfterDelay(string sceneName, float delaySecs)
    {
        yield return new WaitForSeconds(delaySecs);
        SceneManager.LoadScene(sceneName);
    }
}

/// <summary>Shared PlayerPrefs key constants so every script uses the same strings.</summary>
public static class LoginKeys
{
    public const string HasSetName = "UGSLeaderboard_HasSetName";
}
