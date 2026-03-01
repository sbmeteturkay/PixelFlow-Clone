using System.Collections.Generic;
using UnityEngine;

namespace Game.Feature.Level
{
    public class PixelGrid : MonoBehaviour
    {
        private PixelArtData artData;

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Visuals")]
        [SerializeField] private PixelCell cellPrefab;
        [SerializeField] private float cellSize = 0.5f;
        [SerializeField] private float colorLerpFactor = 0.6f;
        [Header("Shooter Edge Boundaries")]
        public float topZ;
        public float bottomZ;
        public float rightX;
        public float leftX;

        // ── Runtime ───────────────────────────────────────────────────────
        private PixelCell[,] _cells;
        private int _totalLiveCells;
        private int _destroyedCells;

        public Bounds GridBounds { get; private set; }

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

        public void BuildGrid(PixelArtData data, float dataColorTolerance)
        {
            ClearGrid();
            artData = data;

            _cells = new PixelCell[data.columns, data.rows];
            _totalLiveCells = 0;
            _destroyedCells = 0;

            
            var (indexToCluster, clusterColors) = LevelCreationExtensions.BuildColorClusters(data, dataColorTolerance);
            float originX = transform.position.x - (data.columns * cellSize) * 0.5f + cellSize * 0.5f;
            float originZ = transform.position.z - (data.rows * cellSize) * 0.5f + cellSize * 0.5f;
            float y = transform.position.y + cellSize / 2;

            for (int row = 0; row < data.rows; row++)
            {
                for (int col = 0; col < data.columns; col++)
                {
                    int paletteIndex = data.GetPixelIndex(col, row);
                    if (paletteIndex < 0) continue;

                    Color color = data.palette[paletteIndex].color;

                    Vector3 pos = new Vector3(
                        originX + col * cellSize,
                        y,
                        originZ + (data.rows - 1 - row) * cellSize
                    );

                    PixelCell cell = Instantiate(cellPrefab, pos, Quaternion.identity, transform);
                    cell.gameObject.name = $"Cell_{col}_{row}";
                    cell.transform.localScale = Vector3.one * cellSize;
                    
                    int clusterIndex = indexToCluster[paletteIndex];
                    Color average = clusterColors[clusterIndex];
                    Color original = data.palette[paletteIndex].color;
                    Color finalColor = Color.Lerp(original, average, colorLerpFactor);

                    cell.Initialize(col, row, clusterIndex, finalColor);

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
            GridBounds = new Bounds(transform.position, new Vector3(width, cellSize, depth));
        }

        // ═════════════════════════════════════════════════════════════════
        // Edge Detection (used by Shooter)
        // ═════════════════════════════════════════════════════════════════

        public GridEdge GetEdgeForPosition(Vector3 worldPos)
        {
            bool betweenLeftRight = worldPos.x > leftX && worldPos.x < rightX;
            bool betweenTopBottom = worldPos.z > bottomZ && worldPos.z < topZ;

            if (betweenLeftRight && betweenTopBottom)
            {
                // Grid içinde — olmamalı ama safety
                return GridEdge.Corner;
            }

            if (betweenLeftRight && !betweenTopBottom)
            {
                // Top veya Bottom, center'a göre karar ver
                return worldPos.z > transform.position.z ? GridEdge.Top : GridEdge.Bottom;
            }

            if (betweenTopBottom && !betweenLeftRight)
            {
                // Left veya Right, center'a göre karar ver
                return worldPos.x > transform.position.x ? GridEdge.Right : GridEdge.Left;
            }

            // Her iki koşul da sağlanmıyor → Corner
            return GridEdge.Corner;
        }
        // ═════════════════════════════════════════════════════════════════
        // Queries
        // ═════════════════════════════════════════════════════════════════

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

        public int WorldXToCol(float worldX)
        {
            float originX = GridBounds.center.x - (artData.columns * cellSize) * 0.5f + cellSize * 0.5f;
            int col = Mathf.RoundToInt((worldX - originX) / cellSize);
            return Mathf.Clamp(col, 0, artData.columns - 1);
        }

        public int WorldZToRow(float worldZ)
        {
            float originZ = GridBounds.center.z - (artData.rows * cellSize) * 0.5f + cellSize * 0.5f;
            int row = artData.rows - 1 - Mathf.RoundToInt((worldZ - originZ) / cellSize);
            return Mathf.Clamp(row, 0, artData.rows - 1);
        }

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
            float width   = (artData != null ? artData.columns : 30) * cellSize;
            float depth   = (artData != null ? artData.rows    : 25) * cellSize;
            float y       = transform.position.y;
            float extent  = Mathf.Max(width, depth) * 1.5f;

            // Grid bounds
            Gizmos.color = new Color(0.3f, 0.8f, 1f, 0.4f);
            Gizmos.DrawWireCube(transform.position, new Vector3(width, cellSize, depth));

            // Top
            Gizmos.color = new Color(0f, 1f, 0f, 0.8f);
            Gizmos.DrawLine(new Vector3(-extent, y, topZ), new Vector3(extent, y, topZ));

            // Bottom
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.8f);
            Gizmos.DrawLine(new Vector3(-extent, y, bottomZ), new Vector3(extent, y, bottomZ));

            // Right
            Gizmos.color = new Color(0.2f, 0.5f, 1f, 0.8f);
            Gizmos.DrawLine(new Vector3(rightX, y, -extent), new Vector3(rightX, y, extent));

            // Left
            Gizmos.color = new Color(1f, 0.6f, 0f, 0.8f);
            Gizmos.DrawLine(new Vector3(leftX, y, -extent), new Vector3(leftX, y, extent));

            // Corners
            Gizmos.color = Color.white;
            Gizmos.DrawSphere(new Vector3(leftX,  y, topZ),    0.08f);
            Gizmos.DrawSphere(new Vector3(rightX, y, topZ),    0.08f);
            Gizmos.DrawSphere(new Vector3(leftX,  y, bottomZ), 0.08f);
            Gizmos.DrawSphere(new Vector3(rightX, y, bottomZ), 0.08f);

#if UNITY_EDITOR
            UnityEditor.Handles.Label(new Vector3(extent, y, topZ),    " Top");
            UnityEditor.Handles.Label(new Vector3(extent, y, bottomZ), " Bottom");
            UnityEditor.Handles.Label(new Vector3(rightX, y,  extent), " Right");
            UnityEditor.Handles.Label(new Vector3(leftX,  y,  extent), " Left");
#endif
        }
    }

    public enum GridEdge { Top, Bottom, Left, Right,Corner }
}