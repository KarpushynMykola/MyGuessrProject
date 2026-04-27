using UnityEngine;

[System.Serializable]
public struct MapPoint
{
    public string locationName;
    public float latitude;
    public float longitude;
}

[CreateAssetMenu(fileName = "LocationData", menuName = "Scriptable Objects/LocationData")]
public class LocationData : ScriptableObject
{
    [Header("Загальна інформація")]
    public string packName;
    public string description;
    public Sprite previewImage;

    [Header("Список локацій")]
    public MapPoint[] locations;
    public MapPoint GetRandomLocation()
    {
        if (locations == null || locations.Length == 0)
        {
            Debug.LogError($"[LocationPack] Пакет {packName} порожній!");
            return new MapPoint { latitude = 50.45f, longitude = 30.52f };
        }

        int randomIndex = Random.Range(0, locations.Length);
        return locations[randomIndex];
    }
}
