using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms.Impl;

public class SC_constellation : MonoBehaviour
{
    public Animator animator;
    public GameObject constellation_visual;
    public List<SC_star_achievement> stars;
    public Animator anim;
    public bool already_unlocked;
    public SO_Sticker reward;
    void Start()
    {
        CheckAllStarsUnlocked2();
    }

    void Update()
    {

    }

    private void CheckAllStarsUnlocked2()
    {
        if (stars == null || stars.Count == 0)
            return;

        foreach (SC_star_achievement star in stars)
        {
            if (star == null || !star.debloque || !star.récolté)
            {
                constellation_visual.SetActive(false);
                return;
            }
        }

        constellation_visual.SetActive(true);
    }

    public void CheckAllStarsUnlocked()
    {
        if (stars == null || already_unlocked || stars.Count == 0)
            return;

        foreach (SC_star_achievement star in stars)
        {
            if (star == null || !star.debloque || !star.récolté)
            {
                constellation_visual.SetActive(false);
                SC_achievement_menu.instance.caninteract = true;
                return;
            }
        }
        SC_achievement_menu.instance.caninteract = false;
        anim.enabled = true;
        SC_achievement_menu.instance.hide_sticker();
        constellation_visual.SetActive(true);
        already_unlocked = true;
    }

    public void show_reward()
    {
        SC_sticker_popup.instance.ShowStickers(
            new SO_Sticker[] { reward }
        );
    }
}