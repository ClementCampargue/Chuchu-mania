using UnityEngine;
using UnityEngine.InputSystem;

public class SC_menu_navigation : MonoBehaviour
{
    [Header("Buttons")]
    public SC_Button[] buttons;
    public int defaultSelected = 0;
    public bool autoFindButtons = true;

    [Header("Input Actions")]
    public InputActionReference move;
    public InputActionReference submit;
    public InputActionReference pointerPosition;
    [Header("Auto Sorting")]
    public bool sortButtonsByPosition = true;
    public bool reverseVertical = true;
    [Header("Navigation")]
    public bool wrapHorizontal = true;
    public bool wrapVertical = true;
    public bool useGrid = false;
    public int columns = 1;

    [Header("Hold Navigation")]
    public float initialDelay = 0.4f;
    public float repeatRate = 0.1f;

    private int currentIndex;
    private float lastMoveTime;
    public float moveCooldown = 0.05f;

    private Vector2 currentMoveInput;
    private float holdTimer;
    private bool isHolding;

    private Vector2 lastMousePosition;
    private bool mouseMode;

    private void Awake()
    {
        if (autoFindButtons)
        {
            buttons = GetComponentsInChildren<SC_Button>(true);
        }

        if (sortButtonsByPosition)
        {
            SortButtonsByRectTransform();
        }
    }
    private void SortButtonsByRectTransform()
    {
        System.Array.Sort(buttons, (a, b) =>
        {
            RectTransform rectA = a.GetComponent<RectTransform>();
            RectTransform rectB = b.GetComponent<RectTransform>();

            if (rectA == null || rectB == null)
                return 0;


            Vector3 posA = rectA.localPosition;
            Vector3 posB = rectB.localPosition;


            // Tolérance pour considérer deux boutons sur la même ligne
            float lineTolerance = 20f;


            // Même ligne -> tri gauche droite
            if (Mathf.Abs(posA.y - posB.y) < lineTolerance)
            {
                return posA.x.CompareTo(posB.x);
            }


            // Sinon tri vertical
            if (reverseVertical)
            {
                return posB.y.CompareTo(posA.y);
            }
            else
            {
                return posA.y.CompareTo(posB.y);
            }
        });
    }
    private void OnEnable()
    {
        if (move != null)
        {
            move.action.Enable();
            move.action.performed += OnMoveInput;
            move.action.canceled += OnMoveRelease;
        }

        if (submit != null)
        {
            submit.action.Enable();
            submit.action.performed += OnSubmit;
        }

        if (pointerPosition != null)
        {
            pointerPosition.action.Enable();
            lastMousePosition = pointerPosition.action.ReadValue<Vector2>();
        }


        SelectFirstAvailable();
    }


    private void OnDisable()
    {
        if (move != null)
        {
            move.action.performed -= OnMoveInput;
            move.action.canceled -= OnMoveRelease;
            move.action.Disable();
        }

        if (submit != null)
        {
            submit.action.performed -= OnSubmit;
            submit.action.Disable();
        }

        if (pointerPosition != null)
        {
            pointerPosition.action.Disable();
        }
    }


    private void Update()
    {
        // Maintien navigation
        if (isHolding && currentMoveInput.sqrMagnitude > 0.2f)
        {
            holdTimer -= Time.unscaledDeltaTime;

            if (holdTimer <= 0)
            {
                Navigate(currentMoveInput);
                holdTimer = repeatRate;
            }
        }


        // Mouvement souris = quitter sélection clavier
        if (pointerPosition != null)
        {
            Vector2 mousePos = pointerPosition.action.ReadValue<Vector2>();

            if ((mousePos - lastMousePosition).sqrMagnitude > 1f)
            {
                mouseMode = true;

                if (IsButtonAvailable(buttons[currentIndex]))
                    buttons[currentIndex].UnSelect();
            }

            lastMousePosition = mousePos;
        }
    }


    private void OnMoveInput(InputAction.CallbackContext ctx)
    {
        currentMoveInput = ctx.ReadValue<Vector2>();

        if (!isHolding)
        {
            mouseMode = false;

            Navigate(currentMoveInput);

            isHolding = true;
            holdTimer = initialDelay;
        }
    }


    private void OnMoveRelease(InputAction.CallbackContext ctx)
    {
        currentMoveInput = Vector2.zero;
        isHolding = false;
    }


    private void Navigate(Vector2 value)
    {
        if (Time.unscaledTime < lastMoveTime + moveCooldown)
            return;

        if (value.sqrMagnitude < 0.2f)
            return;


        int next = currentIndex;


        if (!useGrid)
        {
            int direction = 0;

            if (Mathf.Abs(value.y) > Mathf.Abs(value.x))
                direction = value.y > 0 ? -1 : 1;
            else
                direction = value.x > 0 ? 1 : -1;


            next = GetNextAvailableIndex(currentIndex, direction);
        }
        else
        {
            next = NavigateGrid(value);
        }


        if (next != currentIndex)
        {
            Select(next);
        }


        lastMoveTime = Time.unscaledTime;
    }


    private int NavigateGrid(Vector2 value)
    {
        int row = currentIndex / columns;
        int col = currentIndex % columns;


        if (Mathf.Abs(value.y) > Mathf.Abs(value.x))
        {
            row += value.y > 0 ? -1 : 1;
        }
        else
        {
            col += value.x > 0 ? 1 : -1;
        }


        int maxRows = Mathf.CeilToInt((float)buttons.Length / columns);


        if (wrapHorizontal)
        {
            if (col < 0)
                col = columns - 1;

            if (col >= columns)
                col = 0;
        }
        else
        {
            col = Mathf.Clamp(col, 0, columns - 1);
        }


        if (wrapVertical)
        {
            if (row < 0)
                row = maxRows - 1;

            if (row >= maxRows)
                row = 0;
        }
        else
        {
            row = Mathf.Clamp(row, 0, maxRows - 1);
        }


        int index = row * columns + col;


        if (index >= buttons.Length)
            return currentIndex;


        if (!IsButtonAvailable(buttons[index]))
            return GetNextAvailableIndex(index, 1);


        return index;
    }


    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        if (mouseMode)
            return;

        if (IsButtonAvailable(buttons[currentIndex]))
        {
            buttons[currentIndex].Press();
        }
    }


    private void Select(int index)
    {
        if (buttons.Length == 0)
            return;


        if (!IsButtonAvailable(buttons[index]))
            return;


        if (IsButtonAvailable(buttons[currentIndex]))
        {
            buttons[currentIndex].UnSelect();
        }


        currentIndex = index;

        buttons[currentIndex].Select();
    }


    private void SelectFirstAvailable()
    {
        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsButtonAvailable(buttons[i]))
            {
                currentIndex = i;
                buttons[i].Select();
                return;
            }
        }
    }


    private bool IsButtonAvailable(SC_Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.enabled;
    }


    private int GetNextAvailableIndex(int startIndex, int direction)
    {
        int index = startIndex;


        for (int i = 0; i < buttons.Length; i++)
        {
            index += direction;


            if (index < 0)
                index = buttons.Length - 1;


            if (index >= buttons.Length)
                index = 0;


            if (IsButtonAvailable(buttons[index]))
                return index;
        }


        return startIndex;
    }
}