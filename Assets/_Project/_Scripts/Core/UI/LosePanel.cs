using Game.Feature.Level;
using UnityEngine;
using UnityEngine.UI;

public class LosePanel : BasePanel
{
    [SerializeField] Button restartButton;
    protected override void Subscribe()   => LevelManager.Instance.OnLose += Show;
    protected override void Unsubscribe() => LevelManager.Instance.OnLose -= Show;

    protected override void Start()
    {
        base.Start();
        restartButton.onClick.AddListener(()=>
        {
            LevelManager.Instance.RetryLevel();
            Hide();
        });
    }
}