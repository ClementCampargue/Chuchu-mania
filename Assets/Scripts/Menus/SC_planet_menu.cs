using System.Collections.Generic;
using UnityEngine;

public class SC_planet_menu : MonoBehaviour
{
    public SO_level level;
    public SpriteRenderer sprite;
    public Color selected;
    public Color unselected;
    public Color locked;



    public void update_header()
    {
        if (sprite != null)
        {
            sprite.color = selected;
        }
        SC_level_selection_screen.instance.update_header(level);
    }

    public void close_header()
    {
        if (sprite != null)
        {
            if (level.unlocked)
            {
                sprite.color = locked;
            }
            else
            {
                sprite.color = unselected;
            }
        }
 
        SC_level_selection_screen.instance.close_header();
    }

    void Start()
    {
        if(level.unlocked)
        {
            if (sprite != null)
            {
                sprite.color = locked;
            }
        }
    }

    public void click()
    {
        SC_level_selection_screen.instance.open_planet(level);
    }

    // Update is called once per frame
    void Update()
    {

    }


}
