#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(PixelArtData))]
public class PixelArtDataEditor : Editor
{
    private int _selectedPaletteIndex = 0;
    private const float CELL_PX = 24f;
    private Vector2 _scrollPos;

    public override void OnInspectorGUI()
    {
        PixelArtData data = (PixelArtData)target;

        EditorGUI.BeginChangeCheck();

        // ── Grid Size ─────────────────────────────────────────────
        EditorGUILayout.LabelField("Grid Size", EditorStyles.boldLabel);
        int newCols = EditorGUILayout.IntField("Columns", data.columns);
        int newRows = EditorGUILayout.IntField("Rows", data.rows);

        if (newCols != data.columns || newRows != data.rows)
        {
            data.columns = Mathf.Max(1, newCols);
            data.rows = Mathf.Max(1, newRows);
            data.ResetPixels();
        }

        EditorGUILayout.Space(6);

        // ── Palette ───────────────────────────────────────────────
        EditorGUILayout.LabelField("Color Palette", EditorStyles.boldLabel);
        SerializedObject so = new SerializedObject(target);
        so.Update();
        EditorGUILayout.PropertyField(so.FindProperty("palette"), true);
        so.ApplyModifiedProperties();

        EditorGUILayout.Space(4);

        // Palette selector buttons
        EditorGUILayout.LabelField("Active Color:", EditorStyles.miniLabel);
        EditorGUILayout.BeginHorizontal();

        // Eraser
        GUI.backgroundColor = _selectedPaletteIndex == -1 ? Color.yellow : Color.gray;
        if (GUILayout.Button("✕", GUILayout.Width(CELL_PX), GUILayout.Height(CELL_PX)))
            _selectedPaletteIndex = -1;

        for (int i = 0; i < data.palette.Count; i++)
        {
            Color c = data.palette[i].color;
            GUI.backgroundColor = i == _selectedPaletteIndex ? Color.Lerp(c, Color.white, 0.4f) : c;
            if (GUILayout.Button(i.ToString(), GUILayout.Width(CELL_PX), GUILayout.Height(CELL_PX)))
                _selectedPaletteIndex = i;
        }

        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space(8);

        // ── Pixel Grid ────────────────────────────────────────────
        EditorGUILayout.LabelField("Pixel Art", EditorStyles.boldLabel);

        _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos,
            GUILayout.Height(Mathf.Min(data.rows * (CELL_PX + 2) + 20, 500)));

        for (int row = 0; row < data.rows; row++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int col = 0; col < data.columns; col++)
            {
                int idx = data.GetPixelIndex(col, row);
                GUI.backgroundColor = idx >= 0 && idx < data.palette.Count
                    ? data.palette[idx].color
                    : new Color(0.15f, 0.15f, 0.15f);

                if (GUILayout.Button("", GUILayout.Width(CELL_PX), GUILayout.Height(CELL_PX)))
                {
                    data.pixels[row * data.columns + col] = _selectedPaletteIndex;
                    EditorUtility.SetDirty(data);
                }
            }
            GUI.backgroundColor = Color.white;
            EditorGUILayout.EndHorizontal();
        }

        EditorGUILayout.EndScrollView();

        EditorGUILayout.Space(8);

        // ── Tools ─────────────────────────────────────────────────
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All"))
        {
            data.ResetPixels();
            EditorUtility.SetDirty(data);
        }
        if (GUILayout.Button("Fill All") && _selectedPaletteIndex >= 0)
        {
            for (int i = 0; i < data.pixels.Count; i++)
                data.pixels[i] = _selectedPaletteIndex;
            EditorUtility.SetDirty(data);
        }
        EditorGUILayout.EndHorizontal();

        if (EditorGUI.EndChangeCheck())
            EditorUtility.SetDirty(data);
    }
}
#endif
