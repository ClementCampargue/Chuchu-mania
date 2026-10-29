using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Localization;

public class SC_first_launch : MonoBehaviour
{

    public bool reset_controls;
    public InputActionReference actionReference;

    private InputActionRebindingExtensions.RebindingOperation rebindOperation;
    private InputActionAsset inputAsset;
    private const string SaveKey = "rebinds";
    void Awake()
    {
        if (actionReference != null && actionReference.action != null)
        {
            inputAsset = actionReference.action.actionMap.asset;
        }
    }
    public void ResetAllBindings()
    {
        if (inputAsset == null)
        {
            Debug.LogError("InputActionAsset introuvable.");
            return;
        }

        // Supprime tous les overrides de l'asset en mémoire.
        inputAsset.RemoveAllBindingOverrides();

        // Supprime les anciennes sauvegardes de rebinding.
        PlayerPrefs.DeleteKey(SaveKey);
        PlayerPrefs.Save();

        // Affiche les bindings réellement restaurés.
        foreach (var map in inputAsset.actionMaps)
        {
            foreach (var action in map.actions)
            {
                for (int i = 0; i < action.bindings.Count; i++)
                {
                    Debug.Log(
                        $"{action.actionMap.name}/{action.name} " +
                        $"[{i}] = {action.bindings[i].effectivePath}"
                    );
                }
            }
        }
    }


    void OnEnable()
    {
        if (reset_controls)
        {
            ResetAllBindings();
        }
        if (PlayerPrefs.GetFloat("firstlaunchever") == 0)
        {




            // AUDIO

            PlayerPrefs.SetFloat("Master", 1);
            PlayerPrefs.SetFloat("Music", 1);
            PlayerPrefs.SetFloat("Sound", 1);


            // GAME

            PlayerPrefs.SetFloat("Langue", 0);


            PlayerPrefs.SetFloat("Tremblement", 0);
            PlayerPrefs.SetFloat("Vibration", 0);
            PlayerPrefs.SetFloat("Cheats", 1);


            // DISPLAY

            PlayerPrefs.SetFloat("Resolution", 3);
            PlayerPrefs.SetFloat("Ecran", 1);
            PlayerPrefs.SetFloat("V sync", 0);
            PlayerPrefs.SetFloat("FPS", 2);
            PlayerPrefs.SetFloat("Overlay", 0);

            // CONTROLS



        PlayerPrefs.SetFloat("firstlaunchever", 1);

            PlayerPrefs.Save();
        }
    }


    private void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {

    }
}
