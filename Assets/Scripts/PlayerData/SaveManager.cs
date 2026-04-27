using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string SavePath => Path.Combine(Directory.GetParent(Application.dataPath).FullName, "player_profile.json");

    public static void Save(PlayerData data)
    {
        string json = JsonUtility.ToJson(data, true);
        File.WriteAllText(SavePath, json);
        Debug.Log($"<color=green>[SaveManager]</color> Дані успішно збережено за шляхом: {SavePath}");
    }

    public static PlayerData Load()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            PlayerData loadedData = JsonUtility.FromJson<PlayerData>(json);
            Debug.Log("<color=blue>[SaveManager]</color> Профіль гравця завантажено.");
            return loadedData;
        }
        else
        {
            Debug.Log("<color=orange>[SaveManager]</color> Файл збереження не знайдено. Створюємо новий профіль.");
            return new PlayerData();
        }
    }
}