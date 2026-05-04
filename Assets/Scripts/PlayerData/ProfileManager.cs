using UnityEngine;

public class ProfileManager : MonoBehaviour
{
    public static ProfileManager Instance { get; private set; }

    [Header("Поточний профіль")]
    public PlayerData activeProfile;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadData();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void LoadData()
    {
        activeProfile = SaveManager.Load();
    }

    public void SaveData()
    {
        SaveManager.Save(activeProfile);
    }

    public void SetPlayerName(string newName)
    {
        activeProfile.playerName = newName;
        SaveData();
    }
    public void AddGameResult(int score)
    {
        activeProfile.gamesPlayed++;

        if (score > activeProfile.bestScore)
        {
            activeProfile.bestScore = score;
        }

        SaveData();
    }

    public void UpdateXPNetwork(int XPChange)
    {
        activeProfile.level += XPChange;

        SaveData();
        Debug.Log($"<color=cyan>[XP System]</color> Зміна XP: {XPChange}. Поточний рівень: {activeProfile.level}");
    }
}
