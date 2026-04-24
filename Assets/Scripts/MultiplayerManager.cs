using Unity.Netcode;
using UnityEngine;

public class MultiplayerManager : NetworkBehaviour
{
    #region ЗМІННІ
    public UIManager ui;
    public GameManager game;
    public CalculatorManager calculator;
    public TimerManager timer;
    public MapManager map;

    public bool networkSyncPending = false;

    public NetworkVariable<float> netPanoLat = new NetworkVariable<float>(50.4501f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> netPanoLng = new NetworkVariable<float>(30.5234f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> hostGuessLat = new NetworkVariable<float>();
    public NetworkVariable<float> hostGuessLng = new NetworkVariable<float>();
    public NetworkVariable<float> clientGuessLat = new NetworkVariable<float>();
    public NetworkVariable<float> clientGuessLng = new NetworkVariable<float>();

    public NetworkVariable<bool> hostReady = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> clientReady = new NetworkVariable<bool>(false);

    public NetworkVariable<int> netMaxRounds = new NetworkVariable<int>(5, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);

    public NetworkVariable<float> netRemainingTime = new NetworkVariable<float>(0f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    public NetworkVariable<float> netTimeLimit = new NetworkVariable<float>(60f, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    #endregion

    #region МЕРЕЖЕВІ RPC ТА ПОДІЇ
    private void Start()
    {
        NetworkManager.Singleton.OnClientConnectedCallback += OnPlayerConnected;
        NetworkManager.Singleton.OnClientDisconnectCallback += OnPlayerDisconnected;
    }

    public override void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnPlayerConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnPlayerDisconnected;
        }
        base.OnDestroy();
    }

    private void OnPlayerConnected(ulong clientId)
    {
        if (IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            ui.SetPlayerStatusText($"Player in lobby: {playerCount}");
            ui.SetStartGameButtonState(playerCount >= 2);
        }
    }

    private void OnPlayerDisconnected(ulong clientId)
    {
        if (IsServer)
        {
            int playerCount = NetworkManager.Singleton.ConnectedClientsIds.Count;
            ui.SetPlayerStatusText($"Player in lobby: {playerCount}");
            ui.SetStartGameButtonState(playerCount >= 2);
        }
    }

    public override void OnNetworkSpawn()
    {
        netPanoLat.OnValueChanged += OnPanoCoordChanged;
        netPanoLng.OnValueChanged += OnPanoCoordChanged;

        netMaxRounds.OnValueChanged += (oldVal, newVal) => { game.maxRounds = newVal; };
        netTimeLimit.OnValueChanged += (oldVal, newVal) => { timer.timeLimit = newVal; };

        if (!IsServer)
        {
            networkSyncPending = true;
            game.maxRounds = netMaxRounds.Value;
            timer.timeLimit = netTimeLimit.Value;
        }

        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += HandleDisconnect;
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback -= HandleDisconnect;
        }
    }

    private void HandleDisconnect(ulong clientId)
    {
        if (!IsServer || clientId == NetworkManager.ServerClientId || game.isGameStarted)
        {
            game.BackToMainMenu();
        }
    }

    public void OnPanoCoordChanged(float oldVal, float newVal)
    {
        if (map.isUpdatingFromBrowser) return;
        if (IsServer) return;

        if (map.isLoaded)
        {
            map.MoveTo(netPanoLat.Value, netPanoLng.Value, false);
        }
        else
        {
            networkSyncPending = true;
        }
    }

    public async void handleHostClicked()
    {
        ui.ShowLobby();
        ui.SetJoinCodeText("Creating");
        ui.SetPlayerStatusText("Waiting players");

        string code = await RelayManager.Instance.CreateRelay();

        if (!string.IsNullOrEmpty(code))
        {
            ui.SetJoinCodeText("Code: " + code);
        }
        else
        {
            ui.SetJoinCodeText("Error");
        }
    }

    public async void handleJoinClicked()
    {
        string code = ui.joinInputField.text;
        if (code.Length != 6) return;

        try
        {
            await RelayManager.Instance.JoinRelay(code);

            ui.ShowLobby();
            ui.SetStartGameButtonState(false);
            ui.SetPlayerStatusText("You've joined! Waiting host");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Помилка приєднання до Relay: {e.Message}");
            ui.joinCodeText.text = e.Message;
        }
    }

    public void handleStartGameClicked()
    {
        if (IsServer)
        {
            StartHostGameClientRpc();
        }
    }

    public void HandleButtonClick()
    {
        if (!game.isResultPhase)
        {
            if (!game.hasClickedMap) return;

            if (IsSpawned)
            {
                ui.SetNextButtonState(false);
                ui.SetNextButtonText("Wait opponent");
                SubmitReadyServerRpc(game.lastClickLat, game.lastClickLng);
            }
            else
            {
                game.ProcessResult(game.currentPanoLat, game.currentPanoLng, game.lastClickLat, game.lastClickLng);
                game.isResultPhase = true;
                timer.isTimerRunning = false;
            }
        }
        else
        {
            if (IsSpawned)
            {
                if (IsServer)
                {
                    if (game.currentRound >= game.maxRounds) ShowFinalSummaryClientRpc();
                    else StartNextRoundClientRpc();
                }
            }
            else
            {
                if (game.currentRound >= game.maxRounds) ui.ShowFinalSummary(IsSpawned, game.hostTotalScoreMP, game.clientTotalScoreMP, game.totalScore);
                else game.StartNextRound();
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    public void SubmitReadyServerRpc(float lat, float lng, ServerRpcParams rpcParams = default)
    {
        if (rpcParams.Receive.SenderClientId == NetworkManager.ServerClientId)
        {
            hostGuessLat.Value = lat;
            hostGuessLng.Value = lng;
            hostReady.Value = true;
        }
        else
        {
            clientGuessLat.Value = lat;
            clientGuessLng.Value = lng;
            clientReady.Value = true;
        }

        if (hostReady.Value && clientReady.Value)
        {
            FinishRoundClientRpc(game.currentPanoLat, game.currentPanoLng,
                                 hostGuessLat.Value, hostGuessLng.Value,
                                 clientGuessLat.Value, clientGuessLng.Value,
                                 hostReady.Value, clientReady.Value);
        }
    }

    [ClientRpc]
    private void StartHostGameClientRpc()
    {
        ui.showPanel(ui.lobbyPanel, false);

        Debug.Log("Гра починається для всіх!");

        if (IsServer)
        {
            game.StartGame();
        }
        else
        {
            game.StartGame();
        }
    }

    [ClientRpc]
    public void StartGameClientRpc(int rounds, float time)
    {
        game.maxRounds = rounds;
        timer.timeLimit = time;

        game.ExecuteStartGameLogic();
    }

    [ClientRpc]
    public void StartNextRoundClientRpc()
    {
        if (IsServer)
        {
            hostReady.Value = false;
            clientReady.Value = false;
        }
        game.StartNextRound();
    }

    [ClientRpc]
    public void FinishRoundClientRpc(float targetLat, float targetLng, float hLat, float hLng, float cLat, float cLng, bool hReady, bool cReady)
    {
        game.isResultPhase = true;
        timer.isTimerRunning = false;
        ui.SetNextButtonState(true);

        float hDist = calculator.CalculateDistance(targetLat, targetLng, hLat, hLng);
        int hScore = (hReady) ? calculator.CalculateScore(hDist) : 0;
        game.hostTotalScoreMP += hScore;

        float cDist = calculator.CalculateDistance(targetLat, targetLng, cLat, cLng);
        int cScore = (cReady) ? calculator.CalculateScore(cDist) : 0;
        game.clientTotalScoreMP += cScore;

        string hStatus = hReady ? $"{hDist:F1} km" : "Didn't have time";
        string cStatus = cReady ? $"{cDist:F1} km" : "Didn't have time";

        ui.ShowRoundSummary($"Result round {game.currentRound}",
                            $"{cScore}", cStatus, $"{game.clientTotalScoreMP}",
                            $"{hScore}", hStatus, $"{game.hostTotalScoreMP}");

        bool myReady = IsServer ? hReady : cReady;
        bool oppReady = IsServer ? cReady : hReady;

        float myLat = IsServer ? hLat : cLat;
        float myLng = IsServer ? hLng : cLng;
        float oppLat = IsServer ? cLat : hLat;
        float oppLng = IsServer ? cLng : hLng;

        if (myReady && oppReady)
        {
            game.ProcessResult(targetLat, targetLng, myLat, myLng, oppLat, oppLng);
        }
        else if (myReady && !oppReady)
        {
            game.ProcessResult(targetLat, targetLng, myLat, myLng, 0, 0);
        }
        else if (!myReady && oppReady)
        {
            game.ProcessResult(targetLat, targetLng, 0, 0, oppLat, oppLng);
        }
        else
        {
            game.ProcessResult(targetLat, targetLng, 0, 0, 0, 0);
        }

        if (IsSpawned && !IsServer) ui.SetNextButtonState(false);
    }

    [ClientRpc]
    public void ShowFinalSummaryClientRpc()
    {
        ui.ShowFinalSummary(IsSpawned, game.hostTotalScoreMP, game.clientTotalScoreMP, game.totalScore);
    }

    [ClientRpc]
    public void RestartGameClientRpc()
    {
        if (IsServer)
        {
            hostReady.Value = false;
            clientReady.Value = false;
        }
        game.RestartGame();
    }
    #endregion
}
