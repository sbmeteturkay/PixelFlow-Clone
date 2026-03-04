using System;
using System.Collections.Generic;
using System.Linq;
using Game.Core;
using Game.Core.Life;
using Game.Feature.Shooting;
using UnityEngine;
using UnityEngine.Splines;

namespace Game.Feature.Level
{
    public class LevelManager : MonoBehaviour
    {
        private const int COLUMN_COUNT = 3;

        // ── Inspector ─────────────────────────────────────────────────────

        [SerializeField] private int levelIndex;

        [Header("References")]
        [SerializeField] private PixelGrid pixelGrid;

        [SerializeField] private SplineContainer shooterSpline;

        [Header("Waiting Area")]
        [SerializeField] private Transform waitingAreaRoot;

        [SerializeField] private float waitingSpacing = 1.5f;
        private readonly List<Shooter> _shooters = new();
        private Queue<Shooter>[] _columnQueues;
        private bool _levelActive;

        // ── Runtime ───────────────────────────────────────────────────────
        private List<LevelData> _levels = new();
        public Action<int> OnLevelStart;
        public Action OnLose;

        public Action OnNoLives;

        // ── Events ─────────────────────────────────────────────────────
        public Action OnWin;

        // ── Singleton ─────────────────────────────────────────────────────
        public static LevelManager Instance { get; private set; }

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
            LoadLevels();
        }

        private void OnDestroy()
        {
            LevelSelection.OnLevelSelected -= OnLevelSelected;
        }

        private void LoadLevels()
        {
            _levels = Resources.LoadAll<LevelData>("LevelData").ToList();
            _levels = _levels.OrderBy(l => l.name).ToList();
        }

        private void OnLevelSelected(int level)
        {
            if (!LivesSystem.Instance.HasLives)
            {
                OnNoLives?.Invoke();
                return;
            }

            levelIndex = level - 1;
            if (_levels != null)
                StartLevel(_levels[levelIndex % _levels.Count]);
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
            OnLevelStart(levelIndex + 1);
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
            _columnQueues = new Queue<Shooter>[COLUMN_COUNT];
            for (int i = 0; i < COLUMN_COUNT; i++)
            {
                _columnQueues[i] = new();
            }


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
                   + Vector3.up * .25f;
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
            OnWin?.Invoke();
            AudioManager.Instance.PlayWin();
        }

        private void HandleLose()
        {
            if (!_levelActive) return;
            EndLevel();
            LivesSystem.Instance.SpendLife();
            OnLose?.Invoke();
            AudioManager.Instance.PlayLose();
        }

        // ═════════════════════════════════════════════════════════════════
        // Retry
        // ═════════════════════════════════════════════════════════════════

        public void RetryLevel()
        {
            EndLevel();

            foreach (Shooter s in _shooters)
            {
                s.ResetShooter();
            }

            ShooterManager.Instance.ClearShooters();

            if (!LivesSystem.Instance.HasLives)
            {
                OnNoLives?.Invoke();
                return;
            }

            pixelGrid.ResetLevel();
            StartLevel(_levels[levelIndex % _levels.Count]);
        }

        public void NextLevel()
        {
            levelIndex = (levelIndex + 1) % _levels.Count;
            RetryLevel();
        }
    }
}