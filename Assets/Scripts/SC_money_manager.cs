using UnityEngine;

public class SC_money_manager : MonoBehaviour
{
    public static SC_money_manager instance;
    public int money;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        money = PlayerPrefs.GetInt("Money");
    }


    private void OnDestroy()
    {
        PlayerPrefs.SetInt("Money", money);

    }

    private void OnApplicationQuit()
    {
        PlayerPrefs.SetInt("Money", money);

    }
}
