using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SC_slider_ : MonoBehaviour
{
    public UnityEvent event_;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Curseur")]
    [SerializeField] private SpriteRenderer cursor;
    [SerializeField] private Transform pointMin;
    [SerializeField] private Transform pointMax;

    [Header("Valeur")]
    [Range(0f, 1f)]
    [SerializeField] private float value = 0f;

    [Header("Segmentation")]
    [SerializeField] private bool segmented = false;
    [Min(2)]
    [SerializeField] private int numberOfSteps = 5;

    [Header("Vitesse")]
    [Min(0f)]
    [SerializeField] private float speed = 1f;

    public bool can_move;
    public float Value => value;

    public SC_Button button;

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        UpdateCursor();
    }



    public void _eneable()
    {
        can_move = false;

    }
    public void SetValueFromMouse(Vector3 mousePosition, Camera cam)
    {
        if (cursor == null || pointMin == null || pointMax == null || cam == null)
            return;

        // Convertit la souris en position monde.
        Vector3 worldMouse = cam.ScreenToWorldPoint(
            new Vector3(mousePosition.x, mousePosition.y,
            Mathf.Abs(cam.transform.position.z - transform.position.z))
        );

        // Axe de déplacement du curseur.
        Vector3 min = pointMin.position;
        Vector3 max = pointMax.position;
        Vector3 axis = max - min;

        float lengthSquared = axis.sqrMagnitude;

        if (lengthSquared < 0.0001f)
            return;

        // Projection de la souris sur l'axe du slider.
        float newValue = Vector3.Dot(worldMouse - min, axis) / lengthSquared;

        SetValue(Mathf.Clamp01(newValue));
    }


    public void disable()
    {
        can_move = true;

    }

private void Update()
{
    if (moveAction == null || moveAction.action == null)
        return;

    if (!can_move)
        return;

    if (!button.isSelected && !button.isHovered)
            return;

    float input = moveAction.action.ReadValue<Vector2>().x;

    if (Mathf.Abs(input) > 0.001f)
    {
        value += input * speed * Time.unscaledDeltaTime;
        value = Mathf.Clamp01(value);

        UpdateCursor();
    }
}


    private void UpdateCursor()
    {
        if (cursor == null || pointMin == null || pointMax == null)
            return;

        float displayValue = value;
        event_.Invoke();

        if (segmented)
        {
            int steps = Mathf.Max(2, numberOfSteps);

            displayValue = Mathf.Round(value * (steps - 1))
                         / (steps - 1);
        }

        Vector3 position = Vector3.Lerp(
            pointMin.position,
            pointMax.position,
            displayValue
        );

        cursor.transform.position = position;
    }


    public void SetValue(float newValue)
    {
        value = Mathf.Clamp01(newValue);
        UpdateCursor();
    }
}