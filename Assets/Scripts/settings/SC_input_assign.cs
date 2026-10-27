
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_input_assign : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference actionReference;

    private int bindingIndex;

    [Tooltip("Activé = manette, désactivé = clavier et souris")]
    private bool gamepadBinding;

    [Header("UI")]
    public GameObject listeningOverlay;
    public List<SC_Button> buttons;

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private InputActionAsset inputAsset;

    private const string SaveKey = "rebinds";

    private bool actionWasEnabled;
    private bool rebindInProgress;

    void Awake()
    {
        if (actionReference != null && actionReference.action != null)
        {
            inputAsset = actionReference.action.actionMap.asset;
        }
    }

    void Start()
    {
        LoadBindings();
    }

    public void StartRebind()
    {
        if (rebindInProgress)
            return;

        if (actionReference == null || actionReference.action == null)
        {
            Debug.LogError("SC_input_assign : Action Reference manquant.");
            return;
        }

        if (SC_controller_manager.instance == null)
        {
            Debug.LogError("SC_input_assign : Controller Manager manquant.");
            return;
        }

        var action = actionReference.action;

        // Sélection du périphérique.
        gamepadBinding = SC_controller_manager.instance.using_controller;
        bindingIndex = gamepadBinding ? 1 : 0;

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            Debug.LogError("SC_input_assign : Binding Index invalide.");
            return;
        }

        var binding = action.bindings[bindingIndex];

        if (binding.isComposite || binding.isPartOfComposite)
        {
            Debug.LogWarning(
                "Sélectionne un binding concret à reconfigurer."
            );
            return;
        }

        // Vérifie le type du binding existant.
        string path = binding.effectivePath;

        if (!string.IsNullOrEmpty(path))
        {
            bool isGamepadPath =
                path.Contains("<Gamepad>") ||
                path.Contains("<XInputController>");

            bool isKeyboardPath = path.Contains("<Keyboard>");

            if ((gamepadBinding && isKeyboardPath) ||
                (!gamepadBinding && isGamepadPath))
            {
                Debug.LogWarning(
                    "Le binding sélectionné ne correspond pas au périphérique choisi."
                );
                return;
            }
        }

        // Désactive les boutons de l'interface pendant l'écoute.
        if (buttons != null)
        {
            foreach (SC_Button but in buttons)
            {
                if (but != null)
                    but.enabled = false;
            }
        }

        if (listeningOverlay != null)
            listeningOverlay.SetActive(true);

        actionWasEnabled = action.enabled;
        action.Disable();

        rebindInProgress = true;

        var rebind = action.PerformInteractiveRebinding(bindingIndex);

        if (gamepadBinding)
        {
            // Mode manette : uniquement les contrôles de manette.
            rebind.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else
        {
            // Mode clavier et souris :
            // autorise le clavier et les contrôles de souris.
            rebind.WithControlsHavingToMatchPath("<Keyboard>");
            rebind.WithControlsHavingToMatchPath("<Mouse>");
        }

        rebindOperation = rebind
            // Échap annule le rebinding.
            .WithCancelingThrough("<Keyboard>/escape")

            // Ignore le déplacement de la souris.
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")

            // Ignore également le déplacement générique du pointeur.
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")

            // Attend brièvement avant de valider le contrôle détecté.
            .OnMatchWaitForAnother(0.1f)

            .OnComplete(operation =>
            {
                ResolveConflictsSmart(operation);
                FinishRebind(true);
            })

            .OnCancel(operation =>
            {
                FinishRebind(false);
            })

            .Start();
    }

    void ResolveConflictsSmart(
        InputActionRebindingExtensions.RebindingOperation operation)
    {
        if (inputAsset == null || operation.selectedControl == null)
            return;

        string newPath = operation.selectedControl.path;

        bool isGamepad = operation.selectedControl.device is Gamepad;
        bool isMouse = operation.selectedControl.device is Mouse;
        bool isKeyboard = operation.selectedControl.device is Keyboard;

        foreach (var map in inputAsset.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    // Ignore le binding actuellement reconfiguré.
                    if (action == actionReference.action &&
                        i == bindingIndex)
                    {
                        continue;
                    }

                    var binding = action.bindings[i];

                    // Ne modifie jamais les bindings composites parents.
                    if (binding.isComposite)
                        continue;

                    string otherPath = binding.effectivePath;

                    if (string.IsNullOrEmpty(otherPath))
                        continue;

                    bool otherIsGamepad =
                        otherPath.Contains("<Gamepad>") ||
                        otherPath.Contains("<XInputController>");

                    bool otherIsKeyboard =
                        otherPath.Contains("<Keyboard>");

                    bool otherIsMouse =
                        otherPath.Contains("<Mouse>");

                    // Résout les conflits uniquement pour le même périphérique.
                    if (isGamepad && !otherIsGamepad)
                        continue;

                    if (isKeyboard && !otherIsKeyboard)
                        continue;

                    if (isMouse && !otherIsMouse)
                        continue;

                    if (otherPath == newPath)
                    {
                        action.RemoveBindingOverride(i);
                    }
                }
            }
        }
    }

    void FinishRebind(bool completed)
    {
        if (rebindOperation != null)
        {
            rebindOperation.Dispose();
            rebindOperation = null;
        }

        rebindInProgress = false;

        // Restaure l'état initial de l'action.
        if (actionReference != null &&
            actionReference.action != null &&
            actionWasEnabled)
        {
            actionReference.action.Enable();
        }

        // Réactive les boutons de l'interface.
        if (buttons != null)
        {
            foreach (SC_Button but in buttons)
            {
                if (but != null)
                    but.enabled = true;
            }
        }

        if (listeningOverlay != null)
            listeningOverlay.SetActive(false);

        if (completed)
        {
            SaveBindings();
            UpdateBindingDisplay();
        }
    }

    public void SaveBindings()
    {
        if (inputAsset == null)
            return;

        PlayerPrefs.SetString(
            SaveKey,
            inputAsset.SaveBindingOverridesAsJson()
        );

        PlayerPrefs.Save();
    }

    public void LoadBindings()
    {
        if (inputAsset == null)
            return;

        string json = PlayerPrefs.GetString(SaveKey, "");

        if (!string.IsNullOrEmpty(json))
        {
            inputAsset.LoadBindingOverridesFromJson(json);
        }
    }

    public void ResetToDefault()
    {
        if (actionReference == null || actionReference.action == null)
            return;

        var action = actionReference.action;

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
            return;

        action.RemoveBindingOverride(bindingIndex);

        SaveBindings();
        UpdateBindingDisplay();
    }

    // À connecter au bouton Reset All du menu des paramètres.
    public void ResetAllBindings()
    {
        if (inputAsset == null)
            return;

        inputAsset.RemoveAllBindingOverrides();

        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        UpdateBindingDisplay();
    }

    public void UpdateBindingDisplay()
    {
        // Exemple avec TextMeshPro :
        //
        // TMP_Text label = GetComponentInChildren<TMP_Text>();
        //
        // if (label != null &&
        //     actionReference != null &&
        //     actionReference.action != null)
        // {
        //     label.text = actionReference.action
        //         .GetBindingDisplayString(bindingIndex);
        // }
    }

    void OnDisable()
    {
        if (rebindOperation != null)
        {
            rebindOperation.Cancel();
            rebindOperation.Dispose();
            rebindOperation = null;
        }

        rebindInProgress = false;

        if (listeningOverlay != null)
            listeningOverlay.SetActive(false);

        if (buttons != null)
        {
            foreach (SC_Button but in buttons)
            {
                if (but != null)
                    but.enabled = true;
            }
        }

        if (actionReference != null &&
            actionReference.action != null &&
            actionWasEnabled)
        {
            actionReference.action.Enable();
        }
    }
}
