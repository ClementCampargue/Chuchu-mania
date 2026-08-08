using System.Collections.Generic;
using UnityEngine;

public class sc_health_system : MonoBehaviour
{
    public List<SpriteRenderer> hearts;
    public Sprite heart_on;
    public Sprite heart_off;

    public int current_health;
    public int max_health;
    public static sc_health_system instance;
    private void Awake()
    {
        instance = this;
    }
    void Start()
    {
        current_health = max_health;
    }

    // Update is called once per frame
    void Update()
    {

    }
    public void take_damage(int i)
    {
        hearts[current_health].sprite = heart_off;
        current_health = current_health -i;
    }

    public void revive()
    {
        current_health = max_health;

        foreach (SpriteRenderer spr in hearts)
        {
            spr.sprite = heart_on;
        }
    }
}
