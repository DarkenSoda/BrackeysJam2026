using System;
using TMPro;
using UGSLeaderboardKit;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    [Header("Login UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button enterButton;

    const string HasNameKey = "UGSLeaderboard_HasSetName";
    const string NextSceneName = "Leaderboard";

    async void Start()
    {
        enterButton.interactable = false;
        await UGSBootstrap.InitializeAsync();

        enterButton.onClick.AddListener(SubmitNameAndLogin);
        nameInputField.onValueChanged.AddListener(ChangeSubmitButtonState);

        bool returningPlayer = AuthenticationService.Instance.IsSignedIn &&
                                PlayerPrefs.GetInt(HasNameKey, 0) == 1 &&
                                !string.IsNullOrEmpty(AuthenticationService.Instance.PlayerName);

        if (returningPlayer)
        {
            loginPanel.SetActive(false);
            LoadNextScene();
        }
        else
        {
            loginPanel.SetActive(true);
        }
    }

    void OnDestroy()
    {
        enterButton.onClick.RemoveListener(SubmitNameAndLogin);
        nameInputField.onValueChanged.RemoveListener(ChangeSubmitButtonState);
    }

    void ChangeSubmitButtonState(string arg) => enterButton.interactable = !string.IsNullOrEmpty(arg);

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
            LoadNextScene();
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
    private void LoadNextScene()
    {
        SceneManager.LoadScene(NextSceneName);
    }

    void SetBusy(bool busy)
    {
        enterButton.interactable = !busy && !string.IsNullOrEmpty(nameInputField.text);
        nameInputField.interactable = !busy;
    }
}
