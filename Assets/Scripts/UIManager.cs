using TMPro;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Cysharp.Threading.Tasks.Triggers;

public class UIManager : MonoBehaviour
{
    [Header("UI: Панелі")]
    public GameObject mainMenuPanel;
    public GameObject singleplayerPanel;
    public GameObject multiplayerPanel;
    public GameObject inGamePanel;
    public GameObject roundSummaryPanel;
    public GameObject summaryPanel;
    public GameObject lobbyPanel;

    [Header("UI: елементи")]
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI roundCountText;
    public TextMeshProUGUI timerText;
    public TextMeshProUGUI summaryScoreText;
    public TextMeshProUGUI roundSummaryText;
    public TextMeshProUGUI nextButtonText;
    public Button nextButton;
    public Button menuButtonInGame;
    //Синглплеєр
    public TextMeshProUGUI roundsSettingsText;
    public TextMeshProUGUI timerSettingsText;
    //Мультиплеєр
    public TextMeshProUGUI netRoundsSettingsText;
    public TextMeshProUGUI netTimerSettingsText;

    public TMP_Text joinCodeText;
    public TMP_InputField joinInputField;
    public TMP_Text playerStatusText;
    public Button hostButton;
    public Button joinButton;
    public Button startGameButton;

    [Header("UI: Налаштування")]
    public int tempMaxRounds = 5;
    public float tempTimeLimit = 60f;

    public void showPanel (GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    public void ChangeRounds(int amount)
    {
        tempMaxRounds = Mathf.Clamp(tempMaxRounds + amount, 2, 10);
        UpdateSettingsUI();
    }

    public void ChangeTimer(int amount)
    {
        tempTimeLimit = Mathf.Clamp(tempTimeLimit + amount, 30, 120);
        UpdateSettingsUI();
    }

    public void SetDistanceText(string text)
    {
        if (distanceText != null) distanceText.text = text;
    }

    public void SetPointsText(string text)
    {
        if (pointsText != null) pointsText.text = text;
    }

    public void SetRoundCountText(string text)
    {
        if (roundCountText != null) roundCountText.text = text;
    }

    public void SetNextButtonText(string text)
    {
        if (nextButtonText != null) nextButtonText.text = text;
    }

    public void SetJoinCodeText(string text)
    {
        if (joinCodeText != null) joinCodeText.text = text;
    }

    public void SetPlayerStatusText(string text)
    {
        if (playerStatusText !=  null) playerStatusText.text = text;
    }

    public void SetNextButtonState(bool isInteractable)
    {
        if (nextButton != null) nextButton.interactable = isInteractable;
    }

    public void SetStartGameButtonState(bool isInteractable)
    {
        if (startGameButton != null) startGameButton.interactable = isInteractable;
    }

    public void StartGameUI()
    {
        showPanel(singleplayerPanel, false);
        showPanel(inGamePanel, true);
        SetNextButtonState(false);
        SetNextButtonText("Guessr");
    }

    public void RestartUI()
    {
        SetNextButtonState(false);
        showPanel(summaryPanel, false);
        showPanel(roundSummaryPanel, false);
        SetNextButtonText("Guessr");
    }

    public void StartNextRoundUI()
    {
        SetNextButtonText("Guessr");
        SetNextButtonState(false);
        showPanel(roundSummaryPanel, false);
    }

    public void ShowMainMenu()
    {
        showPanel(mainMenuPanel, true);
        showPanel(inGamePanel, false);
        showPanel(singleplayerPanel, false);
        showPanel(multiplayerPanel, false);
        showPanel(roundSummaryPanel, false);
        showPanel(lobbyPanel, false);
    }

    public void ShowSingleplayerSettings()
    {
        showPanel(mainMenuPanel, false);
        showPanel(singleplayerPanel, true);
        UpdateSettingsUI();
    }

    public void ShowMultiplayerMenu()
    {
        mainMenuPanel.SetActive(false);
        multiplayerPanel.SetActive(true);
        UpdateSettingsUI();
    }

    public void ShowLobby()
    {
        multiplayerPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        SetStartGameButtonState(false);
    }

    public void ShowFinalSummary(bool IsSpawned, int hostTotalScoreMP, int clientTotalScoreMP, int totalScore)
    {
        summaryPanel.SetActive(true);

        if (IsSpawned)
        {
            string winnerText = "Final result";
            if (hostTotalScoreMP > clientTotalScoreMP)
                winnerText = "<color=#FFD700>Host won</color>";
            else if (clientTotalScoreMP > hostTotalScoreMP)
                winnerText = "<color=#FFD700>Client won</color>";
            else
                winnerText = "<color=#FFFFFF>Draw</color>";

            summaryScoreText.text = $"<b>Final result</b>\n\n" +
                                   $"Host: {hostTotalScoreMP} points\n" +
                                   $"Client: {clientTotalScoreMP} points\n\n" +
                                   $"{winnerText}";
        }
        else
        {
            summaryScoreText.text = $"Final result:\n<color=#FFD700>{totalScore}</color> points";
        }
    }

    public void ShowRoundSummary(string summaryText)
    {
        showPanel(roundSummaryPanel, true);
        if (roundSummaryText != null) roundSummaryText.text = summaryText;
    }

    public void UpdateSettingsUI()
    {
        if (roundsSettingsText != null && netRoundsSettingsText != null)
        {
            roundsSettingsText.text = tempMaxRounds.ToString();
            netRoundsSettingsText.text = tempMaxRounds.ToString();
        }
        if (timerSettingsText != null && netTimerSettingsText != null)
        {
            netTimerSettingsText.text = tempTimeLimit.ToString() + "s";
            timerSettingsText.text = tempTimeLimit.ToString() + "s";
        }
    }
    public void UpdateTimerUI(float timeValue)
    {
        if (timerText != null)
        {
            int seconds = Mathf.CeilToInt(timeValue);
            timerText.text = $"{seconds}с";
            timerText.color = seconds <= 10 ? Color.red : Color.white;
        }
    }
}
