using UnityEngine;
using System;

public class GoldSystem : MonoBehaviour
{
    // ── Constants ─────────────────────────────────────────────────────


    private const string KeyGold = "gold";

    // ── Singleton ─────────────────────────────────────────────────────

    public static GoldSystem Instance { get; private set; }

    // ── Properties ────────────────────────────────────────────────────

    public int CurrentGold { get; private set; }

    // ── Events ────────────────────────────────────────────────────────

    public event Action<int> OnGoldChanged;

    // ─────────────────────────────────────────────────────────────────

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;

        Load();
    }

    // ═════════════════════════════════════════════════════════════════
    // Public API
    // ═════════════════════════════════════════════════════════════════

    /// <summary>Called on level complete without ad.</summary>
    public void RewardLevelComplete(int amount)
    {
        AddGold(amount);
    }

    public bool TrySpend(int amount)
    {

        if (amount>CurrentGold)
            return false;
        CurrentGold -= amount;
        Save();
        OnGoldChanged?.Invoke(CurrentGold);
        return true;
    }
    // ═════════════════════════════════════════════════════════════════
    // Internal
    // ═════════════════════════════════════════════════════════════════

    private void AddGold(int amount)
    {
        CurrentGold += amount;
        Save();
        OnGoldChanged?.Invoke(CurrentGold);
    }

    // ═════════════════════════════════════════════════════════════════
    // Persistence
    // ═════════════════════════════════════════════════════════════════

    private void Save()
    {
        PlayerPrefs.SetInt(KeyGold, CurrentGold);
        PlayerPrefs.Save();
    }

    private void Load()
    {
        CurrentGold = PlayerPrefs.GetInt(KeyGold, 1000);
    }
}