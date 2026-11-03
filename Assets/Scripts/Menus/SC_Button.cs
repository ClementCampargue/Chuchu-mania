
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
public class SC_Button : MonoBehaviour
{
    public bool IsControllerSelected => isSelected && !isHovered;
    public static bool IsControllerNavigation { get; private set; }

    public bool select_on_enable;

    [Header("Animations")]
    public string hover;
    public string unhover;
    public string press;

    [Header("Events")]
    public UnityEvent onClick;
    public UnityEvent onHover;
    public UnityEvent onUnhover;

    [Header("States")]
    public bool isSelected;
    public bool isHovered;
    public bool isPressed;

    private Collider2D collider_;

    [Header("References")]
    public GameObject indicator;
    public SC_juiciness juice;
    public SC_juiciness juice2;
    public SC_menu_navigation navigation;

    [Header("Settings")]
    public bool accept_input = true;
    public bool clickable = true;

    private Animator anim;

    private static SC_Button activeButton;

    private bool mouseInside;
    private bool previousController;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        collider_ = GetComponent<Collider2D>();

        previousController = IsUsingController();

        UpdateIndicator();
    }

    private void Start()
    {
        if (select_on_enable && clickable)
        {
            Select();
        }
    }

    private void OnEnable()
    {
        if (collider_ != null)
            collider_.enabled = true;

        if (select_on_enable && IsUsingController() && clickable)
            Select();
    }

    private void Update()
    {
        bool usingController = IsUsingController();

        // =================================
        // CHANGEMENT DE MODE
        // =================================

        if (usingController && !previousController)
        {
            previousController = true;

            if (activeButton != null && activeButton.isHovered)
            {
                SC_Button hoveredButton = activeButton;

                hoveredButton.isHovered = false;
                hoveredButton.isSelected = false;

                hoveredButton.ForceSelect();
            }
            else if (isHovered)
            {
                isHovered = false;
                Select();
            }

            mouseInside = false;
            UpdateIndicator();
        }
        else if (!usingController && previousController)
        {
            previousController = false;
            mouseInside = false;
        }

        // =================================
        // ÉTAT DU BOUTON
        // =================================

        if ((!isActiveAndEnabled || !clickable) && activeButton == this)
        {
            ReleaseActiveButton();
        }

        if (activeButton != this && (isSelected || isHovered))
        {
            isSelected = false;
            isHovered = false;
            UpdateIndicator();
        }

        // =================================
        // DÉTECTION SOURIS
        // =================================

        if (!usingController)
        {
            UpdateMouseDetection();
        }
    }

    // =================================
    // DÉTECTION DU CURSEUR
    // =================================


    private void UpdateMouseDetection()
    {
        if (SC_scursorManager.instance == null || collider_ == null)
            return;

        if (!collider_.enabled || !clickable || !accept_input)
        {
            if (mouseInside)
                HandleMouseExit();

            return;
        }

        Vector2 cursorPosition =
            SC_scursorManager.instance.transform.position;

        Camera cam = Camera.main;

        if (cam == null)
            return;

        // Position écran du collider.
        Vector3 colliderScreenPosition =
            cam.WorldToScreenPoint(collider_.bounds.center);

        // Conversion du curseur à la profondeur exacte du collider.
        Vector3 cursorScreenPosition = new Vector3(
            cursorPosition.x,
            cursorPosition.y,
            colliderScreenPosition.z
        );

        Vector3 cursorWorldPosition =
            cam.ScreenToWorldPoint(cursorScreenPosition);

        bool inside = collider_.OverlapPoint(cursorWorldPosition);

        if (inside && !mouseInside)
        {
            HandleMouseEnter();
        }
        else if (!inside && mouseInside)
        {
            HandleMouseExit();
        }

        mouseInside = inside;

        if (inside)
        {
            if (MousePressedThisFrame())
                PressAnimation();

            if (MouseReleasedThisFrame() && isHovered)
            {
                Click();
                isPressed = false;
            }
        }
    }

    private bool MousePressedThisFrame()
    {
        return Mouse.current != null &&
               Mouse.current.leftButton.wasPressedThisFrame;
    }

    private bool MouseReleasedThisFrame()
    {
        return Mouse.current != null &&
               Mouse.current.leftButton.wasReleasedThisFrame;
    }

    // =================================
    // ENTRÉE / SORTIE SOURIS
    // =================================

    private void HandleMouseEnter()
    {
        if (IsUsingController())
            return;

        IsControllerNavigation = false;

        Activate();

        isHovered = true;
        isSelected = false;
        isPressed = false;

        if (juice != null)
            juice.PlayJuice();

        Hover();

        UpdateIndicator();
        PlayAnimation(hover);
    }

    private void HandleMouseExit()
    {
        if (IsUsingController())
            return;

        if (!isHovered)
            return;

        isHovered = false;

        OnUnhover();

        if (activeButton == this)
            activeButton = null;

        UpdateIndicator();

        if (!isSelected)
            PlayAnimation(unhover);
    }

    // =================================
    // CONTRÔLEUR
    // =================================

    public static SC_Button GetActiveButton()
    {
        return activeButton;
    }

    private bool IsUsingController()
    {
        return SC_controller_manager.instance != null &&
               SC_controller_manager.instance.using_controller;
    }

    public void Select()
    {
        if (!clickable || !accept_input || !isActiveAndEnabled)
            return;

        IsControllerNavigation = true;

        Activate();

        if (juice != null)
            juice.PlayJuice();

        isSelected = true;
        isHovered = false;
        mouseInside = false;

        UpdateIndicator();

        Hover();
        PlayAnimation(hover);
    }

    public void ForceSelect()
    {
        IsControllerNavigation = true;

        Activate();

        if (juice != null)
            juice.PlayJuice();

        isSelected = true;
        isHovered = false;
        mouseInside = false;

        UpdateIndicator();

        Hover();
        PlayAnimation(hover);
    }

    public void UnSelect()
    {
        if (!isSelected && activeButton != this)
            return;

        isSelected = false;

        if (!isHovered)
        {
            if (activeButton == this)
                activeButton = null;

            OnUnhover();
            PlayAnimation(unhover);
        }

        UpdateIndicator();
    }

    // =================================
    // BOUTON ACTIF
    // =================================

    private void Activate()
    {
        if (activeButton != null && activeButton != this)
        {
            activeButton.ClearActiveState();
        }

        activeButton = this;
    }

    private void ClearActiveState()
    {
        bool wasActive = isSelected || isHovered;

        isSelected = false;
        isHovered = false;
        mouseInside = false;

        if (wasActive)
        {
            OnUnhover();
            PlayAnimation(unhover);
        }

        UpdateIndicator();
    }

    private void ReleaseActiveButton()
    {
        if (activeButton == this)
            activeButton = null;

        ClearActiveState();
    }

    // =================================
    // CLIC
    // =================================

    public void Press()
    {
        if (!clickable || !accept_input)
            return;

        Select();
        PressAnimation();
        Click();
    }

    private void PressAnimation()
    {
        if (isPressed)
            return;

        isPressed = true;

        if (juice2 != null)
            juice2.PlayJuice();

        PlayAnimation(press);

        CancelInvoke(nameof(ResetPressed));
        Invoke(nameof(ResetPressed), 0.5f);
    }

    private void ResetPressed()
    {
        isPressed = false;
    }

    public void Click()
    {
        if (!clickable || !accept_input)
            return;

        onClick?.Invoke();
    }

    public void Hover()
    {
        onHover?.Invoke();
    }

    public void OnUnhover()
    {
        onUnhover?.Invoke();
    }

    // =================================
    // INDICATEUR
    // =================================

    private void UpdateIndicator()
    {
        if (indicator != null)
            indicator.SetActive(isSelected || isHovered);
    }

    // =================================
    // ANIMATIONS
    // =================================

    public void PlayAnimation(string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger))
            return;

        anim.enabled = true;

        if (!string.IsNullOrEmpty(hover))
            anim.ResetTrigger(hover);

        if (!string.IsNullOrEmpty(unhover))
            anim.ResetTrigger(unhover);

        if (!string.IsNullOrEmpty(press))
            anim.ResetTrigger(press);

        anim.SetTrigger(trigger);
    }

    // =================================
    // DÉSACTIVATION
    // =================================

    private void OnDisable()
    {
        CancelInvoke();

        if (collider_ != null)
            collider_.enabled = false;

        if (activeButton == this)
            activeButton = null;

        isSelected = false;
        isHovered = false;
        isPressed = false;
        mouseInside = false;

        UpdateIndicator();
    }

    private void OnDestroy()
    {
        if (activeButton == this)
            activeButton = null;
    }

    // =================================
    // GIZMOS
    // =================================

    private void OnDrawGizmosSelected()
    {
        Collider2D col = GetComponent<Collider2D>();

        if (col != null)
        {
            Gizmos.color = Color.green;

            Gizmos.DrawWireCube(
                col.bounds.center,
                col.bounds.size
            );
        }
    }
}
