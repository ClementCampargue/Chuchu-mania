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

    private void Start()
    {
        current_health = max_health;
        UpdateHearts();
    }

    public void take_damage(int damage)
    {
        if (damage <= 0)
            return;

        current_health = Mathf.Max(current_health - damage, 0);

        UpdateHearts();
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null)
                continue;

            // Exemple :
            // 3 vies -> hearts[0], hearts[1], hearts[2] allumés
            // 2 vies -> hearts[0], hearts[1] allumés
            // 1 vie  -> hearts[0] allumé
            // 0 vie  -> tous éteints
            hearts[i].sprite = i < current_health
                ? heart_on
                : heart_off;
        }
    }

    public void revive()
    {
        current_health = max_health;
        UpdateHearts();
    }
}
