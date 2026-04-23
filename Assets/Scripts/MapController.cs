using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;


public class MapController : NetworkBehaviour
{
    #region ЗМІННІ: Налаштування браузера
    [Header("Налаштування браузера")]
    public GameObject browserObject;
    private object browserClient;
    public bool isUpdatingFromBrowser = false;
    public bool isLoaded = false;
    #endregion

    #region Екземпляри класів
    public UIManager ui;
    public CalculatorManager calculator;
    public MultiplayerManager multiplayer;
    #endregion

    #region ЗМІННІ: Логіка гри
    [Header("Система раундів")]
    public int currentRound = 1;
    public int maxRounds = 5;

    [Header("Система балів")]
    public int totalScore = 0;
    public int hostTotalScoreMP = 0;
    public int clientTotalScoreMP = 0;

    [Header("Налаштування Таймера")]
    public float timeLimit = 60f;
    public float currentTime;
    public bool isTimerRunning = false;

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
    void OnEnable() { Application.logMessageReceivedThreaded += HandleUnityLog; }
    void OnDisable() { Application.logMessageReceivedThreaded -= HandleUnityLog; }

    void Start()
    {
        currentRound = 1;

        if (browserObject == null) return;

        isGameStarted = false;

        ui.SetNextButtonState(false);
        ui.ShowMainMenu();

        FindBrowserClient();
    }

