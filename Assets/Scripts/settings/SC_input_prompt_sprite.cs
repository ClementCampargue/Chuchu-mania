
using System;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(SpriteRenderer))]
public class GamepadInputSprite : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference inputAction;

    [Header("Composite 2D Vector")]
    [SerializeField] private bool useCompositePart;
    [SerializeField] private string compositePartName = "up";

    [Tooltip("-1 = recherche automatique du composite parent.")]
    [SerializeField] private int compositeParentBindingIndex = -1;

    [Header("Fallback")]
    [SerializeField] private Sprite defaultSprite;

    private SpriteRenderer spriteRenderer;
    private SC_input_display_master master;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    private void Start()
    {
        master = SC_input_display_master.instance;
        UpdateSprite();
    }

    private void OnEnable()
    {
        UpdateSprite();
    }

    public void UpdateSprite()
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

        string path = GetGamepadBindingPath();

        if (string.IsNullOrEmpty(path))
        {
            SetFallback();
            return;
        }

        Sprite result = GetSpriteFromPath(path);

        spriteRenderer.sprite = result != null
            ? result
            : defaultSprite;
    }

    // --------------------------------------------------
    // RECHERCHE DU BINDING
    // --------------------------------------------------

    private string GetGamepadBindingPath()
    {
        var action = inputAction.action;

        // Cas d'une direction de composite : up/down/left/right.
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

                    if (IsGamepadPath(path))
                        return path;
                }
            }

            return null;
        }

        // Cas d'une action classique : recherche un binding
        // manette concret, sans dépendre d'un index fixe.
        foreach (var binding in action.bindings)
        {
            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            string path = binding.effectivePath;

            if (IsGamepadPath(path))
                return path;
        }

        return null;
    }

    private bool IsGamepadPath(string path)
    {
        return !string.IsNullOrEmpty(path) &&
               path.IndexOf(
                   "<Gamepad>",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }

    // --------------------------------------------------
    // ASSOCIATION CHEMIN / SPRITE
    // --------------------------------------------------

    private Sprite GetSpriteFromPath(string path)
    {
        if (string.IsNullOrEmpty(path) || master == null)
            return defaultSprite;

        path = path.ToLowerInvariant();

        // Épaules
        if (path.Contains("leftshoulder"))
            return master.bump_left;

        if (path.Contains("rightshoulder"))
            return master.bump_right;

        // Gâchettes
        if (path.Contains("lefttrigger"))
            return master.trigger_left;

        if (path.Contains("righttrigger"))
            return master.trigger_right;

        // Boutons centraux
        if (path.Contains("start") || path.Contains("menu"))
            return master.start;

        if (path.Contains("select") ||
            path.Contains("view") ||
            path.Contains("back"))
            return master.select;

        // Boutons ABXY
        if (path.Contains("buttonsouth"))
            return master.A;

        if (path.Contains("buttoneast"))
            return master.B;

        if (path.Contains("buttonwest"))
            return master.X;

        if (path.Contains("buttonnorth"))
            return master.Y;

        // D-pad
        if (path.Contains("dpad/up"))
            return master.pad_up;

        if (path.Contains("dpad/down"))
            return master.pad_down;

        if (path.Contains("dpad/left"))
            return master.pad_left;

        if (path.Contains("dpad/right"))
            return master.pad_right;

        // Stick gauche : affichage des directions
        if (path.Contains("leftstick/up"))
            return master.pad_up;

        if (path.Contains("leftstick/down"))
            return master.pad_down;

        if (path.Contains("leftstick/left"))
            return master.pad_left;

        if (path.Contains("leftstick/right"))
            return master.pad_right;

        return defaultSprite;
    }

    private void SetFallback()
    {
        if (spriteRenderer != null)
            spriteRenderer.sprite = defaultSprite;
    }
}
