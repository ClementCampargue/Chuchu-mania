
using TMPro;
using UnityEngine;

public class SC_planet_menu : MonoBehaviour
{
    [Header("World")]
    public SO_world world;

    [Header("Visuals")]
    public SpriteRenderer sprite;
    public SpriteRenderer sprite_planet;
    public Color selected;
    public Color unselected;
    public Color locked;
    public Sprite locked_;
    public TextMeshPro price;
    public GameObject price_;
 
    private bool IsWorldUnlocked()
    {
        if (world == null)
            return false;

        if (SC_SaveManager.instance != null &&
            SC_SaveManager.instance.data != null)
        {
            return SC_SaveManager.instance.IsWorldUnlocked(world);
        }

        return world.startUnlocked;
    }

    public void update_header()
    {
        if (sprite != null)
            sprite.color = selected;

        if (SC_level_selection_screen.instance != null)
            SC_level_selection_screen.instance.update_header(world);
    }

    public void close_header()
    {
        UpdatePlanetColor();

        if (SC_level_selection_screen.instance != null)
            SC_level_selection_screen.instance.close_header();
    }

    private void OnEnable()
    {
        UpdatePlanetColor();
    }

    private void UpdatePlanetColor()
    {
        if (sprite == null)
            return;

        sprite.color = IsWorldUnlocked()
            ? locked
            : unselected;
        sprite_planet.sprite = IsWorldUnlocked()
            ? world.small_planet_sprite
            : locked_;
        price_.SetActive(!IsWorldUnlocked());
        price.text = world.price.ToString() ;
    }

    public void click()
    {
        if (world == null)
        {
            Debug.LogWarning(
                "Aucun SO_world assigné à " + gameObject.name
            );
            return;
        }

        if (SC_level_selection_screen.instance != null)
            SC_level_selection_screen.instance.open_planet(world);
    }
}