    void Update()
    {
        if (hasClickedMap) ui.SetNextButtonState(true);

        if (needToRetryRandom)
        {
            needToRetryRandom = false;
            TeleportToRandomLocation();
        }

        if (browserClient != null && !isLoaded)
        {
            PropertyInfo prop = browserClient.GetType().GetProperty("IsConnected");
            if (prop != null && (bool)prop.GetValue(browserClient))
            {
                LoadPanorama();
                isLoaded = true;
                multiplayer.networkSyncPending = true;
            }
        }

        if (isLoaded && multiplayer.networkSyncPending)
        {
            Debug.Log($"[Sync] Синхронізація клієнта: {multiplayer.netPanoLat.Value}, {multiplayer.netPanoLng.Value}");
            MoveTo(currentPanoLat, currentPanoLng, false);
            multiplayer.networkSyncPending = false;
        }

        if (!isGameStarted) return;

        if (needToUpdateUI)
        { 
            needToUpdateUI = false;
        }

        if (isTimerRunning && !isResultPhase)
        {
            if (IsSpawned)
            {
                if (IsServer)
                {
                    multiplayer.netRemainingTime.Value -= Time.deltaTime;

                    if (multiplayer.netRemainingTime.Value <= 0)
                    {
                        multiplayer.netRemainingTime.Value = 0;
                        OnTimeUp();
                    }
                }
                ui.UpdateTimerUI(multiplayer.netRemainingTime.Value);
            }
            else
            {
                currentTime -= Time.deltaTime;

                if (currentTime <= 0)
                {
                    currentTime = 0;
                    OnTimeUp();
                }
                ui.UpdateTimerUI(currentTime);
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

            TeleportToRandomLocation();
        }

        else if (!IsSpawned)
        {
            maxRounds = ui.tempMaxRounds;
            timeLimit = ui.tempTimeLimit;
            ExecuteStartGameLogic();
            TeleportToRandomLocation();
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

        StartTimer();
    }

    public void StartNextRound()
    {
        currentRound++;
        isResultPhase = false;
        hasClickedMap = false;
        UpdateBrowserMapState(false, false);

        ui.SetDistanceText("?");
        ui.SetPointsText("?");
        ui.SetRoundCountText($"{currentRound}/{maxRounds}");

        ui.StartNextRoundUI();

        if (!IsSpawned || IsServer)
        {
            TeleportToRandomLocation();
        }

        StartTimer();
    }

    public void RestartGame()
    {
        totalScore = 0;
        currentRound = 1;
        hostTotalScoreMP = 0;
        clientTotalScoreMP = 0;

        isResultPhase = false;
        hasClickedMap = false;
        isTimerRunning = true;

        UpdateBrowserMapState(false, false);

        ui.SetDistanceText("?");
        ui.SetPointsText("?");
        ui.SetRoundCountText($"{currentRound}/{maxRounds}");

        ui.RestartUI();

        if (IsServer || !IsSpawned)
        {
            TeleportToRandomLocation();
            StartTimer();
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
        isTimerRunning = false;
        isResultPhase = false;

        ui.ShowMainMenu();

        UpdateBrowserMapState(false, false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
    #endregion

    #region ТАЙМЕР
    void StartTimer()
    {
        isTimerRunning = true;

        if (IsSpawned)
        {
            if (IsServer)
            {
                multiplayer.netRemainingTime.Value = timeLimit;
            }
        }
        else
        {
            currentTime = timeLimit;
        }
    }

    void OnTimeUp()
    {
        isTimerRunning = false;

        if (IsSpawned)
        {
            if (IsServer)
            {
                multiplayer.FinishRoundClientRpc(currentPanoLat, currentPanoLng,
                                     multiplayer.hostGuessLat.Value, multiplayer.hostGuessLng.Value,
                                     multiplayer.clientGuessLat.Value, multiplayer.clientGuessLng.Value,
                                     multiplayer.hostReady.Value, multiplayer.clientReady.Value);
            }
        }
        else
        {
            if (hasClickedMap)
            {
                ProcessResult(currentPanoLat, currentPanoLng, lastClickLat, lastClickLng);
            }
            else
            {
                ProcessResult(currentPanoLat, currentPanoLng, 0, 0);
            }
            isResultPhase = true;
        }
    }
    #endregion

    #region РЕЗУЛЬТАТИ

    public void ProcessResult(float targetLat, float targetLng, float guessLat, float guessLng, float? opponentLat = null, float? opponentLng = null)
    {
        UpdateBrowserMapState(true, true);

        float distance = calculator.CalculateDistance(targetLat, targetLng, guessLat, guessLng);
        int roundScore = calculator.CalculateScore(distance);
        totalScore += roundScore;

        if (opponentLat.HasValue && opponentLng.HasValue)
        {
            ShowMultiplayerResultInBrowser(targetLat, targetLng, guessLat, guessLng, opponentLat.Value, opponentLng.Value);
        }
        else
        {
            ShowLineInBrowser(guessLat, guessLng);
        }

        ui.SetDistanceText($"{(int)distance}km");
        ui.SetPointsText($"{roundScore} XP");
        needToUpdateUI = true;

        if (currentRound >= maxRounds)
            ui.SetNextButtonText(IsSpawned && !IsServer ? "Waiting result" : "Result");
        else
            ui.SetNextButtonText(IsSpawned && !IsServer ? "Wait Host" : "Continua");
    }
    #endregion

    #region ВЗАЄМОДІЯ З БРАУЗЕРОМ ТА КАРТОЮ
    void LoadPanorama()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "panorama.html");
        string url = "file:///" + filePath.Replace("\\", "/");
        MethodInfo loadMethod = browserClient.GetType().GetMethod("LoadUrl", new Type[] { typeof(string) });
        loadMethod?.Invoke(browserClient, new object[] { url });
    }

    void FindBrowserClient()
    {
        Component[] allComponents = browserObject.GetComponentsInChildren<Component>();
        foreach (var comp in allComponents)
        {
            if (comp == null) continue;
            FieldInfo field = comp.GetType().GetField("browserClient", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            if (field != null)
            {
                browserClient = field.GetValue(comp);
                if (browserClient != null) break;
            }
        }
    }

    public void TeleportToRandomLocation()
    {
        if (IsSpawned && !IsServer) return;

        float newLat = UnityEngine.Random.Range(-20f, 60f);
        float newLng = UnityEngine.Random.Range(-100f, 130f);

        MoveTo(newLat, newLng, true);
    }

    public void MoveTo(float lat, float lng, bool updateNetwork = true)
    {
        currentPanoLat = lat;
        currentPanoLng = lng;

        if (browserClient != null)
        {
            string latStr = lat.ToString(CultureInfo.InvariantCulture);
            string lngStr = lng.ToString(CultureInfo.InvariantCulture);

            string isExact = updateNetwork ? "false" : "true";

            MethodInfo method = browserClient.GetType().GetMethod("ExecuteJs", new Type[] { typeof(string) });
            method?.Invoke(browserClient, new object[] { $"updateLocation({latStr}, {lngStr}, {isExact});" });
        }

        if (IsSpawned && IsServer && updateNetwork)
        {
            multiplayer.netPanoLat.Value = lat;
            multiplayer.netPanoLng.Value = lng;
        }
    }

    void UpdateBrowserMapState(bool fullscreen, bool locked)
    {
        if (browserClient != null)
        {
            string fs = fullscreen.ToString().ToLower();
            string lk = locked.ToString().ToLower();
            MethodInfo method = browserClient.GetType().GetMethod("ExecuteJs", new Type[] { typeof(string) });
            method?.Invoke(browserClient, new object[] { $"setMapState({fs}, {lk});" });
        }
    }

    void ShowLineInBrowser(float clickLat, float clickLng)
    {
        string tLat = currentPanoLat.ToString(CultureInfo.InvariantCulture);
        string tLng = currentPanoLng.ToString(CultureInfo.InvariantCulture);
        MethodInfo method = browserClient.GetType().GetMethod("ExecuteJs", new Type[] { typeof(string) });
        method?.Invoke(browserClient, new object[] { $"showResult({tLat}, {tLng});" });
    }

    void ShowMultiplayerResultInBrowser(float tLat, float tLng, float mLat, float mLng, float oLat, float oLng)
    {
        string tL = tLat.ToString(CultureInfo.InvariantCulture);
        string tG = tLng.ToString(CultureInfo.InvariantCulture);
        string mL = mLat.ToString(CultureInfo.InvariantCulture);
        string mG = mLng.ToString(CultureInfo.InvariantCulture);
        string oL = oLat.ToString(CultureInfo.InvariantCulture);
        string oG = oLng.ToString(CultureInfo.InvariantCulture);

        MethodInfo method = browserClient.GetType().GetMethod("ExecuteJs", new Type[] { typeof(string) });
        method?.Invoke(browserClient, new object[] { $"showMultiplayerResult({tL}, {tG}, {mL}, {mG}, {oL}, {oG});" });
        UpdateBrowserMapState(true, true);
    }

    void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        if (logString.Contains("ACTUAL_PANO_POS:"))
        {
            string data = logString.Substring(logString.IndexOf("ACTUAL_PANO_POS:") + 16).Trim();
            string[] parts = data.Split(',');
            if (parts.Length >= 2)
            {
                float newLat = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float newLng = float.Parse(parts[1], CultureInfo.InvariantCulture);

                currentPanoLat = newLat;
                currentPanoLng = newLng;

                if (IsSpawned && IsServer)
                {
                    if (Mathf.Abs(multiplayer.netPanoLat.Value - newLat) > 0.00001f || Mathf.Abs(multiplayer.netPanoLng.Value - newLng) > 0.00001f)
                    {
                        isUpdatingFromBrowser = true;
                        multiplayer.netPanoLat.Value = newLat;
                        multiplayer.netPanoLng.Value = newLng;
                        isUpdatingFromBrowser = false;
                    }
                }
            }
        }

        if (logString.Contains("CLICK_POS:") && !isResultPhase)
        {
            string dataPart = logString.Substring(logString.IndexOf("CLICK_POS:") + 10).Trim();
            string[] parts = dataPart.Split(',');

            if (parts.Length >= 2)
            {

                lastClickLat = float.Parse(parts[0], CultureInfo.InvariantCulture);
                lastClickLng = float.Parse(parts[1], CultureInfo.InvariantCulture);
                hasClickedMap = true;
                needToUpdateUI = true;
            }
        }
        if (logString.Contains("RETRY_RANDOM"))
        {
            needToRetryRandom = true;
        }
    }
    #endregion
}