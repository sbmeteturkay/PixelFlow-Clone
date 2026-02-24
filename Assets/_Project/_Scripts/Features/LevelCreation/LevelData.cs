using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "PixelFlow/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    public PixelArtData pixelArt;

    [Header("Generation Settings")]
    public int minShootersPerColor = 2;
    public int maxShootersPerColor = 6;
    public int minPixelsPerShooter = 5;
    public int maxPixelsPerShooter = 50;
    public float colorTolerance = .5f;

    [Header("Generated Shooter Data")]
    public List<ShooterData> shooters = new List<ShooterData>();
}

[System.Serializable]
public class ShooterData
{
    public Color targetColor;
    public int colorIndex;
    public int pixelCount;
}
