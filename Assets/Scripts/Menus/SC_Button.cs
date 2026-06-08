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

    [Header("Interaction")]
    public Camera interactionCamera;

    private Animator anim;
    private Collider2D col;

    private bool isHovered;
    private bool isPressed;

    private void Awake()
    {
        anim = GetComponent<Animator>();
        col = GetComponent<Collider2D>();

        if (interactionCamera == null)
            interactionCamera = Camera.main;
    }

    private void Update()
    {
        if (interactionCamera == null)
            return;

        // Position de la souris en coordonnées monde
        Vector3 mousePos = Input.mousePosition;
        Vector3 worldPos = interactionCamera.ScreenToWorldPoint(mousePos);
        worldPos.z = 0f;

        // Raycast / détection du collider sous la souris
        Collider2D hit = Physics2D.OverlapPoint(worldPos);

        bool currentlyHovered = hit == col;

        // Hover Enter
        if (currentlyHovered && !isHovered)
        {
            isHovered = true;

            if (!string.IsNullOrEmpty(hover))
                anim.SetTrigger(hover);
        }

        // Hover Exit
        if (!currentlyHovered && isHovered)
        {
            isHovered = false;

            if (!string.IsNullOrEmpty(unhover))
                anim.SetTrigger(unhover);
        }

        // Mouse Down
        if (isHovered && Input.GetMouseButtonDown(0))
        {
            isPressed = true;

            if (!string.IsNullOrEmpty(press))
                anim.SetTrigger(press);
        }

        // Mouse Up
        if (isPressed && Input.GetMouseButtonUp(0))
        {
            isPressed = false;

            // Clic valide uniquement si la souris est toujours dessus
            if (isHovered)
            {
                onClick?.Invoke();

                if (!string.IsNullOrEmpty(hover))
                    anim.SetTrigger(hover);
            }
            else
            {
                if (!string.IsNullOrEmpty(unhover))
                    anim.SetTrigger(unhover);
            }
        }
    }
}