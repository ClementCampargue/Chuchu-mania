using TMPro;
using UnityEngine;

public class SC_timer : MonoBehaviour
{
    public TextMeshPro timerText;
    public float base_time = 0f;
    public static SC_timer instance;

    private void Awake()
    {
        instance = this;
    }
    void Update()
    {
        base_time -= Time.deltaTime;

        int seconds = Mathf.FloorToInt(base_time);

        seconds = Mathf.Clamp(seconds, 0, 999);

        timerText.text = seconds.ToString("D3");
    }
}
