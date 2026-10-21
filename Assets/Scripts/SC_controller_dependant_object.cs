using UnityEngine;

public class SC_controller_dependant_object : MonoBehaviour
{

    public bool controller;
    public bool mouse_keyboard;

    public bool checkonenanble = true;
    public GameObject obj;

    private SC_controller_manager manager;

    private void OnEnable()
    {
        manager = SC_controller_manager.instance;

        if (checkonenanble)
        {
            if ( controller)
            {
                obj.SetActive(manager.using_controller);
            }
            else if (mouse_keyboard)
            {
                obj.SetActive(!manager.using_controller);
            }
        }
    }
    // Update is called once per frame
    void Update()
    {
        if (!checkonenanble)
        {
            if ( controller)
            {
                obj.SetActive(manager.using_controller);
            }
            else if ( mouse_keyboard)
            {
                obj.SetActive(!manager.using_controller);
            }
        }
    }
}
