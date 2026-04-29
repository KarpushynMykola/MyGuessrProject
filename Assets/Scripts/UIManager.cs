using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    #region ПОСИЛАННЯ
    public MapManager map;
    #endregion

    #region ЗМІННІ
    [Header("UI: Панелі")]
    public GameObject mainMenuPanel;
    public GameObject singleplayerPanel;
    public GameObject multiplayerPanel;
    public GameObject inGamePanel;
    public GameObject roundSummaryPanel;
    public GameObject summaryPanel;
    public GameObject lobbyPanel;
    public GameObject profilePanel;

    [Header("UI: елементи")]
    //Ігровий дані
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI pointsText;
    public TextMeshProUGUI roundCountText;
    public TextMeshProUGUI timerText;

    public TextMeshProUGUI summaryScoreText;
    //Підсумок раунду
    public TextMeshProUGUI currentRoundText;

    public TextMeshProUGUI hScoreText;
    public TextMeshProUGUI cScoreText;
    public TextMeshProUGUI hStatusText;
    public TextMeshProUGUI cStatusText;
    public TextMeshProUGUI hTotalScoreText;
    public TextMeshProUGUI cTotalScoreText;
    public TextMeshProUGUI hostText;
    public TextMeshProUGUI clientText;
    //Ігровий інтерфейс
    public TextMeshProUGUI nextButtonText;
    public Button nextButton;
    public Button menuButtonInGame;
    //Синглплеєр
    public TextMeshProUGUI roundsSettingsText;
    public TextMeshProUGUI timerSettingsText;
    public Dropdown singleMapSelect;
    //Мультиплеєр
    public TextMeshProUGUI netRoundsSettingsText;
    public TextMeshProUGUI netTimerSettingsText;
    public Dropdown netMapSelect;

    public TMP_Text joinCodeText;
    public TMP_InputField joinInputField;
    public TMP_Text playerStatusText;
    public Button hostButton;
    public Button joinButton;
    public Button startGameButton;

    //Профіль
    public TMP_Text nameText;
    public TMP_Text levelText;
    public TMP_Text gamesPlayedText;
    public TMP_Text bestScoresText;
    public TMP_InputField changeNameField;
    public Image panelOfLevel;
    public TMP_Text playedLevelText;

    [Header("UI: Налаштування")]
    public int tempMaxRounds = 5;
    public float tempTimeLimit = 60f;
    #endregion

    #region SET
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

    public void OnMapTypeChanged(int index)
    {
        map.selectedPackIndex = index - 1;
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

    public void SetCurrentRoundText(string text)
    {
        if (currentRoundText != null) currentRoundText.text = text;
    }

    public void SetHScoreText(string text)
    {
        if (hScoreText != null) hScoreText.text = text;
    }

    public void SetCScoreText(string text)
    {
        if (cScoreText != null) cScoreText.text = text;
    }

    public void SetHStatusText(string text)
    {
        if (hStatusText != null) hStatusText.text = text;
    }

    public void SetCStatusText(string text)
    {
        if (cStatusText != null) cStatusText.text = text;
    }

    public void SetHTotalScoreText(string text)
    {
        if (hTotalScoreText != null) hTotalScoreText.text = text;
    }

    public void SetCTotalScoreText(string text)
    {
        if (cTotalScoreText != null) cTotalScoreText.text = text;
    }

    public void SetHostText(string text)
    {
        if (hostText != null) hostText.text = text;
    }

    public void SetClientText(string text)
    {
        if (clientText != null) clientText.text = text;
    }
    #endregion

    #region Налаштування UI
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
    #endregion

    #region SHOW
    public void showPanel(GameObject panel, bool isActive)
    {
        if (panel != null)
        {
            panel.SetActive(isActive);
        }
    }

    public void ShowMainMenu()
    {
        showPanel(mainMenuPanel, true);
        showPanel(inGamePanel, false);
        showPanel(singleplayerPanel, false);
        showPanel(multiplayerPanel, false);
        showPanel(roundSummaryPanel, false);
        showPanel(lobbyPanel, false);
        showPanel(summaryPanel, false); 
        showPanel(profilePanel, false);
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

    public void ShowProfile()
    {

        showPanel(profilePanel, true);
        showPanel(mainMenuPanel, false);
        UpdateProfileUI();
    }

    public void ShowLobby()
    {
        multiplayerPanel.SetActive(false);
        lobbyPanel.SetActive(true);
        SetStartGameButtonState(false);
    }

    public void ShowFinalSummary(
        bool IsSpawned,
        string hostName,
        string clientName,
        int hostTotalScoreMP, 
        int clientTotalScoreMP, 
        int totalScore
        )
    {
        summaryPanel.SetActive(true);
        roundSummaryPanel.SetActive(false);

        if (IsSpawned)
        {
            string winnerText = "Final result";
            if (hostTotalScoreMP > clientTotalScoreMP)
                winnerText = $"<color=#FFD700>{hostName} won</color>";
            else if (clientTotalScoreMP > hostTotalScoreMP)
                winnerText = $"<color=#FFD700>{clientName} won</color>";
            else
                winnerText = "<color=#FFFFFF>Draw</color>";

            summaryScoreText.text = $"<b>Final result</b>\n\n" +
                                   $"{hostName}: {hostTotalScoreMP} points\n" +
                                   $"{clientName}: {clientTotalScoreMP} points\n\n" +
                                   $"{winnerText}";
        }
        else
        {
            summaryScoreText.text = $"Final result:\n<color=#FFD700>{totalScore}</color> points";
        }
    }

    public void ShowRoundSummary(string currentRound,
        string hostName,
        string clientName,
        string cScore, 
        string cStatus, 
        string cTotalScore,
        string hScore,
        string hStatus,
        string hTotalScore
        )
    {
        showPanel(roundSummaryPanel, true);

        SetCurrentRoundText(currentRound);

        SetHostText(hostName);
        SetClientText(clientName);

        SetCScoreText(cScore);
        SetCStatusText(cStatus);
        SetCTotalScoreText(cTotalScore);

        SetHScoreText(hScore);
        SetHStatusText(hStatus);
        SetHTotalScoreText(hTotalScore);
    }
    #endregion

    #region Логіка UI
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

    public void UpdateProfileUI()
    {
        var profile = ProfileManager.Instance.activeProfile;
        playedLevelText.color = Color.white;

        nameText.text = profile.playerName;

        levelText.text = $"Level: {profile.level}";
        gamesPlayedText.text = $"Played: {profile.gamesPlayed}";
        bestScoresText.text = $"Record: {profile.bestScore}";

        UpdatePlayedLevel(profile.level);
    }

    public void UpdatePlayedLevel(int level)
    {
        if (level >= 0 && level < 200)
        {
            playedLevelText.text = "Newcomer";
            playedLevelText.color = Color.grey;
            panelOfLevel.color = Color.white;
        }
        else if (level >= 200 && level < 300)
        {
            playedLevelText.text = "Wanderer";
            panelOfLevel.color = Color.blue;
        }
        else if (level >= 300 && level < 400)
        {
            playedLevelText.text = "Conductor";
            panelOfLevel.color = Color.green;
        }
        else if (level >= 400 && level < 500)
        {
            playedLevelText.text = "Cartographer";
            panelOfLevel.color = Color.purple;
        }
        else if (level >= 500 && level < 600)
        {
            playedLevelText.text = "Geo-Analyst";
            panelOfLevel.color = Color.orange;
        }
        else if (level >= 600 && level < 700)
        {
            playedLevelText.text = "Expert";
            panelOfLevel.color = Color.red;
        }
    }

    public void HandleCopuButton()
    {
        if (joinCodeText.text != "Creating") 
            GUIUtility.systemCopyBuffer = joinCodeText.text;
    }

    public void HandleSaveButtonClicked()
    {
        string newName = changeNameField.text;

        if (!string.IsNullOrEmpty(newName))
        {
            ProfileManager.Instance.SetPlayerName(newName);

            Debug.Log("Ім'я збережено!");

            UpdateProfileUI();
        }
    }
    #endregion
}
