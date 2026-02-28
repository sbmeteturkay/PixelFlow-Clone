using System;
using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;
using Game.Feature.Shooting;

namespace Game.Feature.Level
{
    public class LevelManager : MonoBehaviour
    {
        // ── Events ─────────────────────────────────────────────────────
        public Action OnWin;
        public Action OnLose;
        public Action<int> OnLevelStart;

        // ── Inspector ─────────────────────────────────────────────────────
        [Header("Level")]
        [SerializeField] private List<LevelData> levelData;

        [SerializeField] private int levelIndex;

        [Header("References")]
        [SerializeField] private PixelGrid pixelGrid;

        [SerializeField] private SplineContainer shooterSpline;

        [Header("Waiting Area")]
        [SerializeField] private Transform waitingAreaRoot;

        [SerializeField] private float waitingSpacing = 1.5f;

        // ── Singleton ─────────────────────────────────────────────────────
        public static LevelManager Instance { get; private set; }

        // ── Runtime ───────────────────────────────────────────────────────
        private List<Shooter> _waitingShooters = new();
        private List<Shooter> _shooters = new();
        private Queue<Shooter>[] _columnQueues;
        private bool _levelActive = false;

        private const int COLUMN_COUNT = 3;

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        private void Start()
        {
            LevelSelection.OnLevelSelected += OnLevelSelected;
        }

        private void OnDestroy()
        {
            LevelSelection.OnLevelSelected -= OnLevelSelected;
        }

        private void OnLevelSelected(int level)
        {
            levelIndex = level-1;
            if (levelData != null)
                StartLevel(levelData[levelIndex% levelData.Count]);
        }


        // ═════════════════════════════════════════════════════════════════
        // Level Lifecycle
        // ═════════════════════════════════════════════════════════════════

        public void StartLevel(LevelData data)
        {
            _levelActive = true;

            pixelGrid.BuildGrid(data.pixelArt, data.colorTolerance);
            pixelGrid.OnLevelComplete += HandleWin;
            ShooterManager.Instance.OnLose += HandleLose;

            SpawnShooters(data.shooters);
            OnLevelStart(levelIndex+1);
        }

        private void EndLevel()
        {
            _levelActive = false;
            pixelGrid.OnLevelComplete -= HandleWin;
            ShooterManager.Instance.OnLose -= HandleLose;
        }

        // ═════════════════════════════════════════════════════════════════
        // Shooter Spawning
        // ═════════════════════════════════════════════════════════════════

        private void SpawnShooters(List<ShooterData> shooterDataList)
        {
            _waitingShooters.Clear();

            _columnQueues = new Queue<Shooter>[COLUMN_COUNT];
            for (int i = 0; i < COLUMN_COUNT; i++)
                _columnQueues[i] = new Queue<Shooter>();

            for (int i = 0; i < shooterDataList.Count; i++)
            {
                int col = i % COLUMN_COUNT;
                int row = i / COLUMN_COUNT;

                Vector3 spawnPos = GetWaitingPosition(col, row);
                Shooter shooter = ShooterPool.Instance.Get(shooterDataList[i], spawnPos);
                shooter.SetSpline(shooterSpline);
                shooter.SetInteractable(row == 0);

                _shooters.Add(shooter);
                _columnQueues[col].Enqueue(shooter);
                _waitingShooters.Add(shooter);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // Called by Shooter when it leaves the waiting area
        // ═════════════════════════════════════════════════════════════════

        public void OnShooterLeftWaiting(Shooter shooter)
        {
            // Find which column this shooter belongs to
            int col = -1;
            for (int i = 0; i < COLUMN_COUNT; i++)
            {
                if (_columnQueues[i].Count > 0 && _columnQueues[i].Peek() == shooter)
                {
                    col = i;
                    break;
                }
            }

            if (col == -1) return;

            _columnQueues[col].Dequeue();
            _waitingShooters.Remove(shooter);

            // Shift remaining shooters in this column forward and promote front
            int rowIndex = 0;
            foreach (Shooter s in _columnQueues[col])
            {
                s.MoveTo(GetWaitingPosition(col, rowIndex));
                if (rowIndex == 0) s.SetInteractable(true);
                rowIndex++;
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // Position Helper
        // ═════════════════════════════════════════════════════════════════

        private Vector3 GetWaitingPosition(int col, int row)
        {
            float xOffset = (col - 1) * waitingSpacing;
            float zOffset = -row * waitingSpacing;

            return waitingAreaRoot.position
                   + Vector3.right * xOffset
                   + Vector3.forward * zOffset
                   + Vector3.up;
        }

        // ═════════════════════════════════════════════════════════════════
        // Win / Lose
        // ═════════════════════════════════════════════════════════════════

        private void HandleWin()
        {
            if (!_levelActive) return;
            EndLevel();
            PlayerPrefs.SetInt("CurrentLevel", levelIndex + 2);
            PlayerPrefs.Save();
            Debug.Log(PlayerPrefs.GetInt("CurrentLevel"));
            OnWin?.Invoke();
        }

        private void HandleLose()
        {
            if (!_levelActive) return;
            EndLevel();
            OnLose?.Invoke();
        }

        // ═════════════════════════════════════════════════════════════════
        // Retry
        // ═════════════════════════════════════════════════════════════════

        public void RetryLevel()
        {
            EndLevel();

            foreach (Shooter s in _shooters)
                s.ResetShooter();

            _waitingShooters.Clear();
            ShooterManager.Instance.ClearShooters();
            pixelGrid.ResetLevel();
            StartLevel(levelData[levelIndex]);
        }

        public void NextLevel()
        {
            levelIndex = (levelIndex + 1) % levelData.Count;
            RetryLevel();
        }
    }
}