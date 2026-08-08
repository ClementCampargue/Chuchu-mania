using TMPro;
using UnityEngine;

public class SC_timer : MonoBehaviour
{
    public TextMeshPro timerText;
    private float base_time_ = 0f;
    public float base_time = 0f;
    public static SC_timer instance;
    private bool decrease;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        base_time_ = base_time;
    }
    void Update()
    {   
        if (!decrease) return;
        base_time -= Time.deltaTime;

        int seconds = Mathf.FloorToInt(base_time);

        seconds = Mathf.Clamp(seconds, 0, 999);

        timerText.text = seconds.ToString("D3");
    }

    public void reset_timer()
    {
        base_time = base_time_;
        timerText.text = base_time.ToString();
    }

    public void resume()
    {
        decrease = true;
    }

    public void pause()
    {
        decrease = false;
    }
}
