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
    public float currentPanoLat = 50.4501f;
    public float currentPanoLng = 30.5234f;
    public float lastClickLat;
    public float lastClickLng;
    #endregion

    #region ЖИТТЄВИЙ ЦИКЛ
    void OnEnable() { Application.logMessageReceivedThreaded += map.HandleUnityLog; }
    void OnDisable() { Application.logMessageReceivedThreaded -= map.HandleUnityLog; }

    void Start()
    {
        currentRound = 1;

        if (map.browserObject == null) return;

        isGameStarted = false;

        ui.SetNextButtonState(false);
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
            PropertyInfo prop = map.browserClient.GetType().GetProperty("IsConnected");
            if (prop != null && (bool)prop.GetValue(map.browserClient))
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
                ui.UpdateTimerUI(multiplayer.netRemainingTime.Value);
            }
            else
            {
                timer.currentTime -= Time.deltaTime;

                if (timer.currentTime <= 0)
                {
                    timer.currentTime = 0;
                    timer.OnTimeUp(IsSpawned, IsServer);
                }
                ui.UpdateTimerUI(timer.currentTime);
            }
        }
    }
    #endregion

    #region ЦИКЛ ГРИ (Start, Restart, NextRound, Qiut)
    public void StartGame()
    {
        if (IsSpawned && IsServer)
        {
            multiplayer.netMaxRounds.Value = ui.tempMaxRounds;
            multiplayer.netTimeLimit.Value = ui.tempTimeLimit;

            multiplayer.StartGameClientRpc(ui.tempMaxRounds, ui.tempTimeLimit);

            map.TeleportToRandomLocation();
        }

        else if (!IsSpawned)
        {
            maxRounds = ui.tempMaxRounds;
            timer.timeLimit = ui.tempTimeLimit;
            ExecuteStartGameLogic();
            map.TeleportToRandomLocation();
        }
    }

    public void ExecuteStartGameLogic()
    {
        currentRound = 1;
        totalScore = 0;
        isGameStarted = true;
        isResultPhase = false;

        ui.SetDistanceText("?");
        ui.SetPointsText("?");
        ui.SetRoundCountText($"{currentRound}/{maxRounds}");

        ui.StartGameUI();

        timer.StartTimer(IsSpawned, IsServer);
    }

    public void StartNextRound()
    {
        currentRound++;
        isResultPhase = false;
        hasClickedMap = false;
        map.UpdateBrowserMapState(false, false);

        ui.SetDistanceText("?");
        ui.SetPointsText("?");
        ui.SetRoundCountText($"{currentRound}/{maxRounds}");

        ui.StartNextRoundUI();

        if (!IsSpawned || IsServer)
        {
            map.TeleportToRandomLocation();
        }

        timer.StartTimer(IsSpawned, IsServer);
    }

    public void RestartGame()
    {
        totalScore = 0;
        currentRound = 1;
        hostTotalScoreMP = 0;
        clientTotalScoreMP = 0;

        isResultPhase = false;
        hasClickedMap = false;
        timer.isTimerRunning = true;

        map.UpdateBrowserMapState(false, false);

        ui.SetDistanceText("?");
        ui.SetPointsText("?");
        ui.SetRoundCountText($"{currentRound}/{maxRounds}");

        ui.RestartUI();

        if (IsServer || !IsSpawned)
        {
            map.TeleportToRandomLocation();
            timer.StartTimer(IsSpawned, IsServer);
        }
    }

    public void OnRestartButtonClick()
    {
        if (IsSpawned && IsServer)
        {
            multiplayer.RestartGameClientRpc();
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

        isGameStarted = false;
        timer.isTimerRunning = false;
        isResultPhase = false;

        ui.ShowMainMenu();

        map.UpdateBrowserMapState(false, false);
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
        needToUpdateUI = true;

        if (currentRound >= maxRounds)
            ui.SetNextButtonText(IsSpawned && !IsServer ? "Waiting result" : "Result");
        else
            ui.SetNextButtonText(IsSpawned && !IsServer ? "Wait Host" : "Continue");
    }
    #endregion
}