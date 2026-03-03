using System;
using Game.Core.Life;
using TMPro;
using UnityEngine;

namespace Game.Feature.UI
{
    public class LivesUI:MonoBehaviour
    {
        [SerializeField] private TMP_Text remainingLivesText;
        [SerializeField] private TMP_Text countDown;

        private void Awake()
        {
            LivesSystem.Instance.OnLivesChanged += OnLivesChanged;
            LivesSystem.Instance.OnTimerTick += OnTimerTick;
            OnLivesChanged(LivesSystem.Instance.CurrentLives);
        }
        
        private void OnDestroy()
        {
            LivesSystem.Instance.OnLivesChanged -= OnLivesChanged;
            LivesSystem.Instance.OnTimerTick -= OnTimerTick;
        }
        
        private void OnTimerTick(float seconds)
        {
            int min = Mathf.FloorToInt(seconds / 60);
            int sec = Mathf.FloorToInt(seconds % 60);
            countDown.text = $"{min:00}:{sec:00}";
        }

        private void OnLivesChanged(int lives)
        {
            remainingLivesText.text = lives.ToString();
            if (lives==LivesSystem.MaxLives)
            {
                countDown.text = "MAX";
            }
        }
    }
}