
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

public class SC_list_text : MonoBehaviour
{
    public UnityEvent event_;

    public enum TextMode
    {
        String,
        Localized
    }

    [Header("Input")]
    [SerializeField] private InputActionReference moveAction;

    [Header("Texte")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private TextMode textMode = TextMode.Localized;

    [Header("Liste de textes")]
    [SerializeField] private string[] strings;
    [SerializeField] private LocalizedString[] localizedStrings;

    [Header("Index")]
    [Min(0)]
    public int index = 0;

    [Header("Vitesse")]
    [Min(0.01f)]
    [SerializeField] private float speed = 5f;

    [Min(0f)]
    [SerializeField] private float initialDelay = 0.3f;

    [Header("État")]
    public bool can_move;
    public SC_Button button;

    public SC_juiciness juice;
    private float inputCooldown;
    private bool wasMoving;
    private bool actionEnabledByThisComponent;

    private int Count
    {
        get
        {
            if (textMode == TextMode.String)
                return strings != null ? strings.Length : 0;

            return localizedStrings != null
                ? localizedStrings.Length
                : 0;
        }
    }

    private void OnEnable()
    {
        if (moveAction != null && moveAction.action != null)
        {
            if (!moveAction.action.enabled)
            {
                moveAction.action.Enable();
                actionEnabledByThisComponent = true;
            }
        }

        ResetInput();
        UpdateText();
    }

    private void OnDisable()
    {
        ResetInput();

        if (actionEnabledByThisComponent &&
            moveAction != null &&
            moveAction.action != null)
        {
            moveAction.action.Disable();
            actionEnabledByThisComponent = false;
        }
    }

    // Désactive le déplacement
    public void _eneable()
    {
        can_move = false;
        ResetInput();
    }

    // Autorise le déplacement
    public void disable()
    {
        can_move = true;
        ResetInput();
    }

    private void Update()
    {
        if (!CanReadInput())
        {
            ResetInput();
            return;
        }

        float input = moveAction.action.ReadValue<Vector2>().x;

        // Aucun mouvement : on attend une nouvelle pression.
        if (Mathf.Abs(input) <= 0.5f)
        {
            ResetInput();
            return;
        }

        int direction = input > 0f ? 1 : -1;

        // Premier changement immédiat.
        if (!wasMoving)
        {
            wasMoving = true;

            ChangeIndex(direction);

            inputCooldown = initialDelay;
            return;
        }

        // Répétition tant que l'input est maintenu.
        inputCooldown -= Time.unscaledDeltaTime;

        if (inputCooldown <= 0f)
        {
            ChangeIndex(direction);

            inputCooldown = 1f / Mathf.Max(speed, 0.01f);
        }
    }

    private bool CanReadInput()
    {
        return moveAction != null &&
               moveAction.action != null &&
               can_move &&
               button != null &&
               (button.isSelected || button.isHovered);
    }

    private void ChangeIndex(int direction)
    {
        int count = Count;

        if (count <= 0)
            return;
        juice.PlayJuice();

        // Sécurise l'index avant de le modifier.
        index = ((index % count) + count) % count;

        // Boucle correctement dans les deux sens.
        index = (index + direction + count) % count;

        UpdateText();

        // L'événement reçoit le nouvel index.
        event_?.Invoke();
    }

    private void ResetInput()
    {
        inputCooldown = 0f;
        wasMoving = false;
    }

    private void UpdateText()
    {
        int count = Count;

        if (text == null || count <= 0)
            return;

        // Empêche tout index hors limites.
        index = ((index % count) + count) % count;

        if (textMode == TextMode.String)
        {
            text.text = strings[index];
        }
        else
        {
            text.text = localizedStrings[index].GetLocalizedString();
        }
    }

    public void SetIndex(int newIndex)
    {
        int count = Count;

        if (count <= 0)
            return;

        index = ((newIndex % count) + count) % count;
        UpdateText();
    }

    public void SetTextMode(TextMode mode)
    {
        textMode = mode;
        index = 0;

        ResetInput();
        UpdateText();
    }
}
