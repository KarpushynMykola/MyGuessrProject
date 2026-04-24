using UnityEngine;

public class TimerManager : MonoBehaviour
{
    [Header("Посилання")]
    public GameManager game;
    public MultiplayerManager multiplayer;

    [Header("Налаштування Таймера")]
    public float timeLimit = 60f;
    public float currentTime;
    public bool isTimerRunning = false;

    #region ТАЙМЕР
    public void StartTimer(bool IsSpawned, bool IsServer)
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

    public void OnTimeUp(bool IsSpawned, bool IsServer)
    {
        isTimerRunning = false;

        if (IsSpawned)
        {
            if (IsServer)
            {
                multiplayer.FinishRoundClientRpc(game.currentPanoLat, game.currentPanoLng,
                                     multiplayer.hostGuessLat.Value, multiplayer.hostGuessLng.Value,
                                     multiplayer.clientGuessLat.Value, multiplayer.clientGuessLng.Value,
                                     multiplayer.hostReady.Value, multiplayer.clientReady.Value);
            }
        }
        else
        {
            if (game.hasClickedMap)
            {
                game.ProcessResult(game.currentPanoLat, game.currentPanoLng, game.lastClickLat, game.lastClickLng);
            }
            else
            {
                game.ProcessResult(game.currentPanoLat, game.currentPanoLng, 0, 0);
            }
            game.isResultPhase = true;
        }
    }
    #endregion
}
