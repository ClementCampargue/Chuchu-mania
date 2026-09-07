using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using static UnityEngine.Rendering.GPUSort;

public class SC_ConstellationNavigation : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private int defaultSelectedButton = 0;

    [Header("Input")]
    [SerializeField] private InputActionReference navigateAction;
    [SerializeField] private InputActionReference submitAction;

    [Header("Navigation")]
    [SerializeField] private float directionThreshold = 0.35f;
    [SerializeField] private float repeatDelay = 0.35f;
    [SerializeField] private float repeatRate = 0.12f;

    [Header("Direction Search")]
    [SerializeField] private float directionWeight = 2f;
    [SerializeField] private float distanceWeight = 1f;

    [Header("Mouse")]
    [SerializeField] private float mouseMoveThreshold = 0.01f;

    private SC_Button[] buttons;

    private int currentIndex = -1;
    private int lastSelectedIndex = -1;

    private float nextRepeatTime;
    private Vector2 lastInput;

    // True = clavier/manette
    // False = souris
    private bool navigationMode = false;
    public static SC_ConstellationNavigation instance;
    private void Awake()
    {
        instance = this;
        FindButtons();
    }

    private void OnEnable()
    {
        if (navigateAction != null)
            navigateAction.action.Enable();

        if (submitAction != null)
            submitAction.action.Enable();

        SelectDefaultButton();
    }

    private void OnDisable()
    {
        if (navigateAction != null)
            navigateAction.action.Disable();

        if (submitAction != null)
            submitAction.action.Disable();
    }

    private void Update()
    {
        HandleMouse();
        HandleNavigation();
        HandleSubmit();
    }

    // ============================================================
    // FIND BUTTONS AUTOMATICALLY
    // ============================================================

    private void FindButtons()
    {
        buttons = GetComponentsInChildren<SC_Button>(true);

        Debug.Log(
            $"[SC_ConstellationNavigation] {buttons.Length} boutons trouvés."
        );
    }

    // ============================================================
    // MOUSE
    // ============================================================

    private void HandleMouse()
    {
        if (Mouse.current == null)
            return;

        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        if (mouseDelta.magnitude < mouseMoveThreshold)
            return;

        // La souris reprend le contrôle
        navigationMode = false;

        if (SC_controller_manager.instance != null)
        {
            SC_controller_manager.instance.using_controller = false;
        }

        // Rien à désélectionner
        if (currentIndex < 0)
            return;

        // Mémorise le bouton actuel
        lastSelectedIndex = currentIndex;

        if (buttons != null &&
            currentIndex < buttons.Length &&
            buttons[currentIndex] != null)
        {
            buttons[currentIndex].UnSelect();
        }

        currentIndex = -1;
    }

    // ============================================================
    // NAVIGATION
    // ============================================================

    private void HandleNavigation()
    {
        if (navigateAction == null)
            return;

        Vector2 input = navigateAction.action.ReadValue<Vector2>();

        if (input.magnitude < directionThreshold)
        {
            lastInput = Vector2.zero;
            return;
        }

        // Le clavier/manette reprend le contrôle
        navigationMode = true;

        if (SC_controller_manager.instance != null)
        {
            SC_controller_manager.instance.using_controller = true;
        }

        input.Normalize();

        // --------------------------------------------------------
        // On vient de passer de la souris au clavier/manette
        // --------------------------------------------------------

        if (currentIndex < 0)
        {
            if (lastSelectedIndex >= 0 &&
                lastSelectedIndex < buttons.Length &&
                buttons[lastSelectedIndex] != null &&
                buttons[lastSelectedIndex].clickable)
            {
                SelectButton(lastSelectedIndex);
            }
            else
            {
                SelectDefaultButton();
            }

            lastInput = input;
            nextRepeatTime = Time.unscaledTime + repeatDelay;

            return;
        }

        // --------------------------------------------------------
        // Première pression
        // --------------------------------------------------------

        if (lastInput == Vector2.zero)
        {
            Move(input);

            lastInput = input;
            nextRepeatTime = Time.unscaledTime + repeatDelay;

            return;
        }

        // --------------------------------------------------------
        // Répétition quand on maintient
        // --------------------------------------------------------

        if (Time.unscaledTime >= nextRepeatTime)
        {
            Move(input);

            nextRepeatTime = Time.unscaledTime + repeatRate;
        }

        lastInput = input;
    }

    // ============================================================
    // MOVE
    // ============================================================

    private void Move(Vector2 direction)
    {
        if (currentIndex < 0)
        {
            if (lastSelectedIndex >= 0 &&
                lastSelectedIndex < buttons.Length)
            {
                SelectButton(lastSelectedIndex);
            }
            else
            {
                SelectDefaultButton();
            }

            return;
        }

        SC_Button currentButton = buttons[currentIndex];

        if (currentButton == null)
            return;

        Vector2 currentPosition = currentButton.transform.position;

        int bestIndex = FindBestButton(
            currentPosition,
            direction,
            currentIndex
        );

        if (bestIndex != -1)
        {
            SelectButton(bestIndex);
        }
    }

    // ============================================================
    // FIND BEST BUTTON
    // ============================================================

    private int FindBestButton(
        Vector2 origin,
        Vector2 direction,
        int ignoredIndex)
    {
        int bestIndex = -1;
        float bestScore = float.MaxValue;

        for (int i = 0; i < buttons.Length; i++)
        {
            if (i == ignoredIndex)
                continue;

            SC_Button button = buttons[i];

            if (button == null)
                continue;

            if (!button.gameObject.activeInHierarchy)
                continue;

            if (!button.clickable)
                continue;

            Vector2 offset =
                (Vector2)button.transform.position - origin;

            float distance = offset.magnitude;

            if (distance <= 0.001f)
                continue;

            Vector2 toButton = offset.normalized;

            float dot = Vector2.Dot(
                direction,
                toButton
            );

            // Ignore les boutons trop éloignés de la direction
            if (dot < directionThreshold)
                continue;

            float directionScore = 1f - dot;
            float distanceScore = distance;

            float score =
                directionScore * directionWeight +
                distanceScore * distanceWeight;

            if (score < bestScore)
            {
                bestScore = score;
                bestIndex = i;
            }
        }

        return bestIndex;
    }

    // ============================================================
    // SELECT
    // ============================================================

    private void SelectButton(int index)
    {
        if (buttons == null)
            return;

        if (index < 0 || index >= buttons.Length)
            return;

        if (buttons[index] == null)
            return;

        if (!buttons[index].clickable)
            return;

        // Désélectionne l'ancien bouton
        if (currentIndex >= 0 &&
            currentIndex < buttons.Length &&
            buttons[currentIndex] != null)
        {
            buttons[currentIndex].UnSelect();
        }

        currentIndex = index;

        // Mémorise toujours le dernier bouton
        lastSelectedIndex = index;

        // Sélectionne le nouveau
        buttons[currentIndex].Select();
    }

    // ============================================================
    // DEFAULT
    // ============================================================

    private void SelectDefaultButton()
    {
        if (buttons == null || buttons.Length == 0)
        {
            Debug.LogWarning(
                "[SC_ConstellationNavigation] Aucun SC_Button trouvé."
            );

            return;
        }

        // Priorité au dernier bouton sélectionné
        if (lastSelectedIndex >= 0 &&
            lastSelectedIndex < buttons.Length &&
            buttons[lastSelectedIndex] != null &&
            buttons[lastSelectedIndex].gameObject.activeInHierarchy &&
            buttons[lastSelectedIndex].clickable)
        {
            SelectButton(lastSelectedIndex);
            return;
        }

        // Sinon, utilise le bouton par défaut configuré dans l'Inspector
        int index = Mathf.Clamp(
            defaultSelectedButton,
            0,
            buttons.Length - 1
        );

        // Cherche le premier bouton valide à partir de l'index
        for (int i = index; i < buttons.Length; i++)
        {
            if (buttons[i] != null &&
                buttons[i].gameObject.activeInHierarchy &&
                buttons[i].clickable)
            {
                SelectButton(i);
                return;
            }
        }

        Debug.LogWarning(
            "[SC_ConstellationNavigation] Aucun bouton cliquable trouvé."
        );
    }
    // ============================================================
    // SUBMIT
    // ============================================================

    private void HandleSubmit()
    {
        if (submitAction == null)
            return;

        if (!submitAction.action.WasPressedThisFrame())
            return;

        // Le submit force le mode navigation
        navigationMode = true;

        if (SC_controller_manager.instance != null)
        {
            SC_controller_manager.instance.using_controller = true;
        }

        if (currentIndex < 0)
        {
            if (lastSelectedIndex >= 0 &&
                lastSelectedIndex < buttons.Length)
            {
                SelectButton(lastSelectedIndex);
            }
            else
            {
                SelectDefaultButton();
            }
        }

        if (currentIndex < 0 ||
            currentIndex >= buttons.Length)
            return;

        SC_Button button = buttons[currentIndex];

        if (button == null)
            return;

        if (!button.clickable)
            return;

        button.Press();
    }

    // ============================================================
    // PUBLIC
    // ============================================================

    public SC_Button GetSelectedButton()
    {
        if (currentIndex < 0 ||
            currentIndex >= buttons.Length)
        {
            return null;
        }

        return buttons[currentIndex];
    }

    public void RefreshButtons()
    {
        FindButtons();

        if (currentIndex >= buttons.Length)
            currentIndex = -1;

        if (lastSelectedIndex >= buttons.Length)
            lastSelectedIndex = -1;

        if (currentIndex == -1 &&
            lastSelectedIndex == -1)
        {
            SelectDefaultButton();
        }
    }

    public bool IsNavigationMode()
    {
        return navigationMode;
    }
}
