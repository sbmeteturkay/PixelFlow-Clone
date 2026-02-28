using Game.Feature.Level;
using UnityEngine;
using UnityEngine.UI;

public class WinPanel : BasePanel
{
    [SerializeField] Button nextLevelButton;

    protected override void Subscribe()   => LevelManager.Instance.OnWin += Show;
    protected override void Unsubscribe() => LevelManager.Instance.OnWin -= Show;
    protected override void Start()
    {
        base.Start();
        nextLevelButton.onClick.AddListener(()=>
        {
            LevelManager.Instance.NextLevel();
            Hide();
        });
    }
}