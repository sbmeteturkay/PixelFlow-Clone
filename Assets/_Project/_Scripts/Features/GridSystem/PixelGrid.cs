using UnityEngine;

public class PixelGrid : MonoBehaviour
{
    private PixelArtData artData;
    
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Visuals")]
    [SerializeField] private PixelCell cellPrefab;
    [SerializeField] private float cellSize = 0.5f;

    [Header("Orbit")]
    [Tooltip("How far outside the grid boundary shooters orbit")]
    [SerializeField] public float orbitMargin = 1f;

    // ── Runtime ───────────────────────────────────────────────────────
    private PixelCell[,] _cells;
    private int _totalLiveCells;
    private int _destroyedCells;

    // Grid boundary on X/Z (used by shooter system)
    public Bounds GridBounds { get; private set; }
    public Bounds OrbitBounds { get; private set; }

    // ── Events ────────────────────────────────────────────────────────
    public System.Action OnLevelComplete;
    public System.Action<int, int> OnCellDestroyed;

    // ── Singleton ─────────────────────────────────────────────────────
    public static PixelGrid Instance { get; private set; }

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    // ═════════════════════════════════════════════════════════════════
    // Grid Construction
    // ═════════════════════════════════════════════════════════════════

    //todo: colorTolerance
    public void BuildGrid(PixelArtData data)
    {
        ClearGrid();
        artData = data;

        _cells = new PixelCell[data.columns, data.rows];
        _totalLiveCells = 0;
        _destroyedCells = 0;

        // Center the grid on this transform's position (X/Z plane, Y = 0)
        float originX = transform.position.x - (data.columns * cellSize) * 0.5f + cellSize * 0.5f;
        float originZ = transform.position.z - (data.rows * cellSize) * 0.5f + cellSize * 0.5f;
        float y = transform.position.y+cellSize/2;

        for (int row = 0; row < data.rows; row++)
        {
            for (int col = 0; col < data.columns; col++)
            {
                int colorIdx = data.GetPixelIndex(col, row);
                Color color = data.GetPixelColor(col, row);

                // Row 0 = top of the image → positive Z, rows go toward negative Z
                Vector3 pos = new Vector3(
                    originX + col * cellSize,
                    y,
                    originZ + (data.rows - 1 - row) * cellSize
                );

                PixelCell cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                cell.gameObject.name = $"Cell_{col}_{row}";
                cell.gameObject.transform.localScale = Vector3.one * cellSize;

                cell.Initialize(col, row, colorIdx, color);
                _cells[col, row] = cell;

                if (!cell.IsEmpty) _totalLiveCells++;
            }
        }

        RecalculateBounds();
    }

    private void ClearGrid()
    {
        foreach (Transform child in transform)
            Destroy(child.gameObject);
        _cells = null;
    }

    private void RecalculateBounds()
    {
        float width = artData.columns * cellSize;
        float depth = artData.rows * cellSize;
        Vector3 center = transform.position;

        // Y size is small since grid is flat (cubes have height = cellSize)
        GridBounds = new Bounds(center, new Vector3(width, cellSize, depth));
        OrbitBounds = new Bounds(center, new Vector3(width + orbitMargin * 2f, cellSize, depth + orbitMargin * 2f));
    }

    // ═════════════════════════════════════════════════════════════════
    // Queries (used by Shooter system)
    // ═════════════════════════════════════════════════════════════════

    /// <summary>
    /// Returns the outermost alive cell facing the shooter's position.
    /// The shooter calls this to know which cell is in front of it.
    /// </summary>
    public PixelCell GetFrontCell(GridEdge edge, int lineIndex)
    {
        if (_cells == null) return null;

        return edge switch
        {
            GridEdge.Top    => GetCell(lineIndex, GetTopmostAliveRow(lineIndex)),
            GridEdge.Bottom => GetCell(lineIndex, GetBottommostAliveRow(lineIndex)),
            GridEdge.Left   => GetCell(GetLeftmostAliveCol(lineIndex), lineIndex),
            GridEdge.Right  => GetCell(GetRightmostAliveCol(lineIndex), lineIndex),
            _ => null
        };
    }

