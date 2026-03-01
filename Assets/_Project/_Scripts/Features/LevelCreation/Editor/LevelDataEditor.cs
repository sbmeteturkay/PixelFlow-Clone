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

        var (indexToCluster, clusterColors) = LevelCreationExtensions.BuildColorClusters(art, level.colorTolerance);

        Dictionary<int, int> clusterPixelCounts = new Dictionary<int, int>();

        for (int row = 0; row < art.rows; row++)
        {
            for (int col = 0; col < art.columns; col++)
            {
                int idx = art.GetPixelIndex(col, row);
                if (idx < 0) continue;

                int cluster = indexToCluster[idx];

                if (!clusterPixelCounts.ContainsKey(cluster))
                    clusterPixelCounts[cluster] = 0;

                clusterPixelCounts[cluster]++;
            }
        }

        foreach (var kv in clusterPixelCounts)
        {
            int clusterId = kv.Key;
            int totalPixels = kv.Value;

            List<int> portions = SplitIntoParts(
                totalPixels
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
    private List<int> SplitIntoParts(int total)
    {
        if (total <= 0) return new List<int>();

        int minCount = Mathf.Max(1, total / 50);
        int maxCount = Mathf.Max(1, total / 20);
        int count = Random.Range(minCount, maxCount + 1);

        List<int> parts = new();
        int remaining = total;

        for (int i = 0; i < count; i++)
        {
            if (i == count - 1)
            {
                parts.Add(remaining);
                break;
            }

            int otherSlotsLeft = count - i - 1;
            int canTake = remaining - otherSlotsLeft;
            int take = Random.Range(1, Mathf.Max(1, canTake) + 1);

            parts.Add(take);
            remaining -= take;
        }

        return parts;
    }
}
#endif
