using System.Collections.Generic;
using UnityEngine;

public class sc_health_system : MonoBehaviour
{
    public List<SpriteRenderer> hearts;

    public Sprite heart_on;
    public Sprite heart_on_bonus;
    public Sprite heart_off;

    // Cœurs de base
    public int current_health;
    public int max_health;

    // Cœurs bonus
    public int current_bonus_health;
    public int max_bonus_health;

    public static sc_health_system instance;
    public AudioSource low_health;

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        current_health = max_health;
        current_bonus_health = 0;

        UpdateHearts();
    }

    public void take_damage(int damage)
    {
        if (damage <= 0)
            return;

        // Les dégâts retirent d'abord les cœurs bonus
        if (current_bonus_health > 0)
        {
            int bonus_damage = Mathf.Min(damage, current_bonus_health);

            current_bonus_health -= bonus_damage;
            damage -= bonus_damage;
        }

        // Puis les cœurs de base
        if (damage > 0)
        {
            current_health = Mathf.Max(current_health - damage, 0);
        }

        if (current_health == 1)
        {
            low_health.Play();
        }

        UpdateHearts();
    }

    private void UpdateHearts()
    {
        for (int i = 0; i < hearts.Count; i++)
        {
            if (hearts[i] == null)
                continue;

            // Cœurs de base
            if (i < max_health)
            {
                hearts[i].enabled = true;

                hearts[i].sprite = i < current_health
                    ? heart_on
                    : heart_off;
            }
            // Cœurs bonus
            else
            {
                int bonus_index = i - max_health;

                // Active uniquement les cœurs bonus que le joueur possède
                if (bonus_index < max_bonus_health)
                {
                    hearts[i].enabled = true;
                    if ( bonus_index < current_bonus_health)
                    {
                        hearts[i].sprite = heart_on_bonus;
                        hearts[i].enabled= true;
                    }
                    else
                    {
                        hearts[i].enabled = false;
                    }



                }
                else
                {
                    hearts[i].enabled = false;
                }
            }
        }
    }

    public void revive()
    {
        current_health = max_health;
        current_bonus_health = 0;

        UpdateHearts();
    }

    public void heal()
    {
        // Soigne d'abord les cœurs de base
        if (current_health < max_health)
        {
            current_health++;
        }
        // Puis les cœurs bonus
        else if (current_bonus_health < max_bonus_health)
        {
            current_bonus_health++;
        }

        UpdateHearts();
    }

    public void add_bonus_hearts(int amount)
    {
        if (amount <= 0)
            return;

        max_bonus_health += amount;
        current_bonus_health += amount;

        UpdateHearts();
    }
}