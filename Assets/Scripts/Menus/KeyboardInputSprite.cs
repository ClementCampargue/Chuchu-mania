
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class KeyboardInputSprite : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference inputAction;

    [Header("Fallback")]
    [SerializeField] private Sprite defaultSprite;
    [SerializeField] private string defaultText = "?";

    [Header("Text")]
    [SerializeField] private TextMeshPro textMesh;

    private SpriteRenderer spriteRenderer;
    private SC_input_display_master master;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        master = SC_input_display_master.instance;
        UpdateDisplay();
    }

    private void OnEnable()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (master == null)
            master = SC_input_display_master.instance;

        if (inputAction == null || inputAction.action == null)
        {
            SetFallback();
            return;
        }

        string path = GetKeyboardBindingPath();

        if (string.IsNullOrEmpty(path))
        {
            SetFallback();
            return;
        }

        UpdateFromPath(path);
    }

    private string GetKeyboardBindingPath()
    {
        var action = inputAction.action;

        foreach (var binding in action.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            string path = binding.effectivePath;

            if (string.IsNullOrEmpty(path))
                continue;

            if (path.ToLowerInvariant().StartsWith("<keyboard>/"))
                return path;
        }

        return null;
    }

    private void UpdateFromPath(string path)
    {
        string keyName = path.Substring(path.LastIndexOf('/') + 1);
        keyName = keyName.ToLowerInvariant();

        Sprite specialSprite = GetSpecialKeySprite(keyName);

        if (specialSprite != null)
        {
            // Touche spéciale : sprite visible, texte masqué.
            spriteRenderer.sprite = specialSprite;

            if (textMesh != null)
                textMesh.text = "";

            return;
        }

        // Touche classique : texte visible, sprite masqué.
        spriteRenderer.sprite = master.default_sprite;

        if (textMesh != null)
            textMesh.text = GetDisplayName(keyName);
    }

    private Sprite GetSpecialKeySprite(string keyName)
    {
        if (master == null)
            return null;

        switch (keyName)
        {
            case "space":
                return master.space_bar;

            case "leftctrl":
            case "rightctrl":
            case "ctrl":
                return master.ctrl;

            case "leftshift":
            case "rightshift":
            case "shift":
                return master.maj;

            case "tab":
                return master.tab;

            case "escape":
                return master.escape;

            case "up":
            case "uparrow":
                return master.arrow_up;

            case "down":
            case "downarrow":
                return master.arrow_down;

            case "left":
            case "leftarrow":
                return master.arrow_left;

            case "right":
            case "rightarrow":
                return master.arrow_right;

            default:
                return null;
        }
    }

    private string GetDisplayName(string keyName)
    {
        switch (keyName)
        {
            case "enter":
                return "ENTER";

            case "escape":
                return "ESC";

            case "backspace":
                return "BACKSPACE";

            case "delete":
                return "DEL";

            default:
                return keyName.ToUpperInvariant();
        }
    }

    private void SetFallback()
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = defaultSprite;

        if (textMesh != null)
            textMesh.text = defaultSprite == null ? defaultText : "";
    }
}
