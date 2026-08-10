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

    [Header("Navigation Axis")]
    public NavigationAxis navigationAxis = NavigationAxis.Both;

    public enum NavigationAxis
    {
        Both,
        Horizontal,
        Vertical
    }

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
        }

        if (submit != null)
        {
            submit.action.performed -= OnSubmit;
        }

        if (pointerPosition != null)
        {
            pointerPosition.action.Disable();
        }
    }

    private void Update()
    {
        // =========================
        // NAVIGATION STICK / CLAVIER
        // =========================

        if (move != null && !mouseMode)
        {
            Vector2 input = move.action.ReadValue<Vector2>();

            // Ignore les axes qui ne correspondent pas au menu
            input = FilterNavigationInput(input);

            if (input.sqrMagnitude > 0.2f)
            {
                if (!isHolding)
                {
                    currentMoveInput = input;
                    isHolding = true;

                    // Premier mouvement immédiat
                    Navigate(input);

                    // Délai avant répétition
                    holdTimer = initialDelay;
                }
                else
                {
                    currentMoveInput = input;

                    holdTimer -= Time.unscaledDeltaTime;

                    if (holdTimer <= 0f)
                    {
                        Navigate(input);
                        holdTimer = repeatRate;
                    }
                }
            }
            else
            {
                currentMoveInput = Vector2.zero;
                isHolding = false;
                holdTimer = 0f;
            }
        }

        // =========================
        // SOURIS
        // =========================

        if (pointerPosition != null)
        {
            Vector2 mousePos = pointerPosition.action.ReadValue<Vector2>();

            if ((mousePos - lastMousePosition).sqrMagnitude > 1f)
            {
                mouseMode = true;

                if (buttons.Length > 0 &&
                    currentIndex >= 0 &&
                    currentIndex < buttons.Length &&
                    IsButtonAvailable(buttons[currentIndex]))
                {
                    if (buttons[currentIndex].clickable)
                    buttons[currentIndex].UnSelect();
                }
            }

            lastMousePosition = mousePos;
        }
    }

    private void OnMoveInput(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();

        // Ignore complètement les inputs qui ne correspondent
        // pas à l'orientation du menu.
        input = FilterNavigationInput(input);

        if (input.sqrMagnitude < 0.2f)
            return;

        currentMoveInput = input;

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
        holdTimer = 0f;
    }

    /// <summary>
    /// Filtre l'input selon l'orientation du menu.
    /// Horizontal : ignore Y
    /// Vertical   : ignore X
    /// Both       : conserve X et Y
    /// </summary>
    private Vector2 FilterNavigationInput(Vector2 input)
    {
        switch (navigationAxis)
        {
            case NavigationAxis.Horizontal:
                input.y = 0f;
                break;

            case NavigationAxis.Vertical:
                input.x = 0f;
                break;

            case NavigationAxis.Both:
                break;
        }

        return input;
    }

    private void Navigate(Vector2 value)
    {
        // Sécurité supplémentaire : filtre l'input même si
        // Navigate() est appelé depuis un autre endroit.
        value = FilterNavigationInput(value);

        if (Time.unscaledTime < lastMoveTime + moveCooldown)
            return;

        if (value.sqrMagnitude < 0.2f)
            return;

        int next = currentIndex;

        if (!useGrid)
        {
            int direction = 0;

            if (Mathf.Abs(value.y) > Mathf.Abs(value.x))
            {
                // Navigation verticale
                direction = value.y > 0 ? -1 : 1;
            }
            else
            {
                // Navigation horizontale
                direction = value.x > 0 ? 1 : -1;
            }

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
        // Sécurité : filtre l'input avant de calculer ligne/colonne
        value = FilterNavigationInput(value);

        if (value.sqrMagnitude < 0.2f)
            return currentIndex;

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

        if (currentIndex >= 0 &&
            currentIndex < buttons.Length &&
            IsButtonAvailable(buttons[currentIndex]))
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
        if (buttons == null || buttons.Length == 0)
            return;

        // Essaie d'abord le bouton par défaut
        if (defaultSelected >= 0 &&
            defaultSelected < buttons.Length &&
            IsButtonAvailable(buttons[defaultSelected]))
        {
            currentIndex = defaultSelected;
            buttons[currentIndex].Select();
            return;
        }

        // Sinon prend le premier disponible
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