    public PixelCell GetCell(int col, int row)
    {
        if (_cells == null) return null;
        if (col < 0 || col >= artData.columns || row < 0 || row >= artData.rows) return null;
        return _cells[col, row];
    }

    // ═════════════════════════════════════════════════════════════════
    // Coordinate Conversion
    // ═════════════════════════════════════════════════════════════════

    /// <summary>World X position → column index</summary>
    public int WorldXToCol(float worldX)
    {
        float originX = GridBounds.center.x - (artData.columns * cellSize) * 0.5f + cellSize * 0.5f;
        int col = Mathf.RoundToInt((worldX - originX) / cellSize);
        return Mathf.Clamp(col, 0, artData.columns - 1);
    }

    /// <summary>World Z position → row index</summary>
    public int WorldZToRow(float worldZ)
    {
        float originZ = GridBounds.center.z - (artData.rows * cellSize) * 0.5f + cellSize * 0.5f;
        int row = artData.rows - 1 - Mathf.RoundToInt((worldZ - originZ) / cellSize);
        return Mathf.Clamp(row, 0, artData.rows - 1);
    }

    /// <summary>Column + row → world position (center of that cell)</summary>
    public Vector3 CellToWorld(int col, int row)
    {
        float originX = GridBounds.center.x - (artData.columns * cellSize) * 0.5f + cellSize * 0.5f;
        float originZ = GridBounds.center.z - (artData.rows * cellSize) * 0.5f + cellSize * 0.5f;
        return new Vector3(
            originX + col * cellSize,
            transform.position.y,
            originZ + (artData.rows - 1 - row) * cellSize
        );
    }

    // ═════════════════════════════════════════════════════════════════
    // Outermost Alive Cell Helpers
    // ═════════════════════════════════════════════════════════════════

    private int GetTopmostAliveRow(int col)
    {
        for (int r = 0; r < artData.rows; r++)
            if (IsCellAlive(col, r)) return r;
        return -1;
    }

    private int GetBottommostAliveRow(int col)
    {
        for (int r = artData.rows - 1; r >= 0; r--)
            if (IsCellAlive(col, r)) return r;
        return -1;
    }

    private int GetLeftmostAliveCol(int row)
    {
        for (int c = 0; c < artData.columns; c++)
            if (IsCellAlive(c, row)) return c;
        return -1;
    }

    private int GetRightmostAliveCol(int row)
    {
        for (int c = artData.columns - 1; c >= 0; c--)
            if (IsCellAlive(c, row)) return c;
        return -1;
    }

    private bool IsCellAlive(int col, int row)
    {
        PixelCell c = GetCell(col, row);
        return c != null && c.IsAlive && !c.IsEmpty;
    }

    // ═════════════════════════════════════════════════════════════════
    // Hit Handling
    // ═════════════════════════════════════════════════════════════════

    public bool HandleHit(PixelCell cell, int shooterColorIndex)
    {
        if (cell == null) return false;

        bool destroyed = cell.TryHit(shooterColorIndex);
        if (destroyed)
        {
            _destroyedCells++;
            OnCellDestroyed?.Invoke(cell.Column, cell.Row);

            if (_destroyedCells >= _totalLiveCells)
                OnLevelComplete?.Invoke();
        }
        return destroyed;
    }

    // ═════════════════════════════════════════════════════════════════
    // Level Reset
    // ═════════════════════════════════════════════════════════════════

    public void ResetLevel()
    {
        if (_cells == null) return;
        _destroyedCells = 0;
        foreach (PixelCell cell in _cells)
            cell?.ResetCell();
    }

    // ═════════════════════════════════════════════════════════════════
    // Gizmos
    // ═════════════════════════════════════════════════════════════════

    private void OnDrawGizmos()
    {
        float width = (artData != null ? artData.columns : 30) * cellSize;
        float depth = (artData != null ? artData.rows : 25) * cellSize;

        Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
        Gizmos.DrawWireCube(transform.position, new Vector3(width, cellSize, depth));

        Gizmos.color = new Color(1f, 0.8f, 0.2f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(width + orbitMargin * 2f, cellSize, depth + orbitMargin * 2f));
    }
}

public enum GridEdge { Top, Bottom, Left, Right }
