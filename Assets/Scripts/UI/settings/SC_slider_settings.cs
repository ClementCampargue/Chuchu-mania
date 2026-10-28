
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SC_slider_ : MonoBehaviour
{
    public UnityEvent event_;
    public AudioSource audio;
    public List<AudioClip> audios;
    public bool play_random_audio;
    public AudioClip juice;
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

    private int lastPlayedStep = -1;

    // Gestion de la souris
    private bool mouseIsDragging;
    private int mouseStartStep = -1;

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        UpdateCursor();
        lastPlayedStep = GetCurrentStep(value);
    }

    public void _eneable()
    {
        can_move = false;
    }

    public void disable()
    {
        can_move = true;
    }

    // --------------------------------------------------
    // SOURIS
    // --------------------------------------------------

    // Appeler cette méthode au début du clic/drag.
    public void OnMousePress()
    {
        mouseIsDragging = true;
        mouseStartStep = GetCurrentStep(value);
    }

    // Appeler cette méthode pendant le déplacement.
    public void SetValueFromMouse(Vector3 mousePosition, Camera cam)
    {
        if (cursor == null || pointMin == null ||
            pointMax == null || cam == null)
            return;

        Vector3 worldMouse = cam.ScreenToWorldPoint(
            new Vector3(
                mousePosition.x,
                mousePosition.y,
                Mathf.Abs(cam.transform.position.z - transform.position.z)
            )
        );

        Vector3 min = pointMin.position;
        Vector3 axis = pointMax.position - min;

        float lengthSquared = axis.sqrMagnitude;

        if (lengthSquared < 0.0001f)
            return;

        float newValue = Vector3.Dot(worldMouse - min, axis)
                         / lengthSquared;

        newValue = Mathf.Clamp01(newValue);

        // Si le drag n'a pas été initialisé, on l'initialise.
        if (!mouseIsDragging)
            OnMousePress();

        // Mise à jour visuelle sans jouer de son pendant le drag.
        value = newValue;
        UpdateCursor();
    }

    // Appeler une seule fois au relâchement du clic.
    public void OnMouseRelease()
    {
        if (!mouseIsDragging)
            return;

        int finalStep = GetCurrentStep(value);

        // Un son seulement si on a terminé sur un autre snap.
        if (finalStep != mouseStartStep)
        {
            lastPlayedStep = finalStep;
        }

        mouseIsDragging = false;
        mouseStartStep = -1;
    }

    // --------------------------------------------------
    // MANETTE / CLAVIER
    // --------------------------------------------------

    private void Update()
    {
        if (moveAction == null || moveAction.action == null)
            return;

        if (!can_move)
            return;

        if (button == null ||
            (!button.isSelected && !button.isHovered))
            return;

        float input = moveAction.action.ReadValue<Vector2>().x;

        if (Mathf.Abs(input) < 0.001f)
            return;

        float oldValue = value;

        value = Mathf.Clamp01(
            value + input * speed * Time.unscaledDeltaTime
        );

        int oldStep = GetCurrentStep(oldValue);
        int newStep = GetCurrentStep(value);

        if (newStep != oldStep)
        {
            if (newStep != lastPlayedStep)
            {
                lastPlayedStep = newStep;
                PlayRandom();
            }
        }

        UpdateCursor();
    }

    // --------------------------------------------------
    // SONS
    // --------------------------------------------------

    public void PlayRandom()
    {
        if (audio == null ||
            audios == null || audios.Count == 0)
            return;

        List<AudioClip> validAudios = audios.FindAll(
            clip => clip != null
        );

        if (validAudios.Count == 0)
            return;

        int randomIndex = Random.Range(0, validAudios.Count);


        if (play_random_audio)
        {
            audio.Stop();
            audio.clip = validAudios[randomIndex];

            audio.Play();
        }
        else
        {
            audio.clip = juice;
            audio.PlayOneShot(juice);
        }
        audio.enabled = true;
    }

    // --------------------------------------------------
    // SNAPS
    // --------------------------------------------------

    private int GetCurrentStep(float val)
    {
        int steps = Mathf.Max(2, numberOfSteps);

        return Mathf.RoundToInt(
            Mathf.Clamp01(val) * (steps - 1)
        );
    }

    // --------------------------------------------------
    // CURSEUR / VALEUR
    // --------------------------------------------------

    private void UpdateCursor()
    {
        if (cursor == null || pointMin == null || pointMax == null)
            return;

        float displayValue = value;

        if (segmented)
        {
            int steps = Mathf.Max(2, numberOfSteps);

            displayValue = Mathf.Round(value * (steps - 1))
                         / (steps - 1);
        }

        cursor.transform.position = Vector3.Lerp(
            pointMin.position,
            pointMax.position,
            displayValue
        );

        event_?.Invoke();
    }

    public void SetValue(float newValue)
    {
        float oldValue = value;

        value = Mathf.Clamp01(newValue);

        if (segmented)
        {
            int oldStep = GetCurrentStep(oldValue);
            int newStep = GetCurrentStep(value);

            if (oldStep != newStep && newStep != lastPlayedStep)
            {
                lastPlayedStep = newStep;
                PlayRandom();
            }
        }

        UpdateCursor();
    }
}
