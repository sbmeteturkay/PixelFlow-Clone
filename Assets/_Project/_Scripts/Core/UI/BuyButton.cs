using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BuyButton : MonoBehaviour
{
    public Action OnBought;
    [SerializeField] private int value;
    [SerializeField] private Button targetButton;
    [SerializeField] private TMP_Text valueText;
    private bool canBeBought;

    private void Start()
    {
        valueText.text = value.ToString();
        targetButton.onClick.AddListener(TryBuy);
        OnGoldChanged(GoldSystem.Instance.CurrentGold);
        GoldSystem.Instance.OnGoldChanged += OnGoldChanged;
    }

    private void OnDestroy()
    {
        GoldSystem.Instance.OnGoldChanged -= OnGoldChanged;
    }

    private void OnGoldChanged(int obj)
    {
        canBeBought = GoldSystem.Instance.CurrentGold >= value;
        targetButton.image.color = canBeBought ? Color.chartreuse : Color.gray5;
    }

    private void TryBuy()
    {
        if (canBeBought)
        {
            if(GoldSystem.Instance.TrySpend(value))
                OnBought.Invoke();
        }
    }

}