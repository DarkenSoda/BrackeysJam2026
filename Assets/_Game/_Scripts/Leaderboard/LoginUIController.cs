using System;
using TMPro;
using UGSLeaderboardKit;
using Unity.Services.Authentication;
using UnityEngine;
using UnityEngine.UI;

public class LoginUIController : MonoBehaviour
{
    [Header("Login UI")]
    [SerializeField] private GameObject loginPanel;
    [SerializeField] private TMP_InputField nameInputField;
    [SerializeField] private Button enterButton;

    // HasSetName key is shared via LoginKeys in SceneLoader.cs

    async void Start()
    {
        enterButton.interactable = false;
        await UGSBootstrap.InitializeAsync();

        enterButton.onClick.AddListener(SubmitNameAndLogin);
        nameInputField.onValueChanged.AddListener(ChangeSubmitButtonState);

        bool returningPlayer = AuthenticationService.Instance.IsSignedIn &&
                                PlayerPrefs.GetInt(LoginKeys.HasSetName, 0) == 1 &&
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
        if (AudioManager.Instance != null) AudioManager.Instance.PlayMenuClick();

        string playerName = nameInputField.text;
        if (string.IsNullOrEmpty(playerName)) return;

        SetBusy(true);
        try
        {
            await AuthenticationService.Instance.UpdatePlayerNameAsync(playerName);
            PlayerPrefs.SetInt(LoginKeys.HasSetName, 1);
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
        // After signing in, go to MainMenu.
        SceneLoader.LoadMainMenu();
    }

    void SetBusy(bool busy)
    {
        enterButton.interactable = !busy && !string.IsNullOrEmpty(nameInputField.text);
        nameInputField.interactable = !busy;
    }
}
