using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_controller_manager : MonoBehaviour
{
    public static SC_controller_manager instance;

    public bool using_controller;
    public bool using_keyboard;
    public bool using_keyboard_controller;

    //public bool keyboardAsController = false;

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        // Détection des inputs clavier et souris
        if (IsMouseInput())
        {
            EventSystem.current.SetSelectedGameObject(null);
            using_controller = false;
            using_keyboard = true;

            using_keyboard_controller = false;

        }
        // Détection des inputs clavier et souris
        if (IsKeyboardInput())
        {
            using_controller = false;
            using_keyboard = true;
            using_keyboard_controller = true;
            return;
        }

        // Détection des inputs manette
        if (IsControllerInput())
        {
            using_controller = true;
            using_keyboard_controller = true;
            using_keyboard = false;
        }
    }

    private bool IsKeyboardOrMouseInput()
    {
        return IsKeyboardInput() || IsMouseInput();
    }

    private bool IsKeyboardInput()
    {
        if (Input.GetKeyDown(KeyCode.W) ||
            Input.GetKeyDown(KeyCode.A) ||
            Input.GetKeyDown(KeyCode.S) ||
            Input.GetKeyDown(KeyCode.D) ||
            Input.GetKeyDown(KeyCode.Q) ||
            Input.GetKeyDown(KeyCode.Z) ||
            Input.GetKeyDown(KeyCode.UpArrow) ||
            Input.GetKeyDown(KeyCode.DownArrow) ||
            Input.GetKeyDown(KeyCode.LeftArrow) ||
            Input.GetKeyDown(KeyCode.RightArrow) ||
            Input.GetKeyDown(KeyCode.Return) ||
            Input.GetKeyDown(KeyCode.Space) ||
            Input.GetKeyDown(KeyCode.Escape))
        {
            return true;
        }

        return false;
    }

    private bool IsMouseInput()
    {
        if (new Vector2(
            Input.GetAxisRaw("Mouse X"),
            Input.GetAxisRaw("Mouse Y")
        ).sqrMagnitude > 0.01f)
        {
            return true;
        }

        if (Input.GetMouseButtonDown(0) ||
            Input.GetMouseButtonDown(1) ||
            Input.GetMouseButtonDown(2))
        {
            return true;
        }

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

        if (Gamepad.current.leftStick.ReadValue().sqrMagnitude > 0.1f ||
            Gamepad.current.rightStick.ReadValue().sqrMagnitude > 0.1f ||
            Gamepad.current.leftTrigger.ReadValue() > 0.1f ||
            Gamepad.current.rightTrigger.ReadValue() > 0.1f)
        {
            return true;
        }

        return false;
    }
}
