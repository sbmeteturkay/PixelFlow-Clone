using System.Collections.Generic;
using System.Linq;
using UnityEngine;
public static class LevelCreationExtensions{
    
    public static bool AreColorsSimilar(Color a, Color b, float tolerance)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tolerance * tolerance;
    }
    public static (Dictionary<int, int> indexToCluster, List<Color> clusterColors) 
        BuildColorClusters(PixelArtData art, float tolerance)
    {
        Dictionary<int, int> indexToCluster = new();
        List<Color> representatives = new();
        List<List<Color>> clusterMembers = new(); // her clusterdaki tüm renkler

        for (int i = 0; i < art.palette.Count; i++)
        {
            Color c = art.palette[i].color;
            bool assigned = false;

            for (int cluster = 0; cluster < representatives.Count; cluster++)
            {
                if (AreColorsSimilar(c, representatives[cluster], tolerance))
                {
                    indexToCluster[i] = cluster;
                    clusterMembers[cluster].Add(c);
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
            {
                indexToCluster[i] = representatives.Count;
                representatives.Add(c);
                clusterMembers.Add(new List<Color> { c });
            }
        }

        // Her cluster için ortalama renk hesapla
        List<Color> clusterColors = clusterMembers
            .Select(AverageColor)
            .ToList();

        return (indexToCluster, clusterColors);
    }

    private static Color AverageColor(List<Color> colors)
    {
        float r = 0, g = 0, b = 0;
        foreach (Color c in colors) { r += c.r; g += c.g; b += c.b; }
        return new Color(r / colors.Count, g / colors.Count, b / colors.Count);
    }
}
