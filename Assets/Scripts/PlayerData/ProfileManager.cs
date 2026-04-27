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

    public void UpdateXPNetwork(int myScore, int oppScore, int oppLevel)
    {
        bool isWin = myScore >= oppScore;
        int scoreDiff = Mathf.Abs(myScore - oppScore);
        int levelDiff = oppLevel - activeProfile.level;

        float totalXPChange = 0;

        if (isWin)
        {
            float baseWin = 10f;
            float scoreBonus = Mathf.Clamp(scoreDiff / 5000f, 0f, 5f);
            float levelBonus = Mathf.Clamp(levelDiff / 100f * 2.5f, 0f, 5f);

            totalXPChange = baseWin + scoreBonus + levelBonus;
        }
        else
        {
            float baseLoss = -5f;
            float scorePenalty = Mathf.Clamp(scoreDiff / 5000f, 0f, 3f);
            float levelMitigation = Mathf.Clamp(levelDiff / 100f * 1.5f, 0f, 2f);

            totalXPChange = baseLoss - scorePenalty + levelMitigation;
        }

        activeProfile.level += Mathf.RoundToInt(totalXPChange);

        SaveData();
        Debug.Log($"<color=cyan>[XP System]</color> Зміна XP: {totalXPChange}. Поточний рівень: {activeProfile.level}");
    }
}
