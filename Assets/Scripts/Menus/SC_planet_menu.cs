using System.Collections.Generic;
using UnityEngine;
using UnityEngine.LightTransport;

public class SC_planet_menu : MonoBehaviour
{
    public SO_world world;
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
        SC_level_selection_screen.instance.update_header(world);
    }

    public void close_header()
    {
        if (sprite != null)
        {
            if (world.unlocked)
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
        if(world.unlocked)
        {
            if (sprite != null)
            {
                sprite.color = locked;
            }
        }
    }

    public void click()
    {
        SC_level_selection_screen.instance.open_planet(world);
    }

    // Update is called once per frame
    void Update()
    {

    }


}
