using System;
using System.Globalization;
using System.IO;
using System.Reflection;
using UnityEngine;


public class MapManager : MonoBehaviour
{
    #region ПОСИЛАННЯ
    [Header("Посилання")]
    public GameManager game;
    public MultiplayerManager multiplayer;
    #endregion

    #region ЗМІННІ
    [Header("Налаштування браузера")]
    public GameObject browserObject;
    public object browserClient;
    public bool isUpdatingFromBrowser = false;
    public bool isLoaded = false;
    #endregion

    #region ВЗАЄМОДІЯ З БРАУЗЕРОМ ТА КАРТОЮ
    public void LoadPanorama()
    {
        string filePath = Path.Combine(Application.streamingAssetsPath, "panorama.html");
        string url = "file:///" + filePath.Replace("\\", "/");
        MethodInfo loadMethod = browserClient.GetType().GetMethod("LoadUrl", new Type[] { typeof(string) });
        loadMethod?.Invoke(browserClient, new object[] { url });
    }

    public void FindBrowserClient()
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
        bool IsSpawned = game.IsSpawned;
        bool IsServer = game.IsServer;

        if (IsSpawned && !IsServer) return;

        float newLat = UnityEngine.Random.Range(-20f, 60f);
        float newLng = UnityEngine.Random.Range(-100f, 130f);

        MoveTo(newLat, newLng, true);
    }

    public void MoveTo(float lat, float lng, bool updateNetwork = true)
    {
        bool IsSpawned = game.IsSpawned;
        bool IsServer = game.IsServer;

        game.currentPanoLat = lat;
        game.currentPanoLng = lng;

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

    public void UpdateBrowserMapState(bool fullscreen, bool locked)
    {
        if (browserClient != null)
        {
            string fs = fullscreen.ToString().ToLower();
            string lk = locked.ToString().ToLower();
            MethodInfo method = browserClient.GetType().GetMethod("ExecuteJs", new Type[] { typeof(string) });
            method?.Invoke(browserClient, new object[] { $"setMapState({fs}, {lk});" });
        }
    }

    public void ShowLineInBrowser(float clickLat, float clickLng)
    {
        string tLat = game.currentPanoLat.ToString(CultureInfo.InvariantCulture);
        string tLng = game.currentPanoLng.ToString(CultureInfo.InvariantCulture);
        MethodInfo method = browserClient.GetType().GetMethod("ExecuteJs", new Type[] { typeof(string) });
        method?.Invoke(browserClient, new object[] { $"showResult({tLat}, {tLng});" });
    }

    public void ShowMultiplayerResultInBrowser(float tLat, float tLng, float mLat, float mLng, float oLat, float oLng)
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

    public void HandleUnityLog(string logString, string stackTrace, LogType type)
    {
        bool IsSpawned = game.IsSpawned;
        bool IsServer = game.IsServer;

        if (logString.Contains("ACTUAL_PANO_POS:"))
        {
            string data = logString.Substring(logString.IndexOf("ACTUAL_PANO_POS:") + 16).Trim();
            string[] parts = data.Split(',');
            if (parts.Length >= 2)
            {
                float newLat = float.Parse(parts[0], CultureInfo.InvariantCulture);
                float newLng = float.Parse(parts[1], CultureInfo.InvariantCulture);

                game.currentPanoLat = newLat;
                game.currentPanoLng = newLng;

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

        if (logString.Contains("CLICK_POS:") && !game.isResultPhase)
        {
            string dataPart = logString.Substring(logString.IndexOf("CLICK_POS:") + 10).Trim();
            string[] parts = dataPart.Split(',');

            if (parts.Length >= 2)
            {

                game.lastClickLat = float.Parse(parts[0], CultureInfo.InvariantCulture);
                game.lastClickLng = float.Parse(parts[1], CultureInfo.InvariantCulture);
                game.hasClickedMap = true;
                game.needToUpdateUI = true;
            }
        }
        if (logString.Contains("RETRY_RANDOM"))
        {
            game.needToRetryRandom = true;
        }
    }
    #endregion
}
