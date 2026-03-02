using TMPro;
using UnityEngine;

public class GoldAmountUI : MonoBehaviour
{
    [SerializeField] private TMP_Text goldText;

    private void Start()
    {
        GoldSystem.Instance.OnGoldChanged += SetGoldText;
        SetGoldText(GoldSystem.Instance.CurrentGold);
    }

    private void OnDestroy()
    {
        GoldSystem.Instance.OnGoldChanged -= SetGoldText;
    }
    private void SetGoldText(int amount)
    {
        goldText.text = amount.ToString();
    }
}