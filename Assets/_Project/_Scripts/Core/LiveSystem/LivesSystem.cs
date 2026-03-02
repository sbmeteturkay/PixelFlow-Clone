using UnityEngine;
using System;

namespace Game.Core.Life
{
    [DefaultExecutionOrder(-1)]
    public class LivesSystem : MonoBehaviour
    {
        // ── Constants ─────────────────────────────────────────────────────

        public const int MaxLives = 5;
        public const float RegenIntervalSeconds = 30 * 60f;

        private const string KeyLives = "lives";
        private const string KeyLastRegen = "last_regen";

        // ── Singleton ─────────────────────────────────────────────────────

        public static LivesSystem Instance { get; private set; }

        // ── Properties ────────────────────────────────────────────────────

        public int CurrentLives { get; private set; }
        public float SecondsUntilNextLife { get; private set; }
        public bool HasLives => CurrentLives > 0;

        // ── Events ────────────────────────────────────────────────────────

        public event Action<int> OnLivesChanged;
        public event Action<float> OnTimerTick;

        // ── Runtime ───────────────────────────────────────────────────────

        private float _tickTimer;

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            Load();
            ProcessOfflineRegen();
        }

        private void Start()
        {
            OnLivesChanged?.Invoke(CurrentLives);
        }

        private void Update()
        {
            if (CurrentLives >= MaxLives) return;

            _tickTimer += Time.deltaTime;
            if (_tickTimer < 1f) return;

            _tickTimer = 0f;
            OnTick();
        }

        // ═════════════════════════════════════════════════════════════════
        // Public API
        // ═════════════════════════════════════════════════════════════════

        public bool SpendLife()
        {
            if (CurrentLives <= 0) return false;

            CurrentLives--;

            if (CurrentLives < MaxLives)
                SetLastRegenTime(DateTime.UtcNow);

            Save();
            OnLivesChanged?.Invoke(CurrentLives);
            return true;
        }

        // ═════════════════════════════════════════════════════════════════
        // Regen
        // ═════════════════════════════════════════════════════════════════

        private void ProcessOfflineRegen()
        {
            if (CurrentLives >= MaxLives) return;

            DateTime lastRegen = GetLastRegenTime();
            float elapsed = (float)(DateTime.UtcNow - lastRegen).TotalSeconds;
            int gained = Mathf.FloorToInt(elapsed / RegenIntervalSeconds);

            if (gained <= 0) return;

            CurrentLives = Mathf.Min(MaxLives, CurrentLives + gained);
            SetLastRegenTime(lastRegen.AddSeconds(gained * RegenIntervalSeconds));
            Save();
            OnLivesChanged?.Invoke(CurrentLives);
        }

        private void OnTick()
        {
            if (CurrentLives >= MaxLives)
            {
                SecondsUntilNextLife = 0f;
                OnTimerTick?.Invoke(0f);
                return;
            }

            DateTime lastRegen = GetLastRegenTime();
            float elapsed = (float)(DateTime.UtcNow - lastRegen).TotalSeconds;
            SecondsUntilNextLife = RegenIntervalSeconds - (elapsed % RegenIntervalSeconds);
            OnTimerTick?.Invoke(SecondsUntilNextLife);

            int gained = Mathf.FloorToInt(elapsed / RegenIntervalSeconds);
            if (gained > 0)
            {
                CurrentLives = Mathf.Min(MaxLives, CurrentLives + gained);
                SetLastRegenTime(lastRegen.AddSeconds(gained * RegenIntervalSeconds));
                Save();
                OnLivesChanged?.Invoke(CurrentLives);
            }
        }

        // ═════════════════════════════════════════════════════════════════
        // Persistence
        // ═════════════════════════════════════════════════════════════════

        private void Save()
        {
            PlayerPrefs.SetInt(KeyLives, CurrentLives);
            PlayerPrefs.Save();
        }

        private void Load()
        {
            CurrentLives = PlayerPrefs.GetInt(KeyLives, MaxLives);
        }

        private DateTime GetLastRegenTime()
        {
            string raw = PlayerPrefs.GetString(KeyLastRegen, string.Empty);
            if (string.IsNullOrEmpty(raw)) return DateTime.UtcNow;
            if (DateTime.TryParse(raw, null, System.Globalization.DateTimeStyles.RoundtripKind, out DateTime result)) 
                return result;
            return DateTime.UtcNow;
        }

        private void SetLastRegenTime(DateTime time)
        {
            PlayerPrefs.SetString(KeyLastRegen, time.ToUniversalTime().ToString("O"));
            PlayerPrefs.Save();
        }
    }
}