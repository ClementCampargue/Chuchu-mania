
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class SC_slider_ : MonoBehaviour
{
    [Header("Events")]
    public UnityEvent event_;

    [Header("Audio")]
    public AudioSource audio;
    public List<AudioClip> audios;
    public bool play_random_audio;
    public AudioClip juice;

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;
    [SerializeField] private InputActionReference resetAction;

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

    [Header("Camera")]
    [SerializeField] private Camera cam;

    public bool can_move;

    public float Value => value;

    public SC_Button button;

    private int lastPlayedStep = -1;

    private bool mouseIsDragging;
    private int mouseStartStep = -1;

    // --------------------------------------------------
    // INITIALISATION
    // --------------------------------------------------

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        if (cam == null)
            cam = Camera.main;

        UpdateCursor();

        lastPlayedStep = GetCurrentStep(value);
    }

    // --------------------------------------------------
    // ACTIVATION
    // --------------------------------------------------

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

    public void OnMousePress()
    {
        mouseIsDragging = true;
        mouseStartStep = GetCurrentStep(value);
    }

    public void SetValueFromMouse(
        Vector3 mousePosition,
        Camera cameraToUse
    )
    {
        Camera currentCam = cameraToUse != null
            ? cameraToUse
            : cam;

        if (cursor == null ||
            pointMin == null ||
            pointMax == null ||
            currentCam == null)
            return;

        Vector3 minScreen = currentCam.WorldToScreenPoint(
            pointMin.position
        );

        Vector3 maxScreen = currentCam.WorldToScreenPoint(
            pointMax.position
        );

        Vector2 axis = (Vector2)maxScreen - (Vector2)minScreen;

        float lengthSquared = axis.sqrMagnitude;

        if (lengthSquared < 0.0001f)
            return;

        float newValue = Vector2.Dot(
            (Vector2)mousePosition - (Vector2)minScreen,
            axis
        ) / lengthSquared;

        newValue = Mathf.Clamp01(newValue);

        if (!mouseIsDragging)
            OnMousePress();

        value = newValue;

        UpdateCursor();
    }

    public void OnMouseRelease()
    {
        if (!mouseIsDragging)
            return;

        int finalStep = GetCurrentStep(value);

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

        if (resetAction.action.WasPressedThisFrame() && can_move)
        {
            value = 1;
            UpdateCursor();
        }
        if (moveAction == null ||
            moveAction.action == null)
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
        if (audio == null)
            return;

        if (play_random_audio)
        {
            if (audios == null || audios.Count == 0)
                return;

            List<AudioClip> validAudios = audios.FindAll(
                clip => clip != null
            );

            if (validAudios.Count == 0)
                return;

            int randomIndex = Random.Range(
                0,
                validAudios.Count
            );

            audio.Stop();
            audio.clip = validAudios[randomIndex];
            audio.enabled = true;
            audio.Play();
        }
        else
        {
            if (juice == null)
                return;

            audio.enabled = true;
            audio.PlayOneShot(juice);
        }
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
    // CURSEUR
    // --------------------------------------------------

    private void UpdateCursor()
    {
        if (cursor == null ||
            pointMin == null ||
            pointMax == null)
            return;

        float displayValue = value;

        if (segmented)
        {
            int steps = Mathf.Max(2, numberOfSteps);

            displayValue = Mathf.Round(
                value * (steps - 1)
            ) / (steps - 1);
        }

        cursor.transform.position = Vector3.Lerp(
            pointMin.position,
            pointMax.position,
            displayValue
        );

        event_?.Invoke();
    }

    // --------------------------------------------------
    // VALEUR EXTERNE
    // --------------------------------------------------

    public void SetValue(float newValue)
    {
        float oldValue = value;

        value = Mathf.Clamp01(newValue);

        if (segmented)
        {
            int oldStep = GetCurrentStep(oldValue);
            int newStep = GetCurrentStep(value);

            if (oldStep != newStep &&
                newStep != lastPlayedStep)
            {
                lastPlayedStep = newStep;
                PlayRandom();
            }
        }

        UpdateCursor();
    }
}
