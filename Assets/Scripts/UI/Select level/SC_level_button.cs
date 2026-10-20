using UnityEngine;

public class SC_level_button : MonoBehaviour
{
    public GameObject unlocked;
    public GameObject locked;
    public bool cant_open;
    public SpriteRenderer rank;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void check_locked(bool locked_)
    {
        cant_open = !locked_;
        unlocked.SetActive(!locked_);
        locked.SetActive(locked_);
    }
}
