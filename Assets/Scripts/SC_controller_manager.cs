using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

public class SC_controller_manager : MonoBehaviour
{
    public static SC_controller_manager instance;

    public bool using_controller;

    [Header("Configuration")]
    public bool keyboardAsController = false;

    private void Awake()
    {
        instance = this;
    }

    void Update()
    {
        // Détection des inputs clavier et souris
        if (IsKeyboardOrMouseInput())
        {
            EventSystem.current.SetSelectedGameObject(null);

            // Si le clavier est considéré comme une manette
            using_controller = keyboardAsController && IsKeyboardInput();

            return;
        }

        // Détection des inputs manette
        if (IsControllerInput())
        {
            using_controller = true;
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
