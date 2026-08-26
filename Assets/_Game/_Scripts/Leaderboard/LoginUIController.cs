using System;
using TMPro;
using UGSLeaderboardKit;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

/// <summary>
/// Owns sign-in only: shows the name-entry panel (or skips it for returning
/// players), signs the player in, and fires OnLoggedIn. Doesn't know the
/// leaderboard UI exists — wire OnLoggedIn to LeaderboardController.Show()
/// in the Inspector to connect the two.
/// </summary>
public class LoginUIController : MonoBehaviour
{
    [Header("Login UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button submitButton;

    [Header("Events")]
    public UnityEvent OnLoggedIn;

    const string HasNameKey = "UGSLeaderboard_HasSetName";

    async void Start()
    {
        submitButton.interactable = false;
        await UGSBootstrap.InitializeAsync();

        submitButton.onClick.AddListener(SubmitNameAndLogin);
        nameInputField.onValueChanged.AddListener(ChangeSubmitButtonState);

        bool returningPlayer = AuthenticationService.Instance.IsSignedIn &&
                                PlayerPrefs.GetInt(HasNameKey, 0) == 1 &&
                                !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName);

        if (returningPlayer)
        {
            loginPanel.SetActive(false);
            OnLoggedIn?.Invoke();
        }
        else
        {
            loginPanel.SetActive(true);
        }
    }

    void OnDestroy()
    {
        submitButton.onClick.RemoveListener(SubmitNameAndLogin);
        nameInputField.onValueChanged.RemoveListener(ChangeSubmitButtonState);
    }

    void ChangeSubmitButtonState(string arg) => submitButton.interactable = !string.IsNullOrEmpty(arg);

    private async void SubmitNameAndLogin()
    {
        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName)) return;

        SetBusy(true);
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            PlayerPrefs.SetInt(HasNameKey, 1);
            PlayerPrefs.Save();

            loginPanel.SetActive(false);
            OnLoggedIn?.Invoke();
        }
        catch (Exception e)
        {
            Debug.LogError("Error logging in: " + e.Message);
        }
        finally
        {
            SetBusy(false);
        }
    }

    void SetBusy(bool busy)
    {
        submitButton.interactable = !busy && !string.IsNullOrEmpty(nameInputField.text);
        nameInputField.interactable = !busy;
    }
}
