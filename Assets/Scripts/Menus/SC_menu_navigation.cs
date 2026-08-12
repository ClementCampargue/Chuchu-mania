using UnityEngine;
using UnityEngine.InputSystem;

public class SC_menu_navigation : MonoBehaviour
{
    [Header("Buttons")]
    public SC_Button[] buttons;
    public int defaultSelected = 0;
    public bool autoFindButtons = true;

    [Header("Mouse")]
    [Tooltip("Si activé, bouger la souris ne désélectionne pas le bouton actuel.")]
    public bool ignore_mouse = false;

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

    [Header("Selection")]
    [Tooltip("Le dernier bouton sélectionné est mémorisé quand le menu est fermé.")]
    [SerializeField] private int lastSelected = -1;

    [Header("Navigation Cooldown")]
    public float moveCooldown = 0.05f;

    private int currentIndex = -1;
    private float lastMoveTime;

    private Vector2 currentMoveInput;
    private float holdTimer;
    private bool isHolding;

    private Vector2 lastMousePosition;
    private bool mouseMode;


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        if (autoFindButtons)
        {
            buttons = GetComponentsInChildren<SC_Button>(true);
        }

        if (buttons == null)
        {
            buttons = new SC_Button[0];
        }

        if (sortButtonsByPosition)
        {
            SortButtonsByRectTransform();
        }

        currentIndex = -1;
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

        mouseMode = false;

        isHolding = false;
        holdTimer = 0f;
        currentMoveInput = Vector2.zero;

