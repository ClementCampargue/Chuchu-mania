using UnityEngine;
using UnityEngine.InputSystem;

public class SC_Menu : MonoBehaviour
{
    private SC_Button[] buttons;

    public InputActionReference move;
    public InputActionReference confirm;

    private int current_button;

    private float holdTime;
    private float lastInput;

    private bool isHolding;
    private int lastDirection; // -1 down, +1 up

    public float repeatDelay = 0.25f;
    public float repeatRate = 0.1f;

    void Start()
    {
        buttons = GetComponentsInChildren<SC_Button>();

        if (buttons.Length > 0)
        {
            current_button = 0;
            buttons[current_button].Select();
        }
    }

    void Update()
    {
        if (buttons.Length == 0)
            return;

        Vector2 input = move.action.ReadValue<Vector2>();

        // Confirm
        if (confirm.action.WasPerformedThisFrame() && IsAnyButtonSelected())
        {
            buttons[current_button].Press();
        }

        int direction = 0;

        if (input.y > 0.5f) direction = 1;
        else if (input.y < -0.5f) direction = -1;

        // --- RESET quand on relâche ---
        if (direction == 0)
        {
            isHolding = false;
            holdTime = 0f;
            lastDirection = 0;
            return;
        }

        // --- PREMIER TAP : immédiat ---
        if (!isHolding)
        {
            Move(direction);
            isHolding = true;
            lastDirection = direction;
            holdTime = 0f;
            return;
        }

        // --- HOLD : répétition avec délai ---
        if (direction == lastDirection)
        {
            holdTime += Time.unscaledDeltaTime;

            if (holdTime >= repeatDelay)
            {
                lastInput += Time.unscaledDeltaTime;

                if (lastInput >= repeatRate)
                {
                    Move(direction);
                    lastInput = 0f;
                }
            }
        }
        else
        {
            // changement de direction -> reset propre
            Move(direction);
            lastDirection = direction;
            holdTime = 0f;
            lastInput = 0f;
        }
    }

    private void Move(int dir)
    {
        buttons[current_button].UnSelect();

        if (dir < 0)
        {
            current_button = (current_button + 1) % buttons.Length;
        }
        else if (dir > 0)
        {
            current_button--;

            if (current_button < 0)
                current_button = buttons.Length - 1;
        }

        buttons[current_button].Select();
    }

    private bool IsAnyButtonSelected()
    {
        foreach (SC_Button button in buttons)
        {
            if (button.isHovered)
                return true;
        }
        return false;
    }
}