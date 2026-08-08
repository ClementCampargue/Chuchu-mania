using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SC_winscreen_scoring : MonoBehaviour
{
    private SC_score score;
    private sc_health_system health;
    private SC_timer time;
    public TextMeshPro life_score;
    public TextMeshPro time_score;
    public TextMeshPro final_score;

    public SpriteRenderer rank;
    public List<Sprite> ranks;

    public int score_per_heart = 5000;
    public int score_per_time = 50;
    private int base_score;
    private int time_bonus;
    private int health_bonus;
    void Start()
    {
        score = SC_score.Instance;
        health = sc_health_system.instance;


    }

    void calculate_dialogue()
    {
        base_score = score.score;
        time_bonus = (int)(time.elapsedTime * score_per_time);
        health_bonus = score_per_heart * health.current_health;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

 
}
