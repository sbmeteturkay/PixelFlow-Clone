    using System;
    using System.Collections.Generic;
    using Game.Feature.Level;
    using PrimeTween;
    using TMPro;
    using UnityEngine;
    using UnityEngine.UI;

    public class LevelSelection : MonoBehaviour
    {
        public static Action<int> OnLevelSelected;
        
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private List<TMP_Text> levelButtonsText = new List<TMP_Text>();
        [SerializeField] private Button playButton;

        // ── Singleton ─────────────────────────────────────────────────────
        public static LevelSelection Instance { get; private set; }

        // ─────────────────────────────────────────────────────────────────

        private void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }
        
        protected void Start()
        {
            UpdateLevelSelectionButtons();
            LevelManager.Instance.OnLevelStart += OnLevelStart;
            LevelManager.Instance.OnNoLives += OnNoLives;
        }



        private void OnDestroy()
        {
            LevelManager.Instance.OnLevelStart -= OnLevelStart;
            LevelManager.Instance.OnNoLives -= OnNoLives;

        }
        private void OnNoLives()
        {
            Show();
        }
        private void OnLevelStart(int obj)
        {
            Hide();
        }

        private void UpdateLevelSelectionButtons()
        {
            int currentLevel = PlayerPrefs.GetInt("CurrentLevel", 1);

            playButton.onClick.RemoveAllListeners();
            playButton.onClick.AddListener(() =>
            {
                OnLevelSelected?.Invoke(currentLevel);
            });

            for (int i = 0; i < levelButtonsText.Count; i++)
            {
                TMP_Text levelButton = levelButtonsText[i];
                levelButton.text = (currentLevel+i).ToString();
            }
        }

        public void Show()
        {
            Tween.Alpha(canvasGroup, 1f, 0.3f);
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;
        }

        private void Hide()
        {
            Tween.Alpha(canvasGroup, 0f, 0.3f);
            canvasGroup.blocksRaycasts = false;
            canvasGroup.interactable = false;
        } 
    }
