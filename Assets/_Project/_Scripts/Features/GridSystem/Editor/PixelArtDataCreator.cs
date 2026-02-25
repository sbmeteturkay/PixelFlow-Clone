using UnityEngine;
using UnityEditor;
using System.Collections.Generic;


public static class PixelArtImporter
{
    [MenuItem("Tools/PixelFlow/Import Pixel Art To Data")]
    public static void Import()
    {
        Texture2D texture = Selection.activeObject as Texture2D;
        if (texture == null)
        {
            Debug.LogError("Select a Texture2D first.");
            return;
        }

        string path = AssetDatabase.GetAssetPath(texture);
        TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);

        importer.textureType = TextureImporterType.Default;
        importer.isReadable = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.SaveAndReimport();

        Color[] pixels = texture.GetPixels();
        int width = texture.width;
        int height = texture.height;

        PixelArtData data = ScriptableObject.CreateInstance<PixelArtData>();
        data.columns = width;
        data.rows = height;

        Dictionary<Color, int> colorToIndex = new Dictionary<Color, int>();

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
                    ColorEntry entry = new ColorEntry();
                    entry.colorName = "Color " + data.palette.Count;
                    entry.color = c;

                    data.palette.Add(entry);
                    colorToIndex[c] = data.palette.Count - 1;
                }

                data.pixels.Add(colorToIndex[c]);
            }
        }
        data.ReverseArrangePixels();

        string savePath = EditorUtility.SaveFilePanelInProject(
            "Save PixelArtData",
            texture.name + "_Data",
            "asset",
            "Save PixelArtData",
            path:"Assets/_Project/Level/PixelArtData"
        );

        if (!string.IsNullOrEmpty(savePath))
        {
            AssetDatabase.CreateAsset(data, savePath);
            AssetDatabase.SaveAssets();
            Debug.Log("PixelArtData created successfully.");
        }
    }
}