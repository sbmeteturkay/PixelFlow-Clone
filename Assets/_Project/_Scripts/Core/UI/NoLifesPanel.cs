using System;
using Game.Core.Life;
using Game.Feature.Level;
using UnityEngine;
using UnityEngine.UI;

public class NoLivesPanel : BasePanel
{
    [SerializeField] private Button watchAdButton;
    [SerializeField] private BuyButton buyLivesButton;

    protected override void Subscribe()=>LevelManager.Instance.OnNoLives+= OnNoLives;

    protected override void Unsubscribe()=>LevelManager.Instance.OnNoLives -= OnNoLives;


    protected override void Start()
    {
        base.Start();
        buyLivesButton.OnBought += OnLifeBought;
    }

    private void OnLifeBought()
    {
        LivesSystem.Instance.FillLives();
        Hide();
    }

    private void OnNoLives()
    {
        Show();
    }
}