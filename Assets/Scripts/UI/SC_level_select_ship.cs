
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_level_select_ship : MonoBehaviour
{
    public SC_Button planet;

    [Header("Input")]
    public InputActionReference move;
    public InputActionReference confirm;

    [Header("Movement")]
    public float acceleration = 8f;
    public float maxSpeed = 5f;
    public float friction = 2f;

    private Vector2 velocity;

    public Vector3 origin;

    private void Start()
    {
        origin = transform.position;
    }
    private void OnEnable()
    {
        move.action.Enable();
        confirm.action.Enable();
    }

  

    void Update()
    {
        if (move == null || move.action == null)
        {
            Debug.LogError("Move n'est pas assigné !");
            return;
        }

        Vector2 input = move.action.ReadValue<Vector2>();

        if (input.sqrMagnitude > 0.01f)
        {
            velocity += input * acceleration * Time.unscaledDeltaTime;
            velocity = Vector2.ClampMagnitude(velocity, maxSpeed);
        }
        else
        {
            velocity = Vector2.MoveTowards(
                velocity,
                Vector2.zero,
                friction * Time.unscaledDeltaTime
            );
        }

        transform.position += (Vector3)(velocity * Time.unscaledDeltaTime);

        // Diagnostic temporaire
        if (input.sqrMagnitude > 0.01f)
        {
            Debug.Log("Input : " + input + " | Vitesse : " + velocity);
        }

        if (confirm != null &&
            confirm.action != null &&
            confirm.action.WasPressedThisFrame() &&
            planet != null)
        {
            planet.Click();
            planet.Press();
        }
    }



    private void OnTriggerEnter2D(Collider2D collision)
    {
        SC_Button button = collision.GetComponent<SC_Button>();

        if (button != null)
            planet = button;
        planet.Hover();
        planet.Select();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        SC_Button button = collision.GetComponent<SC_Button>();
        planet.OnUnhover();
        planet.UnSelect();

        if (button != null && planet == button)
            planet = null;
    }
}
