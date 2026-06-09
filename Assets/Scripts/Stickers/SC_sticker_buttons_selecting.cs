using UnityEngine;
using UnityEngine.InputSystem;

public class SC_sticker_buttons_selecting : MonoBehaviour
{
    private SC_sticker_button[] buttons;

    public InputActionReference move;
    public InputActionReference confirm;

    [Header("Grid")]
    public int columns = 4; // nombre de boutons par ligne

    private int currentButton;

    private float holdTime;
    private float lastInput;

    private bool isHolding;
    private Vector2Int lastDirection;

    public float repeatDelay = 0.25f;
    public float repeatRate = 0.1f;

    void Start()
    {
        buttons = GetComponentsInChildren<SC_sticker_button>();

        if (buttons.Length > 0)
        {
            currentButton = 0;
            buttons[currentButton].OnMouseEnter();
        }
    }

    void Update()
    {
        if (buttons.Length == 0)
            return;

        Vector2 input = move.action.ReadValue<Vector2>();

        // Confirmation
        if (confirm.action.WasPerformedThisFrame() && IsAnyButtonSelected())
        {
            buttons[currentButton].OnMouseDown();
        }

        Vector2Int direction = Vector2Int.zero;

        // On garde uniquement l'axe dominant
        if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
        {
            if (input.x > 0.5f)
                direction = Vector2Int.right;
            else if (input.x < -0.5f)
                direction = Vector2Int.left;
        }
        else
        {
            if (input.y > 0.5f)
                direction = Vector2Int.up;
            else if (input.y < -0.5f)
                direction = Vector2Int.down;
        }

        // Reset lorsqu'on relâche
        if (direction == Vector2Int.zero)
        {
            isHolding = false;
            holdTime = 0f;
            lastInput = 0f;
            lastDirection = Vector2Int.zero;
            return;
        }

        // Premier appui
        if (!isHolding)
        {
            Move(direction);
            isHolding = true;
            lastDirection = direction;
            holdTime = 0f;
            lastInput = 0f;
            return;
        }

        // Maintien
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
            // Changement de direction
            Move(direction);
            lastDirection = direction;
            holdTime = 0f;
            lastInput = 0f;
        }
    }

    private void Move(Vector2Int dir)
    {
        buttons[currentButton].OnMouseExit();

        int row = currentButton / columns;
        int col = currentButton % columns;

        if (dir == Vector2Int.right)
        {
            col++;

            // Cycle sur la même ligne
            if (col >= columns)
                col = 0;
        }
        else if (dir == Vector2Int.left)
        {
            col--;

            // Cycle sur la même ligne
            if (col < 0)
                col = columns - 1;
        }
        else if (dir == Vector2Int.down)
        {
            row++;
        }
        else if (dir == Vector2Int.up)
        {
            row--;
        }

        int newIndex = row * columns + col;

        // Gestion du haut/bas
        if (newIndex < 0 || newIndex >= buttons.Length)
        {
            buttons[currentButton].OnMouseEnter();
            return;
        }

        currentButton = newIndex;
        buttons[currentButton].OnMouseEnter();
    }
    private bool IsAnyButtonSelected()
    {
        foreach (SC_sticker_button button in buttons)
        {
            if (button.hovered)
                return true;
        }

        return false;
    }
}