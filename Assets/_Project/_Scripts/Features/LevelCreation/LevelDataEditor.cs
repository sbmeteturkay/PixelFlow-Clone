#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(LevelData))]
public class LevelDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        LevelData level = (LevelData)target;

        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Generation", EditorStyles.boldLabel);

        if (level.pixelArt == null)
        {
            EditorGUILayout.HelpBox("Assign a Pixel Art Data asset first.", MessageType.Warning);
            return;
        }

        if (GUILayout.Button("Generate Shooters", GUILayout.Height(32)))
        {
            GenerateShooters(level);
            EditorUtility.SetDirty(target);
        }

        // Preview
        if (level.shooters != null && level.shooters.Count > 0)
        {
            EditorGUILayout.Space(6);
            EditorGUILayout.LabelField($"Total Shooters: {level.shooters.Count}", EditorStyles.miniLabel);

            // Group by color for readability
            Dictionary<int, int> totals = new Dictionary<int, int>();
            foreach (var s in level.shooters)
            {
                if (!totals.ContainsKey(s.colorIndex)) totals[s.colorIndex] = 0;
                totals[s.colorIndex] += s.pixelCount;
            }

            foreach (var kv in totals)
            {
                int colorIdx = kv.Key;
                string colorName = colorIdx < level.pixelArt.palette.Count
                    ? level.pixelArt.palette[colorIdx].colorName
                    : $"Color {colorIdx}";
                EditorGUILayout.LabelField($"  {colorName}: {kv.Value} pixels total", EditorStyles.miniLabel);
            }
        }
    }

    private void GenerateShooters(LevelData level)
    {
        level.shooters = new List<ShooterData>();

        PixelArtData art = level.pixelArt;

        float colorTolerance = level.colorTolerance;

        var indexToCluster = BuildColorClusters(art, colorTolerance);

        Dictionary<int, int> clusterPixelCounts = new Dictionary<int, int>();
        Dictionary<int, Color> clusterColors = new Dictionary<int, Color>();

        for (int row = 0; row < art.rows; row++)
        {
            for (int col = 0; col < art.columns; col++)
            {
                int idx = art.GetPixelIndex(col, row);
                if (idx < 0) continue;

                int cluster = indexToCluster[idx];

                if (!clusterPixelCounts.ContainsKey(cluster))
                {
                    clusterPixelCounts[cluster] = 0;
                    clusterColors[cluster] = art.palette[idx].color;
                }

                clusterPixelCounts[cluster]++;
            }
        }

        foreach (var kv in clusterPixelCounts)
        {
            int clusterId = kv.Key;
            int totalPixels = kv.Value;

            List<int> portions = SplitIntoParts(
                totalPixels,
                level.minShootersPerColor,
                level.maxShootersPerColor,
                level.minPixelsPerShooter,
                level.maxPixelsPerShooter
            );

            foreach (int portion in portions)
            {
                level.shooters.Add(new ShooterData
                {
                    colorIndex = clusterId,
                    pixelCount = portion,
                    targetColor = clusterColors[clusterId]
                });
            }
        }

        Debug.Log($"[LevelData] Generated {level.shooters.Count} shooters.");
    }

    /// <summary>
    /// Splits total into random parts, each between minPart and maxPart,
    /// with a count between minCount and maxCount.
    /// </summary>
    private List<int> SplitIntoParts(int total, int minCount, int maxCount, int minPart, int maxPart)
    {
        // Clamp shooter count to what's actually achievable given total and minPart
        int maxAchievable = Mathf.FloorToInt((float)total / minPart);
        int clampedMax = Mathf.Min(maxCount, maxAchievable);
        int clampedMin = Mathf.Min(minCount, clampedMax);

        if (clampedMin <= 0)
        {
            // Not enough pixels for even one shooter, make one with all pixels
            return new List<int> { total };
        }

        int count = Random.Range(clampedMin, clampedMax + 1);
        List<int> parts = new List<int>();

        int remaining = total;

        for (int i = 0; i < count; i++)
        {
            bool isLast = i == count - 1;

            if (isLast)
            {
                parts.Add(remaining);
                break;
            }

            // Max we can take while leaving enough for the rest
            int otherSlotsLeft = count - i - 1;
            int mustLeave = otherSlotsLeft * minPart;
            int canTake = Mathf.Min(maxPart, remaining - mustLeave);
            int take = Random.Range(minPart, Mathf.Max(minPart, canTake) + 1);

            parts.Add(take);
            remaining -= take;
        }

        return parts;
    }
    
    private bool AreColorsSimilar(Color a, Color b, float tolerance)
    {
        float dr = a.r - b.r;
        float dg = a.g - b.g;
        float db = a.b - b.b;
        return (dr * dr + dg * dg + db * db) <= tolerance * tolerance;
    }
    private Dictionary<int, int> BuildColorClusters(PixelArtData art, float tolerance)
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
#endif
