using System.Collections;
using TMPro;
using UnityEngine;

public class SC_money_shop : MonoBehaviour
{
    [SerializeField] private TextMeshPro moneyText;

    [SerializeField] private Color zeroColor = Color.gray;
    [SerializeField] private Color moneyColor = Color.yellow;

    [SerializeField] private int minimumMoney = 0;

    [SerializeField] private float moneyScrollSpeed = 5f;

    public int money = 500;
    public static SC_money_shop instance;

    private Coroutine moneyAnimation;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        money = SC_money_manager.instance.money;
        UpdateMoney();
    }

    public void UpdateMoney()
    {
        money = SC_money_manager.instance.money;

        DisplayMoney(money);
    }

    private void DisplayMoney(int amount)
    {
        int zeroCount = Mathf.Max(0, 6 - amount.ToString().Length);

        string zeros = new string('0', zeroCount);
        string actualMoney = amount.ToString();

        moneyText.text =
            $"<color=#{ColorUtility.ToHtmlStringRGB(zeroColor)}>{zeros}</color>" +
            $"<color=#{ColorUtility.ToHtmlStringRGB(moneyColor)}>{actualMoney}</color>";
    }

    public void AddMoney(int amount)
    {
        money += amount;

        if (moneyAnimation != null)
            StopCoroutine(moneyAnimation);

        moneyAnimation = StartCoroutine(AnimateMoney(money - amount, money));
    }

    public void RemoveMoney(int amount)
    {
        int oldMoney = money;

        money -= amount;
        money = Mathf.Max(minimumMoney, money);

        if (moneyAnimation != null)
            StopCoroutine(moneyAnimation);

        moneyAnimation = StartCoroutine(AnimateMoney(oldMoney, money));
    }

    public void Buy(int amount)
    {
        if (money - amount < minimumMoney)
            return;

        int oldMoney = money;

        money -= amount;
        PlayerPrefs.SetInt("Money", money);

        if (moneyAnimation != null)
            StopCoroutine(moneyAnimation);

        moneyAnimation = StartCoroutine(AnimateMoney(oldMoney, money));
    }

    private IEnumerator AnimateMoney(int startValue, int targetValue)
    {
        float currentValue = startValue;

        while (Mathf.Abs(currentValue - targetValue) > 0.1f)
        {
            currentValue = Mathf.MoveTowards(
                currentValue,
                targetValue,
                moneyScrollSpeed * Time.deltaTime * Mathf.Abs(startValue - targetValue)
            );

            DisplayMoney(Mathf.RoundToInt(currentValue));

            yield return null;
        }

        DisplayMoney(targetValue);
        moneyAnimation = null;
    }
}