        SelectFirstAvailable();
    }


    private void OnDisable()
    {
        if (IsValidIndex(currentIndex) &&
            IsButtonAvailable(buttons[currentIndex]))
        {
            lastSelected = currentIndex;
        }

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

        isHolding = false;
        holdTimer = 0f;
        currentMoveInput = Vector2.zero;
    }


    private void Update()
    {
        // =====================================================
        // SOURIS
        // =====================================================

        if (pointerPosition != null)
        {
            Vector2 mousePos = pointerPosition.action.ReadValue<Vector2>();

            // Si ignore_mouse est activé :
            // la souris est complètement ignorée par ce système.
            if (!ignore_mouse)
            {
                if ((mousePos - lastMousePosition).sqrMagnitude > 1f)
                {
                    if (!mouseMode)
                    {
                        EnterMouseMode();
                    }
                }
            }

            lastMousePosition = mousePos;
        }


        // =====================================================
        // NAVIGATION MANETTE / CLAVIER
        // =====================================================

        if (move != null && !mouseMode)
        {
            Vector2 input = move.action.ReadValue<Vector2>();

            input = FilterNavigationInput(input);

            if (input.sqrMagnitude > 0.2f)
            {
                if (!isHolding)
                {
                    currentMoveInput = input;
                    isHolding = true;

                    Navigate(input);

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
    }


    // =========================================================
    // MODE SOURIS
    // =========================================================

    private void EnterMouseMode()
    {
        // Sécurité supplémentaire.
        if (ignore_mouse)
            return;

        mouseMode = true;

        isHolding = false;
        holdTimer = 0f;
        currentMoveInput = Vector2.zero;

        ClearVisualSelection();
    }


    private void ClearVisualSelection()
    {
        if (IsValidIndex(currentIndex) &&
            buttons[currentIndex] != null)
        {
            buttons[currentIndex].UnSelect();
        }
    }


    private void EnterNavigationMode()
    {
        if (!mouseMode)
            return;

        mouseMode = false;

        RestoreCurrentSelection();
    }


    private void RestoreCurrentSelection()
    {
        if (buttons == null || buttons.Length == 0)
            return;

        if (!IsValidIndex(currentIndex) ||
            !IsButtonAvailable(buttons[currentIndex]))
        {
            int available = FindFirstAvailable();

            if (available < 0)
                return;

            currentIndex = available;
            lastSelected = available;
        }

        ClearAllSelectionsExcept(currentIndex);

        buttons[currentIndex].Select();
    }


    // =========================================================
    // INPUT MOVE
    // =========================================================

    private void OnMoveInput(InputAction.CallbackContext ctx)
    {
        Vector2 input = ctx.ReadValue<Vector2>();

        input = FilterNavigationInput(input);

        if (input.sqrMagnitude < 0.2f)
            return;

        // Une entrée manette/clavier force toujours
        // le retour en mode navigation.
        EnterNavigationMode();

        currentMoveInput = input;

        if (!isHolding)
        {
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


    // =========================================================
    // FILTER AXIS
    // =========================================================

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


    // =========================================================
    // NAVIGATION
    // =========================================================

    private void Navigate(Vector2 value)
    {
        value = FilterNavigationInput(value);

        if (value.sqrMagnitude < 0.2f)
            return;

        if (Time.unscaledTime < lastMoveTime + moveCooldown)
            return;

        EnsureValidCurrentIndex();

        int next;

        if (!useGrid)
        {
            int direction;

            if (Mathf.Abs(value.y) > Mathf.Abs(value.x))
            {
                direction = value.y > 0f ? -1 : 1;
            }
            else
            {
                direction = value.x > 0f ? 1 : -1;
            }

            next = GetNextAvailableIndex(
                currentIndex,
                direction
            );
        }
        else
        {
            next = NavigateGrid(value);
        }

        if (next >= 0 && next != currentIndex)
        {
            Select(next);
        }

        lastMoveTime = Time.unscaledTime;
    }


    // =========================================================
    // GRID
    // =========================================================

    private int NavigateGrid(Vector2 value)
    {
        value = FilterNavigationInput(value);

        if (value.sqrMagnitude < 0.2f)
            return currentIndex;

        if (columns <= 0)
            columns = 1;

        EnsureValidCurrentIndex();

        int row = currentIndex / columns;
        int col = currentIndex % columns;

        bool vertical =
            Mathf.Abs(value.y) > Mathf.Abs(value.x);

        if (vertical)
        {
            row += value.y > 0f ? -1 : 1;
        }
        else
        {
            col += value.x > 0f ? 1 : -1;
        }

        int maxRows = Mathf.CeilToInt(
            (float)buttons.Length / columns
        );

        // Horizontal
        if (wrapHorizontal)
        {
            if (col < 0)
                col = columns - 1;

            if (col >= columns)
                col = 0;
        }
        else
        {
            col = Mathf.Clamp(
                col,
                0,
                columns - 1
            );
        }

        // Vertical
        if (wrapVertical)
        {
            if (row < 0)
                row = maxRows - 1;

            if (row >= maxRows)
                row = 0;
        }
        else
        {
            row = Mathf.Clamp(
                row,
                0,
                maxRows - 1
            );
        }

        int targetIndex = row * columns + col;

        if (targetIndex < 0 ||
            targetIndex >= buttons.Length)
        {
            return FindClosestAvailableInDirection(
                currentIndex,
                value
            );
        }

        if (!IsButtonAvailable(buttons[targetIndex]))
        {
            return FindClosestAvailableInDirection(
                currentIndex,
                value
            );
        }

        return targetIndex;
    }


    // =========================================================
    // GRID FALLBACK
    // =========================================================

    private int FindClosestAvailableInDirection(
        int startIndex,
        Vector2 direction)
    {
        if (buttons == null ||
            buttons.Length == 0)
        {
            return -1;
        }

        if (!IsValidIndex(startIndex))
            return FindFirstAvailable();

        int startRow = startIndex / columns;
        int startCol = startIndex % columns;

        bool vertical =
            Mathf.Abs(direction.y) > Mathf.Abs(direction.x);

        int step = vertical
            ? (direction.y > 0f ? -1 : 1)
            : (direction.x > 0f ? 1 : -1);

        int maxRows = Mathf.CeilToInt(
            (float)buttons.Length / columns
        );

        int row = startRow;
        int col = startCol;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (vertical)
            {
                row += step;

                if (wrapVertical)
                {
                    if (row < 0)
                        row = maxRows - 1;

                    if (row >= maxRows)
                        row = 0;
                }
                else
                {
                    if (row < 0 ||
                        row >= maxRows)
                    {
                        break;
                    }
                }
            }
            else
            {
                col += step;

                if (wrapHorizontal)
                {
                    if (col < 0)
                        col = columns - 1;

                    if (col >= columns)
                        col = 0;
                }
                else
                {
                    if (col < 0 ||
                        col >= columns)
                    {
                        break;
                    }
                }
            }

            int index = row * columns + col;

            if (index >= 0 &&
                index < buttons.Length &&
                IsButtonAvailable(buttons[index]))
            {
                return index;
            }
        }

        return startIndex;
    }


    // =========================================================
    // SUBMIT
    // =========================================================

    private void OnSubmit(InputAction.CallbackContext ctx)
    {
        // En mode souris, pas de Submit clavier/manette.
        if (mouseMode)
            return;

        EnsureValidCurrentIndex();

        if (!IsValidIndex(currentIndex))
            return;

        SC_Button selectedButton = buttons[currentIndex];

        if (!IsButtonAvailable(selectedButton))
            return;

        selectedButton.Press();
    }


    // =========================================================
    // SELECTION
    // =========================================================

    private void Select(int index)
    {
        if (buttons == null ||
            buttons.Length == 0)
        {
            return;
        }

        if (!IsValidIndex(index))
            return;

        if (!IsButtonAvailable(buttons[index]))
            return;

        // Garantit qu'un seul bouton est visuellement sélectionné.
        ClearAllSelectionsExcept(index);

        currentIndex = index;
        lastSelected = index;

        buttons[index].Select();
    }


    private void ClearAllSelectionsExcept(int exceptIndex)
    {
        if (buttons == null)
            return;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == exceptIndex)
                continue;

            if (buttons[i] != null)
            {
                buttons[i].UnSelect();
            }
        }
    }


    // =========================================================
    // INITIAL SELECTION
    // =========================================================

    private void SelectFirstAvailable()
    {
        if (buttons == null ||
            buttons.Length == 0)
        {
            return;
        }

        ClearAllSelectionsExcept(-1);

        // Dernier bouton
        if (IsValidIndex(lastSelected) &&
            IsButtonAvailable(buttons[lastSelected]))
        {
            currentIndex = lastSelected;

            buttons[currentIndex].Select();

            return;
        }

        // Bouton par défaut
        if (IsValidIndex(defaultSelected) &&
            IsButtonAvailable(buttons[defaultSelected]))
        {
            currentIndex = defaultSelected;
            lastSelected = defaultSelected;

            buttons[currentIndex].Select();

            return;
        }

        // Premier disponible
        int first = FindFirstAvailable();

        if (first >= 0)
        {
            currentIndex = first;
            lastSelected = first;

            buttons[currentIndex].Select();
        }
    }


    // =========================================================
    // RESET
    // =========================================================

    public void ResetFirstSelected()
    {
        if (buttons == null ||
            buttons.Length == 0)
        {
            return;
        }

        ClearAllSelectionsExcept(-1);

        lastSelected = -1;
        currentIndex = -1;

        mouseMode = false;
        isHolding = false;
        holdTimer = 0f;

        SelectFirstAvailable();
    }


    // =========================================================
    // VALIDATION
    // =========================================================

    private void EnsureValidCurrentIndex()
    {
        if (IsValidIndex(currentIndex) &&
            IsButtonAvailable(buttons[currentIndex]))
        {
            return;
        }

        int available = FindFirstAvailable();

        if (available >= 0)
        {
            currentIndex = available;
            lastSelected = available;

            if (!mouseMode)
            {
                ClearAllSelectionsExcept(currentIndex);

                buttons[currentIndex].Select();
            }
        }
        else
        {
            currentIndex = -1;
        }
    }


    private int FindFirstAvailable()
    {
        if (buttons == null)
            return -1;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (IsButtonAvailable(buttons[i]))
                return i;
        }

        return -1;
    }


    private bool IsValidIndex(int index)
    {
        return buttons != null &&
               index >= 0 &&
               index < buttons.Length;
    }


    private bool IsButtonAvailable(SC_Button button)
    {
        return button != null &&
               button.gameObject.activeInHierarchy &&
               button.enabled;
    }


    // =========================================================
    // GET NEXT AVAILABLE
    // =========================================================

    private int GetNextAvailableIndex(
        int startIndex,
        int direction)
    {
        if (buttons == null ||
            buttons.Length == 0)
        {
            return -1;
        }

        if (direction == 0)
            direction = 1;

        if (!IsValidIndex(startIndex))
            startIndex = 0;

        int index = startIndex;

        for (int i = 0; i < buttons.Length; i++)
        {
            index += direction;

            if (index < 0)
            {
                if (wrapHorizontal || wrapVertical)
                    index = buttons.Length - 1;
                else
                    return startIndex;
            }

            if (index >= buttons.Length)
            {
                if (wrapHorizontal || wrapVertical)
                    index = 0;
                else
                    return startIndex;
            }

            if (IsButtonAvailable(buttons[index]))
                return index;
        }

        return startIndex;
    }


    // =========================================================
    // SORT
    // =========================================================

    private void SortButtonsByRectTransform()
    {
        System.Array.Sort(buttons, (a, b) =>
        {
            if (a == null)
                return 1;

            if (b == null)
                return -1;

            RectTransform rectA =
                a.GetComponent<RectTransform>();

            RectTransform rectB =
                b.GetComponent<RectTransform>();

            if (rectA == null ||
                rectB == null)
            {
                return 0;
            }

            Vector3 posA = rectA.localPosition;
            Vector3 posB = rectB.localPosition;

            float lineTolerance = 20f;

            // Même ligne = gauche vers droite.
            if (Mathf.Abs(posA.y - posB.y) < lineTolerance)
            {
                return posA.x.CompareTo(posB.x);
            }

            // Tri vertical.
            if (reverseVertical)
            {
                return posB.y.CompareTo(posA.y);
            }

            return posA.y.CompareTo(posB.y);
        });
    }


    // =========================================================
    // PUBLIC ACCESS
    // =========================================================

    public int GetCurrentIndex()
    {
        return currentIndex;
    }


    public SC_Button GetCurrentButton()
    {
        if (!IsValidIndex(currentIndex))
            return null;

        return buttons[currentIndex];
    }
}