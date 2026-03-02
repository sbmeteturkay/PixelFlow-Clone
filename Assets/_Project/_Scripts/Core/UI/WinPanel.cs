using System;
using Game.Feature.Level;
using UnityEngine;
using UnityEngine.UI;

public class WinPanel : BasePanel
{
    [SerializeField] private Button nextLevelButton;
    [SerializeField] private Button doubleRewardButton;


    protected override void Subscribe()   => LevelManager.Instance.OnWin += Show;
    protected override void Unsubscribe() => LevelManager.Instance.OnWin -= Show;
    protected override void Start()
    {
        base.Start();
        nextLevelButton.onClick.AddListener(()=>
        {
            LevelManager.Instance.NextLevel();
            GoldSystem.Instance.RewardLevelComplete();
        });
        doubleRewardButton.onClick.AddListener(() =>
        {
            LevelManager.Instance.NextLevel();
            GoldSystem.Instance.RewardLevelCompleteWithAd();
            Hide();
        });
    }
}