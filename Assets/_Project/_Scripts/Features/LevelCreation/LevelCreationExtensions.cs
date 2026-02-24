using System.Collections.Generic;
using UnityEngine;
public static class LevelCreationExtensions{
    
    public static bool AreColorsSimilar(Color a, Color b, float tolerance)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tolerance * tolerance;
    }
    public static Dictionary<int, int> BuildColorClusters(PixelArtData art, float tolerance)
    {
        Dictionary<int, int> indexToCluster = new Dictionary<int, int>();
        List<Color> clusterRepresentatives = new List<Color>();

        for (int i = 0; i < art.palette.Count; i++)
        {
            Color c = art.palette[i].color;

            bool assigned = false;

            for (int cluster = 0; cluster < clusterRepresentatives.Count; cluster++)
            {
                if (AreColorsSimilar(c, clusterRepresentatives[cluster], tolerance))
                {
                    indexToCluster[i] = cluster;
                    assigned = true;
                    break;
                }
            }

            if (!assigned)
            {
                clusterRepresentatives.Add(c);
                indexToCluster[i] = clusterRepresentatives.Count - 1;
            }
        }

        return indexToCluster;
    }
}
