
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;
using UnityEngine.Timeline;

[RequireComponent(typeof(SpriteRenderer))]
public class GamepadInputSprite : MonoBehaviour
{
    [Header("Input")]
    [SerializeField] private InputActionReference inputAction;

    private SC_input_display_master master;



    [Header("Fallback")]
    [SerializeField] private Sprite defaultSprite;

    private SpriteRenderer spriteRenderer;

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
        if (inputAction == null || inputAction.action == null)
            return;

        string path = GetGamepadBindingPath();

        if (string.IsNullOrEmpty(path))
        {
            spriteRenderer.sprite = defaultSprite;
            return;
        }

        spriteRenderer.sprite = GetSpriteFromPath(path);
    }

    private string GetGamepadBindingPath()
    {
        var action = inputAction.action;

        // Récupère le 2e binding (index 1)
        if (action.bindings.Count > 1)
        {
            var binding = action.bindings[1];

            if (!binding.isComposite && !binding.isPartOfComposite)
                return binding.effectivePath;
        }

        return null;
    }

    private Sprite GetSpriteFromPath(string path)
    {
        if (string.IsNullOrEmpty(path))
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

        return defaultSprite;
    }
}