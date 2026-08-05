using UnityEngine;

public class SC_title_screen : MonoBehaviour
{
    public SC_screenshot_transition transi;
    public string scene;
    void Start()
    {

    }

    void Update()
    {
        // Clavier + boutons de manette
        if (Input.anyKeyDown)
        {
            OnAnyInput();
            return;
        }

        // Souris
        if (Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2))
        {
            OnAnyInput();
            return;
        }

        // Molette de la souris
        if (Input.mouseScrollDelta.y != 0)
        {
            OnAnyInput();
        }
    }

    void OnAnyInput()
    {
        transi.Capture(scene);
    }
}
