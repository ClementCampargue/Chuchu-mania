using System.Collections.Generic;
using UnityEngine;

public class SC_constellation : MonoBehaviour
{
    public Animator animator;
    public GameObject constellation_visual;
    public List<SC_star_achievement> stars;

    void Start()
    {
        CheckAllStarsUnlocked();
    }

    void Update()
    {
        CheckAllStarsUnlocked();
    }

    private void CheckAllStarsUnlocked()
    {
        if (stars == null || stars.Count == 0)
            return;

        foreach (SC_star_achievement star in stars)
        {
            if (star == null || !star.debloque)
            {
                constellation_visual.SetActive(false);
                return;
            }
        }

        constellation_visual.SetActive(true);
    }
}