using Game.Feature.Level;
using UnityEngine;
using UnityEngine.UI;

public class NoLivesPanel : BasePanel
{
    [SerializeField] private Button watchAdButton;
    [SerializeField] private Button buyLivesButton;
    protected override void Subscribe()=>LevelManager.Instance.OnNoLives+= OnNoLives;

    protected override void Unsubscribe()=>LevelManager.Instance.OnNoLives -= OnNoLives;

    private void OnNoLives()
    {
        Show();
    }
}