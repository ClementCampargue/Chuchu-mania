
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class SC_input_assign : MonoBehaviour
{
    [Header("Input")]
    public InputActionReference reset_to_default_action;
    public bool canreset;
    public InputActionReference actionReference;

    [Header("Binding cible")]
    [Tooltip("Active pour reconfigurer une direction d'un composite 2D Vector.")]
    public bool useCompositePart;

    [Tooltip("Nom de la direction : up, down, left ou right.")]
    public string compositePartName;

    [Tooltip("Index du binding parent 2D Vector. -1 = recherche automatique.")]
    public int compositeParentBindingIndex = -1;

    private int bindingIndex = -1;
    private bool gamepadBinding;

    [Header("UI")]
    public GameObject listeningOverlay;
    public List<SC_Button> buttons;

    [Header("Sprites")]
    public KeyboardInputSprite prompt_;
    public GamepadInputSprite prompt;

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private InputActionAsset inputAsset;

    private const string SaveKey = "rebinds";

    private bool actionWasEnabled;
    private bool rebindInProgress;
    private bool waitingForOpeningInputRelease;
    private string pendingPartName;

    private void Awake()
    {
        if (actionReference != null &&
            actionReference.action != null &&
            actionReference.action.actionMap != null)
        {
            inputAsset = actionReference.action.actionMap.asset;
        }

        LoadBindings();
    }

    private void Update()
    {
        if (reset_to_default_action != null &&
            reset_to_default_action.action != null &&
            reset_to_default_action.action.WasPressedThisFrame() &&
            canreset)
        {
            ResetToDefault();

            if (prompt != null)
                prompt.UpdateSprite();

            if (prompt_ != null)
                prompt_.UpdateDisplay();
        }
    }

    public void canreset_()
    {
        canreset = true;
    }

    public void cantreset_()
    {
        canreset = false;
    }

    // --------------------------------------------------
    // DÉMARRAGE DU REBINDING
    // --------------------------------------------------

    public void StartRebind()
    {
        pendingPartName = null;
        BeginWaitingForRelease();
    }

    public void StartRebindUp()
    {
        StartRebindDirection("up");
    }

    public void StartRebindDown()
    {
        StartRebindDirection("down");
    }

    public void StartRebindLeft()
    {
        StartRebindDirection("left");
    }

    public void StartRebindRight()
    {
        StartRebindDirection("right");
    }

    public void StartRebindDirection(string partName)
    {
        if (!useCompositePart)
        {
            Debug.LogWarning(
                "Active useCompositePart pour reconfigurer une direction."
            );
            return;
        }

        pendingPartName = partName;
        BeginWaitingForRelease();
    }

    private void BeginWaitingForRelease()
    {
        if (rebindInProgress || waitingForOpeningInputRelease)
            return;

        waitingForOpeningInputRelease = true;
        StartCoroutine(WaitForOpeningInputRelease());
    }

    private IEnumerator WaitForOpeningInputRelease()
    {
        while (AnyButtonIsPressed())
            yield return null;

        yield return null;

        waitingForOpeningInputRelease = false;
        StartRebindInternal();
    }

    private bool AnyButtonIsPressed()
    {
        foreach (var device in InputSystem.devices)
        {
            foreach (var control in device.allControls)
            {
                if (control is ButtonControl button && button.isPressed)
                    return true;
            }
        }

        return false;
    }

    // --------------------------------------------------
    // SÉLECTION DU BINDING
    // --------------------------------------------------

    private bool IsSelectedDevicePath(string path)
    {
        if (string.IsNullOrEmpty(path))
            return false;

        if (gamepadBinding)
        {
            return path.IndexOf(
                "<Gamepad>",
                StringComparison.OrdinalIgnoreCase
            ) >= 0;
        }

        return path.IndexOf(
                   "<Keyboard>",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0
               ||
               path.IndexOf(
                   "<Mouse>",
                   StringComparison.OrdinalIgnoreCase
               ) >= 0;
    }

    private int FindCompositePartIndex(
        InputAction action,
        string partName)
    {
        if (action == null || string.IsNullOrEmpty(partName))
            return -1;

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
                var child = action.bindings[i];

                if (child.isComposite)
                    break;

                if (!child.isPartOfComposite)
                    continue;

                if (!string.Equals(
                    child.name,
                    partName,
                    StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                if (!IsSelectedDevicePath(child.effectivePath))
                    continue;

                return i;
            }
        }

        return -1;
    }

    private int FindRegularBindingIndex(InputAction action)
    {
        for (int i = 0; i < action.bindings.Count; i++)
        {
            var binding = action.bindings[i];

            if (binding.isComposite || binding.isPartOfComposite)
                continue;

            if (IsSelectedDevicePath(binding.effectivePath))
                return i;
        }

        return -1;
    }

    private int GetTargetBindingIndex()
    {
        if (actionReference == null || actionReference.action == null)
            return -1;

        var action = actionReference.action;

        if (useCompositePart)
        {
            return FindCompositePartIndex(action, compositePartName);
        }

        return FindRegularBindingIndex(action);
    }

    // --------------------------------------------------
    // REBINDING
    // --------------------------------------------------

    private void StartRebindInternal()
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

        gamepadBinding = SC_controller_manager.instance.using_controller;

        bindingIndex = GetTargetBindingIndex();

        if (bindingIndex < 0 || bindingIndex >= action.bindings.Count)
        {
            Debug.LogWarning(
                "Binding introuvable pour " +
                (useCompositePart ? pendingPartName : action.name) +
                ". Vérifie les bindings et le périphérique sélectionné."
            );
            return;
        }

        var binding = action.bindings[bindingIndex];

        if (binding.isComposite)
        {
            Debug.LogWarning(
                "Impossible de reconfigurer directement le parent composite."
            );
            return;
        }

        if (useCompositePart && !binding.isPartOfComposite)
        {
            Debug.LogWarning("Le binding sélectionné n'est pas une direction.");
            return;
        }

        SetUIListening(true);

        actionWasEnabled = action.enabled;
        action.Disable();

        rebindInProgress = true;

        var rebind = action.PerformInteractiveRebinding(bindingIndex);

        if (gamepadBinding)
        {
            rebind.WithControlsHavingToMatchPath("<Gamepad>");
        }
        else
        {
            rebind.WithControlsHavingToMatchPath("<Keyboard>");
            rebind.WithControlsHavingToMatchPath("<Mouse>");
        }

        rebindOperation = rebind
            .WithControlsExcluding("<Mouse>/position")
            .WithControlsExcluding("<Mouse>/delta")
            .WithControlsExcluding("<Pointer>/position")
            .WithControlsExcluding("<Pointer>/delta")
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

    // --------------------------------------------------
    // GESTION DES CONFLITS
    // --------------------------------------------------

    private void ResolveConflictsSmart(
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
                    if (action == actionReference.action &&
                        i == bindingIndex)
                    {
                        continue;
                    }

                    var otherBinding = action.bindings[i];

                    if (otherBinding.isComposite)
                        continue;

                    string otherPath = otherBinding.effectivePath;

                    if (string.IsNullOrEmpty(otherPath))
                        continue;

                    bool otherIsGamepad =
                        otherPath.IndexOf(
                            "<Gamepad>",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                    bool otherIsKeyboard =
                        otherPath.IndexOf(
                            "<Keyboard>",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                    bool otherIsMouse =
                        otherPath.IndexOf(
                            "<Mouse>",
                            StringComparison.OrdinalIgnoreCase
                        ) >= 0;

                    if (isGamepad && !otherIsGamepad)
                        continue;

                    if (isKeyboard && !otherIsKeyboard)
                        continue;

                    if (isMouse && !otherIsMouse)
                        continue;

                    if (string.Equals(
                        otherPath,
                        newPath,
                        StringComparison.OrdinalIgnoreCase))
                    {
                        // Libère le contrôle déjà utilisé.
                        action.ApplyBindingOverride(i, string.Empty);
                    }
                }
            }
        }
    }

    // --------------------------------------------------
    // FIN DU REBINDING
    // --------------------------------------------------

    private void FinishRebind(bool completed)
    {
        if (rebindOperation != null)
        {
            rebindOperation.Dispose();
            rebindOperation = null;
        }

        rebindInProgress = false;

        if (actionReference != null &&
            actionReference.action != null &&
            actionWasEnabled)
        {
            actionReference.action.Enable();
        }

        SetUIListening(false);

        if (completed)
        {
            SaveBindings();
            UpdateBindingDisplay();

            if (prompt != null)
                prompt.UpdateSprite();

            if (prompt_ != null)
                prompt_.UpdateDisplay();
        }
    }

    private void SetUIListening(bool listening)
    {
        if (buttons != null)
        {
            foreach (SC_Button button in buttons)
            {
                if (button != null)
                    button.enabled = !listening;
            }
        }

        if (listeningOverlay != null)
            listeningOverlay.SetActive(listening);
    }

    // --------------------------------------------------
    // SAUVEGARDE / CHARGEMENT
    // --------------------------------------------------

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

    // --------------------------------------------------
    // RESET
    // --------------------------------------------------

    public void ResetToDefault()
    {
        if (actionReference == null || actionReference.action == null)
            return;

        if (SC_controller_manager.instance != null)
        {
            gamepadBinding =
                SC_controller_manager.instance.using_controller;
        }

        int index = GetTargetBindingIndex();

        if (index < 0)
        {
            Debug.LogWarning("Binding à réinitialiser introuvable.");
            return;
        }

        bindingIndex = index;

        actionReference.action.RemoveBindingOverride(bindingIndex);

        SaveBindings();
        UpdateBindingDisplay();

        if (prompt != null)
            prompt.UpdateSprite();

        if (prompt_ != null)
            prompt_.UpdateDisplay();
    }

    public void ResetAllBindings()
    {
        if (inputAsset == null)
        {
            Debug.LogError("InputActionAsset introuvable.");
            return;
        }

        inputAsset.RemoveAllBindingOverrides();

        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        UpdateBindingDisplay();

        if (prompt != null)
            prompt.UpdateSprite();

        if (prompt_ != null)
            prompt_.UpdateDisplay();
    }

    public void UpdateBindingDisplay()
    {
        if (actionReference == null || actionReference.action == null)
            return;

        int index = GetTargetBindingIndex();

        if (index < 0)
            return;

        bindingIndex = index;

        // Branche ici ton composant de texte si nécessaire.
        // Les sprites sont rafraîchis dans FinishRebind et ResetToDefault.
    }

    private void OnDisable()
    {
        if (rebindOperation != null)
        {
            rebindOperation.Cancel();
            rebindOperation.Dispose();
            rebindOperation = null;
        }

        rebindInProgress = false;
        waitingForOpeningInputRelease = false;

        StopAllCoroutines();

        SetUIListening(false);

        if (actionReference != null &&
            actionReference.action != null &&
            actionWasEnabled)
        {
            actionReference.action.Enable();
        }
    }
}
