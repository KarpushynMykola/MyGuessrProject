using UnityEngine;

public class CalculatorManager : MonoBehaviour
{
    private const float EarthRadius = 6371f;
    private const int maxScorePerRound = 5000;
    private float perfectRadius = 0.05f;
    private float difficultyFactor = 800f;

    public float CalculateDistance(float lat1, float lon1, float lat2, float lon2)
    {
        float dLat = (lat2 - lat1) * Mathf.Deg2Rad;
        float dLon = (lon2 - lon1) * Mathf.Deg2Rad;
        float a = Mathf.Sin(dLat / 2) * Mathf.Sin(dLat / 2) + Mathf.Cos(lat1 * Mathf.Deg2Rad) * Mathf.Cos(lat2 * Mathf.Deg2Rad) * Mathf.Sin(dLon / 2) * Mathf.Sin(dLon / 2);
        return EarthRadius * 2 * Mathf.Atan2(Mathf.Sqrt(a), Mathf.Sqrt(1 - a));
    }

    public int CalculateScore(float distance)
    {
        if (distance <= perfectRadius) return 5000;
        float points = maxScorePerRound * Mathf.Exp(-distance / difficultyFactor);
        return Mathf.Clamp(Mathf.RoundToInt(points), 0, 5000);
    }
}
