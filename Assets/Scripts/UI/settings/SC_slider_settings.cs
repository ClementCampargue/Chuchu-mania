using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

[RequireComponent(typeof(Collider2D))]
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

    [Header("References")]
    public SC_Button button;

    [Header("Settings")]
    public bool can_move;

    public float Value => value;

    private Collider2D sliderCollider;
    private int lastPlayedStep = -1;

    private bool mouseIsDragging;
    private int mouseStartStep = -1;

    private void Awake()
    {
        sliderCollider = GetComponent<Collider2D>();
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Enable();

        if (resetAction != null && resetAction.action != null)
            resetAction.action.Enable();

        if (cam == null)
            cam = Camera.main;

        UpdateCursor();
        lastPlayedStep = GetCurrentStep(value);
    }

    private void OnDisable()
    {
        if (moveAction != null && moveAction.action != null)
            moveAction.action.Disable();

        if (resetAction != null && resetAction.action != null)
            resetAction.action.Disable();

        StopMouseDrag();
    }

    // Compatibilité avec les appels déjà présents dans le projet.
    public void _eneable()
    {
        can_move = false;
    }

    public void disable()
    {
        can_move = true;
    }

    private void Update()
    {
        HandleMouseInput();
        HandleControllerInput();
    }

    // --------------------------------------------------
    // SOURIS : même principe que SC_Button
    // --------------------------------------------------

    private bool TryGetCursorPosition(out Vector2 screenPosition, out Vector3 worldPosition)
    {
        screenPosition = Vector2.zero;
        worldPosition = Vector3.zero;

        if (SC_scursorManager.instance == null ||
            sliderCollider == null ||
            cam == null)
            return false;

        Vector3 cursorPosition = SC_scursorManager.instance.transform.position;
        screenPosition = new Vector2(cursorPosition.x, cursorPosition.y);

        if (!cam.pixelRect.Contains(screenPosition))
            return false;

        float depth = cam.WorldToScreenPoint(sliderCollider.bounds.center).z;

        worldPosition = cam.ScreenToWorldPoint(
            new Vector3(screenPosition.x, screenPosition.y, depth)
        );

        return true;
    }

    private void HandleMouseInput()
    {
        Mouse mouse = Mouse.current;

        if (mouse == null)
        {
            StopMouseDrag();
            return;
        }

        bool usingController =
            SC_controller_manager.instance != null &&
            SC_controller_manager.instance.using_controller;

        if (usingController)
        {
            StopMouseDrag();
            return;
        }

        if (cam == null)
            cam = Camera.main;

        if (cam == null || sliderCollider == null ||
            !sliderCollider.enabled || !isActiveAndEnabled)
        {
            StopMouseDrag();
            return;
        }

        bool validPosition = TryGetCursorPosition(
            out Vector2 screenPosition,
            out Vector3 worldPosition
        );

        bool pointerInside =
            validPosition && sliderCollider.OverlapPoint(worldPosition);

        if (mouse.leftButton.wasPressedThisFrame && pointerInside)
        {
            OnMousePress();
            SetValueFromMouse(screenPosition, cam);
        }

        if (mouseIsDragging && mouse.leftButton.isPressed && validPosition)
        {
            SetValueFromMouse(screenPosition, cam);
        }

        if (mouse.leftButton.wasReleasedThisFrame)
        {
            OnMouseRelease();
        }
    }

    public void OnMousePress()
    {
        if (mouseIsDragging)
            return;

        mouseIsDragging = true;
        mouseStartStep = GetCurrentStep(value);
    }

    public void SetValueFromMouse(Vector3 mousePosition, Camera cameraToUse)
    {
        Camera currentCam = cameraToUse != null ? cameraToUse : cam;

        if (cursor == null ||
            pointMin == null ||
            pointMax == null ||
            currentCam == null)
            return;

        Vector3 minScreen = currentCam.WorldToScreenPoint(pointMin.position);
        Vector3 maxScreen = currentCam.WorldToScreenPoint(pointMax.position);

        Vector2 axis = (Vector2)maxScreen - (Vector2)minScreen;
        float lengthSquared = axis.sqrMagnitude;

        if (lengthSquared < 0.0001f)
            return;

        float newValue = Vector2.Dot(
            (Vector2)mousePosition - (Vector2)minScreen,
            axis
        ) / lengthSquared;

        value = Mathf.Clamp01(newValue);
        UpdateCursor();
    }

    public void OnMouseRelease()
    {
        if (!mouseIsDragging)
            return;

        int finalStep = GetCurrentStep(value);

        if (finalStep != mouseStartStep)
            lastPlayedStep = finalStep;

        mouseIsDragging = false;
        mouseStartStep = -1;
    }

    private void StopMouseDrag()
    {
        if (mouseIsDragging)
            OnMouseRelease();
    }

    // --------------------------------------------------
    // MANETTE / CLAVIER
    // --------------------------------------------------

    private void HandleControllerInput()
    {
        if (!can_move)
            return;

        if (resetAction != null &&
            resetAction.action != null &&
            resetAction.action.WasPressedThisFrame())
        {
            value = 1f;
            UpdateCursor();
            PlayRandom();
        }

        if (moveAction == null || moveAction.action == null)
            return;

        if (button == null || (!button.isSelected && !button.isHovered))
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

        if (newStep != oldStep && newStep != lastPlayedStep)
        {
            lastPlayedStep = newStep;
            PlayRandom();
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

            List<AudioClip> validAudios = audios.FindAll(clip => clip != null);

            if (validAudios.Count == 0)
                return;

            int randomIndex = Random.Range(0, validAudios.Count);

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
    // SEGMENTATION
    // --------------------------------------------------

    private int GetCurrentStep(float val)
    {
        int steps = Mathf.Max(2, numberOfSteps);
        return Mathf.RoundToInt(Mathf.Clamp01(val) * (steps - 1));
    }

    // --------------------------------------------------
    // CURSEUR VISUEL
    // --------------------------------------------------

    private void UpdateCursor()
    {
        if (cursor == null || pointMin == null || pointMax == null)
            return;

        float displayValue = value;

        if (segmented)
        {
            int steps = Mathf.Max(2, numberOfSteps);
            displayValue = Mathf.Round(value * (steps - 1)) / (steps - 1);
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

            if (oldStep != newStep && newStep != lastPlayedStep)
            {
                lastPlayedStep = newStep;
                PlayRandom();
            }
        }

        UpdateCursor();
    }
}
