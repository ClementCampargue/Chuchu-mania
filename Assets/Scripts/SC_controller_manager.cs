using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_controller_manager : MonoBehaviour
{
    public static SC_controller_manager instance;
    public bool using_controller;

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        for (int i = 0; i < 20; i++)
        {
   
        }
        // Si aucun élément n'est sélectionné
        if (EventSystem.current.currentSelectedGameObject == null)
        {
            // Détection d'un input manette (axes)
            float horizontal = Input.GetAxis("Horizontal");
            float vertical = Input.GetAxis("Vertical");

            if (Mathf.Abs(horizontal) > 0.5f || Mathf.Abs(vertical) > 0.5f || IsControllerInput())
            {
                using_controller = true;
            }
        }

        if (IsKeyboardOrMouseInput())
        {
            EventSystem.current.SetSelectedGameObject(null);
            using_controller = false;
        }

    }
    private bool IsKeyboardOrMouseInput()
    {
        // Clavier
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }

        if (new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y")).sqrMagnitude > 0.01f)
            return true;

        // Clic souris
        if (Input.GetMouseButtonDown(0) || Input.GetMouseButtonDown(1) || Input.GetMouseButtonDown(2))
            return true;

        return false;
    }

    private bool IsControllerInput()
    {
        if (Gamepad.current == null)
            return false;

        if (Gamepad.current.buttonSouth.wasPressedThisFrame ||
            Gamepad.current.buttonEast.wasPressedThisFrame ||
            Gamepad.current.buttonWest.wasPressedThisFrame ||
            Gamepad.current.buttonNorth.wasPressedThisFrame ||
            Gamepad.current.leftShoulder.wasPressedThisFrame ||
            Gamepad.current.rightShoulder.wasPressedThisFrame ||
            Gamepad.current.startButton.wasPressedThisFrame ||
            Gamepad.current.selectButton.wasPressedThisFrame ||
            Gamepad.current.leftStickButton.wasPressedThisFrame ||
            Gamepad.current.rightStickButton.wasPressedThisFrame)
        {
            return true;
        }

        if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f)
        {
            return true;
        }

        if (Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f)
        {
            return true;
        }

        if (Gamepad.current.leftTrigger.ReadValue() > 0.1f ||
            Gamepad.current.rightTrigger.ReadValue() > 0.1f)
        {
            return true;
        }

        return false;
    }
}
