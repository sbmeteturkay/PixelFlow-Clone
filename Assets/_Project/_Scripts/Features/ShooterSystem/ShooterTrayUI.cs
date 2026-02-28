using UnityEngine;

namespace Game.Feature.Shooting.UI
{
    public class ShooterTrayUI:MonoBehaviour
    {
        [SerializeField]TMPro.TMP_Text _text;
        private void Start()
        {
            ShooterManager.Instance.OnTrayListCountChanged += OnTrayListChanged;
        }

        private void OnDestroy()
        {
            ShooterManager.Instance.OnTrayListCountChanged -= OnTrayListChanged;
        }

        private void OnTrayListChanged(int count)
        {
            _text.text=count+"/"+ShooterManager.SlotCapacity;
        }
    }
}