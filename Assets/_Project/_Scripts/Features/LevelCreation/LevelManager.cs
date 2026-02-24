using UnityEngine;
using UnityEngine.Splines;
using System.Collections.Generic;

public class LevelManager : MonoBehaviour
{
    // ── Inspector ─────────────────────────────────────────────────────
    [Header("Level")]
    [SerializeField] private LevelData levelData;

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
    private bool _levelActive = false;

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
        if (levelData != null)
            StartLevel(levelData);
    }

    // ═════════════════════════════════════════════════════════════════
    // Level Lifecycle
    // ═════════════════════════════════════════════════════════════════

    public void StartLevel(LevelData data)
    {
        levelData = data;
        _levelActive = true;

        pixelGrid.BuildGrid(data.pixelArt, data.colorTolerance);
        pixelGrid.OnLevelComplete += HandleWin;
        ShooterManager.Instance.OnLose += HandleLose;

        SpawnShooters(data.shooters);
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

        for (int i = 0; i < shooterDataList.Count; i++)
        {
            Vector3 spawnPos = GetWaitingPosition(i);

            Shooter shooter = ShooterPool.Instance.Get(shooterDataList[i], spawnPos);
            shooter.SetSpline(shooterSpline);

            _waitingShooters.Add(shooter);
        }
    }

    private Vector3 GetWaitingPosition(int index)
    {
        if (waitingAreaRoot == null)
            return new Vector3(index * waitingSpacing, 1f, -8f);

        return waitingAreaRoot.position +
               waitingAreaRoot.right * (index * waitingSpacing)+Vector3.up;
    }

    // ═════════════════════════════════════════════════════════════════
    // Win / Lose
    // ═════════════════════════════════════════════════════════════════

    private void HandleWin()
    {
        if (!_levelActive) return;

        EndLevel();
        Debug.Log("[LevelManager] Level complete!");
    }

    private void HandleLose()
    {
        if (!_levelActive) return;

        EndLevel();
        Debug.Log("[LevelManager] Game over — slot area full!");
    }

    // ═════════════════════════════════════════════════════════════════
    // Retry
    // ═════════════════════════════════════════════════════════════════

    public void RetryLevel()
    {
        EndLevel();

        foreach (Shooter s in _waitingShooters)
            if (s != null && s.gameObject.activeSelf)
                ShooterPool.Instance.Release(s);

        _waitingShooters.Clear();
        pixelGrid.ResetLevel();

        StartLevel(levelData);
    }
}