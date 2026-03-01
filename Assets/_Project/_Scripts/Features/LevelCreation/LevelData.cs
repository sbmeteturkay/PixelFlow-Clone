using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewLevel", menuName = "PixelFlow/Level Data")]
public class LevelData : ScriptableObject
{
    [Header("Grid")]
    public PixelArtData pixelArt;

    [Header("Generation Settings")]
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
