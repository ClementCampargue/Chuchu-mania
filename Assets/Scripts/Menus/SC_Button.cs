using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
public class SC_Button : MonoBehaviour
{
    [Header("Animations")]
    public string hover;
    public string unhover;
    public string press;

    [Header("Events")]
    public UnityEvent onClick;

    private Animator anim;

    private bool isSelected;   // Navigation clavier/manette
    public bool isHovered;    // Souris
    private bool isPressed;
    public GameObject indicator;

    private void Awake()
    {
        anim = GetComponent<Animator>();
    }


    // Appelé par EventSystem Select()
    public void Select()
    {
        if (isSelected)
            return;

        isSelected = true;
        indicator.SetActive(true);
        PlayAnimation(hover);
    }

    private void OnDisable()
    {
        indicator.SetActive(false);
        isSelected = false;
        isHovered = false;
    }

    // Appelé par EventSystem Deselect()
    public void UnSelect()
    {
        if (!isSelected)
            return;


        isSelected = false;

        // Si la souris est dessus, on garde l'état hover
        if (!isHovered)
        {
            PlayAnimation(unhover);
            indicator.SetActive(false);
        }
    }


    private void OnMouseEnter()
    {
        isPressed = false;
        isHovered = true;
        indicator.SetActive(true);

        PlayAnimation(hover);
    }


    private void OnMouseExit()
    {
        isHovered = false;
        indicator.SetActive(false);

        // Si la navigation garde le bouton sélectionné,
        // on ne joue pas l'unhover
        if (!isSelected)
        {
            PlayAnimation(unhover);
        }
    }


    private void OnMouseDown()
    {
        PressAnimation();
    }


    private void OnMouseUp()
    {
        // Clic valide seulement si on relâche sur le bouton
        if (isHovered)
        {
            Click();
        }
        indicator.SetActive(false);

        isPressed = false;
    }


    public void Press()
    {
        PressAnimation();
        Click();
    }


    private void PressAnimation()
    {
        if (isPressed)
            return;

        isPressed = true;
        indicator.SetActive(false);

        PlayAnimation(press);
    }


    private void Click()
    {
        onClick?.Invoke();
    }


    private void PlayAnimation(string trigger)
    {
        if (anim == null || string.IsNullOrEmpty(trigger))
            return;

        anim.ResetTrigger(trigger);
        anim.SetTrigger(trigger);
    }
}