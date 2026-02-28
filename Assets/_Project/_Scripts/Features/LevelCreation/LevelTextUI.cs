using UnityEngine;

namespace Game.Feature.Level.UI
{
    public class LevelTextUI:MonoBehaviour
    {
        [SerializeField]TMPro.TMP_Text _text;
        private void Start()
        {
            LevelManager.Instance.OnLevelStart += OnLevelChange;
        }
        private void OnDestroy()
        {
            LevelManager.Instance.OnLevelStart -= OnLevelChange;
        }

        private void OnLevelChange(int level)
        {
            _text.text="Level "+level;
        }
    }
}