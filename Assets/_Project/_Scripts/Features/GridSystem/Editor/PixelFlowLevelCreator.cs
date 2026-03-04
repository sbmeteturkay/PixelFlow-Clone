#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public class PixelFlowLevelCreator : EditorWindow
{
    private readonly List<Texture2D> _textures = new();
    private float _colorTolerance = 0.25f;
    private string _levelDataSavePath = "Assets/_Project/Level/LevelData";
    private int _maxHeight = 30;

    private int _maxWidth = 40;

    // ── Settings ──────────────────────────────────────────────────────
    private string _pixelArtSavePath = "Assets/_Project/Level/PixelArtData";
    private Vector2 _scroll;

    // ── GUI ───────────────────────────────────────────────────────────

    private void OnGUI()
    {
        _scroll = EditorGUILayout.BeginScrollView(_scroll);

        EditorGUILayout.Space(6);
        EditorGUILayout.LabelField("Save Paths", EditorStyles.boldLabel);
        _pixelArtSavePath = EditorGUILayout.TextField("Pixel Art Path", _pixelArtSavePath);
        _levelDataSavePath = EditorGUILayout.TextField("Level Data Path", _levelDataSavePath);

        if (GUILayout.Button("Clear"))
        {
            AssetDatabase.DeleteAsset(_pixelArtSavePath);
            AssetDatabase.DeleteAsset(_levelDataSavePath);
        }

        if (GUILayout.Button("Create Levels", GUILayout.Height(36)))
            CreateLevels();

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Generation Settings", EditorStyles.boldLabel);
        _colorTolerance = EditorGUILayout.Slider("Color Tolerance", _colorTolerance, 0f, 1f);

        EditorGUILayout.Space(4);
        EditorGUILayout.LabelField("Max Texture Size", EditorStyles.boldLabel);
        _maxWidth = EditorGUILayout.IntField("Max Width", _maxWidth);
        _maxHeight = EditorGUILayout.IntField("Max Height", _maxHeight);
        EditorGUILayout.HelpBox($"Textures larger than {_maxWidth}x{_maxHeight} will be downscaled.", MessageType.Info);

        EditorGUILayout.Space(8);
        EditorGUILayout.LabelField("Textures", EditorStyles.boldLabel);

        Rect dropArea = GUILayoutUtility.GetRect(0, 50, GUILayout.ExpandWidth(true));
        GUI.Box(dropArea, "Drag & Drop Textures Here");
        HandleDrop(dropArea);

        if (GUILayout.Button("Add Selected Textures"))
            AddSelectedTextures();

        EditorGUILayout.Space(4);

        for (int i = _textures.Count - 1; i >= 0; i--)
        {
            EditorGUILayout.BeginHorizontal();
            _textures[i] = (Texture2D)EditorGUILayout.ObjectField(_textures[i], typeof(Texture2D), false);
            if (GUILayout.Button("✕", GUILayout.Width(24)))
                _textures.RemoveAt(i);
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.Space(8);

        GUI.enabled = _textures.Count > 0;
        if (GUILayout.Button("Clear List"))
            _textures.Clear();

        EditorGUILayout.Space(4);


        GUI.enabled = true;
        EditorGUILayout.EndScrollView();
    }

    // ── Menu ──────────────────────────────────────────────────────────

    [MenuItem("Tools/PixelFlow/Level Creator")]
    public static void Open()
    {
        GetWindow<PixelFlowLevelCreator>("PixelFlow Level Creator");
    }

    // ── Drag & Drop ───────────────────────────────────────────────────

    private void HandleDrop(Rect dropArea)
    {
        Event e = Event.current;
        if (!dropArea.Contains(e.mousePosition)) return;

        if (e.type == EventType.DragUpdated)
        {
            DragAndDrop.visualMode = DragAndDropVisualMode.Copy;
            e.Use();
        }
        else if (e.type == EventType.DragPerform)
        {
            DragAndDrop.AcceptDrag();
            foreach (Object obj in DragAndDrop.objectReferences)
            {
                if (obj is Texture2D tex && !_textures.Contains(tex))
                    _textures.Add(tex);
            }

            e.Use();
        }
    }

    private void AddSelectedTextures()
    {
        foreach (Object obj in Selection.objects)
        {
            if (obj is Texture2D tex && !_textures.Contains(tex))
                _textures.Add(tex);
        }
    }

    // ═════════════════════════════════════════════════════════════════
    // Level Creation
    // ═════════════════════════════════════════════════════════════════

    private void CreateLevels()
    {
        EnsureDirectory(_pixelArtSavePath);
        EnsureDirectory(_levelDataSavePath);

        int created = 0;

        foreach (Texture2D tex in _textures)
        {
            if (tex == null) continue;

            PixelArtData pixelArt = ImportTexture(tex);
            if (pixelArt == null) continue;

            string artPath = $"{_pixelArtSavePath}/{tex.name}_Data.asset";
            AssetDatabase.CreateAsset(pixelArt, artPath);

            LevelData level = CreateInstance<LevelData>();
            level.pixelArt = pixelArt;
            level.colorTolerance = _colorTolerance;

            GenerateShooters(level);

            string levelPath = $"{_levelDataSavePath}/{tex.name}_Level.asset";
            AssetDatabase.CreateAsset(level, levelPath);

            created++;
        }

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[PixelFlow] {created} level(s) created.");
    }

    // ═════════════════════════════════════════════════════════════════
    // Texture → PixelArtData
    // ═════════════════════════════════════════════════════════════════

    private PixelArtData ImportTexture(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        // Downscale if needed
        Texture2D source = texture.width > _maxWidth || texture.height > _maxHeight
            ? ResizeTexture(texture, _maxWidth, _maxHeight)
            : texture;

        Color[] pixels = source.GetPixels();
        int width = source.width;
        int height = source.height;

        PixelArtData data = CreateInstance<PixelArtData>();
        data.columns = width;
        data.rows = height;

        Dictionary<Color, int> colorToIndex = new();
        data.palette.Clear();
        data.pixels.Clear();

        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                Color c = pixels[y * width + x];

                if (c.a < 0.1f)
                {
                    data.pixels.Add(-1);
                    continue;
                }

                if (!colorToIndex.ContainsKey(c))
                {
                    data.palette.Add(new()
                    {
                        colorName = "Color " + data.palette.Count,
                        color = c
                    });
                    colorToIndex[c] = data.palette.Count - 1;
                }

                data.pixels.Add(colorToIndex[c]);
            }
        }

        data.ReverseArrangePixels();
        return data;
    }

    private Texture2D ResizeTexture(Texture2D source, int maxWidth, int maxHeight)
    {
        float ratio = Mathf.Min((float)maxWidth / source.width, (float)maxHeight / source.height);
        int newWidth = Mathf.Max(1, Mathf.RoundToInt(source.width * ratio));
        int newHeight = Mathf.Max(1, Mathf.RoundToInt(source.height * ratio));

        RenderTexture rt = RenderTexture.GetTemporary(newWidth, newHeight, 0, RenderTextureFormat.ARGB32);
        rt.filterMode = FilterMode.Point;
        Graphics.Blit(source, rt);

        RenderTexture.active = rt;
        Texture2D result = new(newWidth, newHeight, TextureFormat.RGBA32, false);
        result.ReadPixels(new(0, 0, newWidth, newHeight), 0, 0);
        result.Apply();

        RenderTexture.active = null;
        RenderTexture.ReleaseTemporary(rt);
        return result;
    }

    // ═════════════════════════════════════════════════════════════════
    // Shooter Generation
    // ═════════════════════════════════════════════════════════════════

    private void GenerateShooters(LevelData level)
    {
        level.shooters = new();

        PixelArtData art = level.pixelArt;
        (Dictionary<int, int> indexToCluster, List<Color> clusterColors) =
            LevelCreationExtensions.BuildColorClusters(art, level.colorTolerance);

        Dictionary<int, int> clusterPixelCounts = new();

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

        foreach (KeyValuePair<int, int> kv in clusterPixelCounts)
        {
            int clusterId = kv.Key;
            int totalPixels = kv.Value;

            List<int> portions = SplitIntoParts(totalPixels);

            foreach (int portion in portions)
            {
                level.shooters.Add(new()
                {
                    colorIndex = clusterId,
                    pixelCount = portion,
                    targetColor = clusterColors[clusterId]
                });
            }
        }

        //shuffle
        level.shooters = level.shooters.OrderBy(i => Guid.NewGuid()).ToList();
    }

    private List<int> SplitIntoParts(int total)
    {
        if (total <= 0) return new();

        int[] allowedSizes = { 10, 20, 30, 40 };
        List<int> parts = new();
        int remaining = total;

        while (remaining > 0)
        {
            int[] possible = allowedSizes.Where(s => s <= remaining).ToArray();

            if (possible.Length == 0)
            {
                parts.Add(remaining);
                break;
            }

            int take = possible[Random.Range(0, possible.Length)];
            parts.Add(take);
            remaining -= take;
        }

        return parts;
    }

    // ── Helpers ───────────────────────────────────────────────────────

    private void EnsureDirectory(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;

        string[] parts = path.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++)
        {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next))
                AssetDatabase.CreateFolder(current, parts[i]);
            current = next;
        }
    }
}
#endif