using System.Reflection;
using Unity.Netcode;
using UnityEngine;


public class GameManager : NetworkBehaviour
{
    #region ПОСИЛАННЯ
    public UIManager ui;
    public CalculatorManager calculator;
    public MultiplayerManager multiplayer;
    public TimerManager timer;
    public MapManager map;
    public SoundManager sound;
    #endregion

    #region ЗМІННІ: Логіка гри
    [Header("Система раундів")]
    public int currentRound = 1;
    public int maxRounds = 5;

    [Header("Система балів")]
    public int totalScore = 0;
    public int hostTotalScoreMP = 0;
    public int clientTotalScoreMP = 0;

    //Стан ігри
    public bool isGameStarted = false;
    public bool hasClickedMap = false;
    public bool isResultPhase = false;
    public bool needToUpdateUI = false;
    public bool needToRetryRandom = false;

    //Координати
    public float currentPanoLat;
    public float currentPanoLng;
    public float lastClickLat;
    public float lastClickLng;
    #endregion

    #region ЖИТТЄВИЙ ЦИКЛ
    void OnEnable() { Application.logMessageReceivedThreaded += map.HandleUnityLog; }
    void OnDisable() { Application.logMessageReceivedThreaded -= map.HandleUnityLog; }

    void Start()
    {
        if (map.browserObject == null) return;
        ResetGameData();
        ui.ShowMainMenu();
        map.FindBrowserClient();
    }

    void Update()
    {
        if (hasClickedMap) ui.SetNextButtonState(true);

        if (needToRetryRandom)
        {
            needToRetryRandom = false;
            map.TeleportToRandomLocation();
        }

        if (map.browserClient != null && !map.isLoaded)
        {
            if (map.IsBrowserConnected())
            {
                map.LoadPanorama();
                map.isLoaded = true;
                multiplayer.networkSyncPending = true;
            }
        }

        if (map.isLoaded && multiplayer.networkSyncPending)
        {
            Debug.Log($"[Sync] Синхронізація клієнта: {multiplayer.netPanoLat.Value}, {multiplayer.netPanoLng.Value}");
            map.MoveTo(currentPanoLat, currentPanoLng, false);
            multiplayer.networkSyncPending = false;
        }

        if (!isGameStarted) return;

        if (needToUpdateUI)
        { 
            needToUpdateUI = false;
        }

        if (timer.isTimerRunning && !isResultPhase)
        {
            float currentRemainingTime;

            if (IsSpawned)
            {
                if (IsServer)
                {
                    multiplayer.netRemainingTime.Value -= Time.deltaTime;

                    if (multiplayer.netRemainingTime.Value <= 0)
                    {
                        multiplayer.netRemainingTime.Value = 0;
                        timer.OnTimeUp(IsSpawned, IsServer);
                    }
                }
                currentRemainingTime = multiplayer.netRemainingTime.Value;
            }
            else
            {
                timer.currentTime -= Time.deltaTime;

                if (timer.currentTime <= 0)
                {
                    timer.currentTime = 0;
                    timer.OnTimeUp(IsSpawned, IsServer);
                }
                currentRemainingTime = timer.currentTime;
            }

            ui.UpdateTimerUI(currentRemainingTime);

            if (currentRemainingTime <= 10.5f && currentRemainingTime > 0.1f)
            {
                sound.StartTicking();
            }
            else
            {
                sound.StopTicking();
            }
        }
    }

    private void ResetGameData()
    {
        isGameStarted = false;
        isResultPhase = false;
        timer.isTimerRunning = false;
        hasClickedMap = false;
        needToUpdateUI = false;
        needToRetryRandom = false;

        currentRound = 1;
        hostTotalScoreMP = 0;
        clientTotalScoreMP = 0;
        totalScore = 0;
    }
    #endregion

    #region ЦИКЛ ГРИ (Start, Restart, NextRound, Qiut)
    public void StartGame()
    {
        if (IsSpawned && IsServer)
        {
            StartMultyPlayerSession();
        }
        else if (!IsSpawned)
        {
            StartSinglePlayerSession();
        }
    }

    private void StartMultyPlayerSession()
    {
        multiplayer.netMaxRounds.Value = ui.tempMaxRounds;
        multiplayer.netTimeLimit.Value = ui.tempTimeLimit;
        multiplayer.StartGameClientRpc(ui.tempMaxRounds, ui.tempTimeLimit);
        map.TeleportToRandomLocation();
    }

    private void StartSinglePlayerSession()
    {
        maxRounds = ui.tempMaxRounds;
        timer.timeLimit = ui.tempTimeLimit;
        ExecuteStartGameLogic();
        map.TeleportToRandomLocation();
    }

    public void ExecuteStartGameLogic()
    {
        ResetGameData();
        isGameStarted = true;

        ui.StartGameUI(currentRound, maxRounds);

        timer.StartTimer(IsSpawned, IsServer);
    }

    public void StartNextRound()
    {
        currentRound++;
        isResultPhase = false;
        hasClickedMap = false;

        map.UpdateBrowserMapState(false, false);

        ui.StartNextRoundUI(currentRound, maxRounds);

        if (!IsSpawned || IsServer)
        {
            map.TeleportToRandomLocation();
        }

        timer.StartTimer(IsSpawned, IsServer);
    }

    public void RestartGame()
    {
        ResetGameData();
        isGameStarted = true;
        timer.isTimerRunning = true;

        map.UpdateBrowserMapState(false, false);

        ui.RestartUI(currentRound, maxRounds);

        if (IsServer || !IsSpawned)
        {
            map.TeleportToRandomLocation();
            timer.StartTimer(IsSpawned, IsServer);
        }
    }

    public void OnRestartButtonClick()
    {
        if (IsSpawned)
        {
            multiplayer.HandleRestartButtonClick();
        }
        else if (!IsSpawned)
        {
            RestartGame();
        }
    }

    public void BackToMainMenu()
    {
        if ((NetworkManager.Singleton != null && NetworkManager.Singleton.IsListening) || IsSpawned)
        {
            NetworkManager.Singleton.Shutdown();
        }

        ResetGameData();
        ui.ShowMainMenu();
        sound.StopTicking();
        map.UpdateBrowserMapState(false, false);

        Debug.Log($"Чи почалася гра: {isGameStarted}");
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion

    #region РЕЗУЛЬТАТИ
    public void ProcessResult(float targetLat, float targetLng, float guessLat, float guessLng, float? opponentLat = null, float? opponentLng = null)
    {
        map.UpdateBrowserMapState(true, true);

        float distance = calculator.CalculateDistance(targetLat, targetLng, guessLat, guessLng);
        int roundScore = calculator.CalculateScore(distance);
        totalScore += roundScore;

        if (opponentLat.HasValue && opponentLng.HasValue)
        {
            map.ShowMultiplayerResultInBrowser(targetLat, targetLng, guessLat, guessLng, opponentLat.Value, opponentLng.Value);
        }
        else
        {
            map.ShowLineInBrowser(guessLat, guessLng);
        }

        ui.SetDistanceText($"{(int)distance}km");
        ui.SetPointsText($"{roundScore} XP");
        ui.SetNextButtonState(true);
        sound.StopTicking();
        needToUpdateUI = true;

        if (currentRound >= maxRounds)
            ui.SetNextButtonText(IsSpawned && !IsServer ? "Waiting result" : "Result");
        else
            ui.SetNextButtonText(IsSpawned && !IsServer ? "Wait Host" : "Continue");
    }
    #endregion
}