using UnityEngine;
using UnityEngine.Events;

[RequireComponent(typeof(Collider2D))]
[RequireComponent(typeof(Animator))]
public class SC_Button : MonoBehaviour
{
    [Header("Anims")]
    public string hover;
    public string unhover;
    public string press;

    [Header("Events")]
    public UnityEvent onClick;

    private SpriteRenderer spriteRenderer;
    public bool isHovered;
    private bool isPressed;

    private Animator anim;
    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();

        anim = GetComponent<Animator>();
    }
    public void Select()
    {
        isHovered = true;
        anim.SetTrigger(hover);
    }
    public void UnSelect()
    {
        isHovered = true;
        anim.SetTrigger(unhover);
    }
    public void Press()
    {
        isPressed = true;
        anim.SetTrigger(press);
        onClick?.Invoke();
    }
    private void OnMouseEnter()
    {
        Select();
    }

    private void OnMouseExit()
    {
        UnSelect();
    }

    private void OnMouseDown()
    {
        isPressed = true;
        anim.SetTrigger(press);
    }

    private void OnMouseUp()
    {
        // Si la souris est toujours sur le bouton,
        // on considère que c'est un clic valide.
        if (isHovered)
        {
            onClick?.Invoke();
        }
    }
}