
using System;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

[RequireComponent(typeof(SpriteRenderer))]
public class KeyboardInputSprite : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference inputAction;

    [Header("Composite 2D Vector")]
    [SerializeField] private bool useCompositePart;
    [SerializeField] private string compositePartName = "up";

    [Tooltip("-1 recherche automatiquement le composite.")]
    [SerializeField] private int compositeParentBindingIndex = -1;

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

        if (useCompositePart)
        {
            for (int parent = 0; parent < action.bindings.Count; parent++)
            {
                var parentBinding = action.bindings[parent];

                if (!parentBinding.isComposite)
                    continue;

                if (compositeParentBindingIndex >= 0 &&
                    parent != compositeParentBindingIndex)
                {
                    continue;
                }

                for (int i = parent + 1; i < action.bindings.Count; i++)
                {
                    var binding = action.bindings[i];

                    if (binding.isComposite)
                        break;

                    if (!binding.isPartOfComposite)
                        continue;

                    if (!string.Equals(
                        binding.name,
                        compositePartName,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }

                    string path = binding.effectivePath;

                    if (IsKeyboardPath(path))
                        return path;
                }
            }

            return null;
        }

        foreach (var binding in action.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            string path = binding.effectivePath;

            if (IsKeyboardPath(path))
                return path;
        }

        return null;
    }

    private bool IsKeyboardPath(string path)
    {
        return !string.IsNullOrEmpty(path) &&
               path.StartsWith(
                   "<Keyboard>/",
                   StringComparison.OrdinalIgnoreCase
               );
    }

    private void UpdateFromPath(string path)
    {
        string keyName = path.Substring(path.LastIndexOf('/') + 1);
        keyName = keyName.ToLowerInvariant();

        Sprite specialSprite = GetSpecialKeySprite(keyName);

        if (specialSprite != null)
        {
            spriteRenderer.sprite = specialSprite;

            if (textMesh != null)
                textMesh.text = "";

            return;
        }

        Sprite regularSprite = master != null
            ? master.default_sprite
            : defaultSprite;

        spriteRenderer.sprite = regularSprite;

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

            case "uparrow":
                return "UP";

            case "downarrow":
                return "DOWN";

            case "leftarrow":
                return "LEFT";

            case "rightarrow":
                return "RIGHT";

            default:
                return keyName.ToUpperInvariant();
        }
    }

    private void SetFallback()
    {
        if (spriteRenderer != null)
        {
            spriteRenderer.sprite = master != null
                ? master.default_sprite
                : defaultSprite;
        }

        if (textMesh != null)
            textMesh.text = defaultText;
    }
}
