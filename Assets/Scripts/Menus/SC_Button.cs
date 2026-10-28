
using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SC_Button : MonoBehaviour
{
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

    [Header("References")]
    public GameObject indicator;
    public SC_juiciness juice;
    public SC_juiciness juice2;
    public SC_menu_navigation navigation;

    [Header("Settings")]
    public bool accept_input = true;
    public bool clickable = true;

    private Animator anim;

    // Un seul bouton actif pour tout le menu.
    private static SC_Button activeButton;

    private bool mouseInside;
    private bool wasController;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        previousController = IsUsingController();
        wasController = previousController;
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
        if (select_on_enable && IsUsingController() && clickable)
        {
            Select();
        }
    }


    private bool previousController;

    private void Update()
    {
        bool usingController = IsUsingController();

        // Passage de la souris à la manette.
        if (usingController && !previousController)
        {
            previousController = true;

            // Le bouton survolé devient la sélection de départ.
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
        // Passage de la manette à la souris.
        else if (!usingController && previousController)
        {
            previousController = false;

            // On conserve la sélection actuelle jusqu'au
            // prochain survol réel de la souris.
            mouseInside = false;
        }

        // Un seul bouton actif à la fois.
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
    }

    private bool IsUsingController()
    {
        return SC_controller_manager.instance != null &&
               SC_controller_manager.instance.using_controller;
    }

    // Sélection clavier/manette.
    public void Select()
    {
        if (!clickable || !accept_input || !isActiveAndEnabled)
            return;

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

    // Désélection clavier/manette.
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

    private void OnMouseEnter()
    {
        if (IsUsingController())
            return;

        mouseInside = true;

        if (!clickable || !accept_input)
            return;

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

    private void OnMouseExit()
    {
        mouseInside = false;

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

    private void OnMouseDown()
    {
        if (IsUsingController())
            return;

        if (!clickable || !accept_input || !isHovered)
            return;

        PressAnimation();
    }

    private void OnMouseUp()
    {
        if (IsUsingController())
            return;

        if (!clickable || !accept_input)
            return;

        if (isHovered && mouseInside)
        {
            Click();
        }

        isPressed = false;
    }

    // Appel externe, par exemple depuis la navigation.
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

    private void UpdateIndicator()
    {
        if (indicator != null)
        {
            indicator.SetActive(isSelected || isHovered);
        }
    }

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

    private void OnDisable()
    {
        CancelInvoke();

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
