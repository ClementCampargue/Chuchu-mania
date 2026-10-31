using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;
using TMPro;

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

    [Header("Display")]
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Image uiImage;
    [SerializeField] private TextMeshPro textMesh;
    [SerializeField] private TextMeshProUGUI uiText;

    private SC_input_display_master master;

    private void Awake()
    {
        // Récupération automatique des composants.
        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();

        if (uiImage == null)
            uiImage = GetComponent<Image>();

        if (textMesh == null)
            textMesh = GetComponentInChildren<TextMeshPro>();

        if (uiText == null)
            uiText = GetComponentInChildren<TextMeshProUGUI>();
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
                    continue;

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
                        continue;

                    string path = binding.effectivePath;

                    if (IsSupportedPath(path))
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

            if (IsSupportedPath(path))
                return path;
        }

        return null;
    }

    private bool IsSupportedPath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        return path.StartsWith("<Keyboard>/", StringComparison.OrdinalIgnoreCase)
            || path.StartsWith("<Mouse>/", StringComparison.OrdinalIgnoreCase);
    }

    private void UpdateFromPath(string path)
    {
        string keyName = path.Substring(path.LastIndexOf('/') + 1);
        keyName = keyName.ToLowerInvariant();

        Sprite specialSprite = GetSpecialKeySprite(keyName);

        if (specialSprite != null)
        {
            SetSprite(specialSprite);
            SetText("");
            return;
        }

        Sprite regularSprite = master != null
            ? master.default_sprite
            : defaultSprite;

        SetSprite(regularSprite);
        SetText(GetDisplayName(keyName));
    }

    private void SetSprite(Sprite sprite)
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = sprite;

        if (uiImage != null)
            uiImage.sprite = sprite;
    }

    private void SetText(string value)
    {
        if (textMesh != null)
            textMesh.text = value;

        if (uiText != null)
            uiText.text = value;
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

            case "leftbutton":
            case "mouse 0":
                return master.mouse_left;

            case "rightbutton":
            case "mouse 1":
                return master.mouse_right;

            case "middlebutton":
            case "mouse 2":
                return master.mouse_middle;

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
        Sprite fallback = master != null
            ? master.default_sprite
            : defaultSprite;

        SetSprite(fallback);
        SetText(defaultText);
    }
}