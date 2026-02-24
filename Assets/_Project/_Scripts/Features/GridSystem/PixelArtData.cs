using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "NewPixelArt", menuName = "PixelFlow/Pixel Art Data")]
public class PixelArtData : ScriptableObject
{
    [Header("Grid Size")]
    public int columns = 30;
    public int rows = 25;

    [Header("Color Palette")]
    public List<ColorEntry> palette = new List<ColorEntry>();

    [Header("Pixel Data (row * column elements, palette index, -1 = empty)")]
    public List<int> pixels = new List<int>();

    public int GetPixelIndex(int col, int row)
    {
        if (col < 0 || col >= columns || row < 0 || row >= rows) return -1;
        int i = row * columns + col;
        if (i >= pixels.Count) return -1;
        return pixels[i];
    }

    public Color GetPixelColor(int col, int row)
    {
        int idx = GetPixelIndex(col, row);
        if (idx < 0 || idx >= palette.Count) return Color.clear;
        return palette[idx].color;
    }

    public void ResetPixels()
    {
        pixels = new List<int>(new int[columns * rows]);
        for (int i = 0; i < pixels.Count; i++) pixels[i] = -1;
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        int required = columns * rows;
        while (pixels.Count < required) pixels.Add(-1);
        while (pixels.Count > required) pixels.RemoveAt(pixels.Count - 1);
    }
#endif
}

[System.Serializable]
public class ColorEntry
{
    public string colorName = "Color";
    public Color color = Color.white;
}
