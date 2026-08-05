using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class SC_dialogue_system : MonoBehaviour
{
    [Header("Dialogue Data")]
    public List<SO_dialogue_line> dialogues;
    public bool cutscene;
    [Header("References")]
    public SC_typewriter typewriter;

    public TMP_Text characterName;
    public TMP_Text characterName2;
    public SpriteRenderer portrait;
     
    [Header("Input")]
    public InputActionReference nextDialogueInput;
    public Action onDialogueEnd;

    public Animator animator;

    private int currentIndex;
    private bool dialogueActive;

    public static SC_dialogue_system instance;
    public SC_cutscene cutscene_;

    public RectTransform text_position;
    public RectTransform dialogue_trs;
    public RectTransform monologue_trs;
    public GameObject choices;
    private void Awake()
    {
        // Evite les doublons et réinitialise correctement
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;

        // Reset des variables au démarrage
        currentIndex = 0;
        dialogueActive = false;
    }

    private void Start()
    {
        choices.SetActive(false);
        if (cutscene)
        {
            StartDialogue();
        }
    }


    private void OnEnable()
    {
        if (nextDialogueInput != null)
        {
            nextDialogueInput.action.Enable();
            nextDialogueInput.action.performed += NextInput;
        }
    }


    private void OnDisable()
    {
        if (nextDialogueInput != null)
        {
            nextDialogueInput.action.performed -= NextInput;
            nextDialogueInput.action.Disable();
        }
    }


    public void StartDialogue()
    {
        if (dialogues == null || dialogues.Count == 0)
        {
            Debug.LogWarning("Aucun dialogue assigné !");
            return;
        }
        choices.SetActive(false);
        characterName.gameObject.SetActive(true);

        currentIndex = 0;
        dialogueActive = true;

        if (animator != null)
        {
            animator.enabled = true;
            animator.Play("popup_dialogue_show");
        }

        ShowDialogue();
    }


    private void ShowDialogue()
    {
        if (currentIndex >= dialogues.Count)
            return;


        SO_dialogue_line line = dialogues[currentIndex];


        if (line.character.name == string.Empty)
        {
            choices.SetActive(true);

            text_position.position = monologue_trs.position;
            characterName.gameObject.SetActive(false);
        }
        else
        {
            choices.SetActive(false);
            text_position.position = dialogue_trs.position; 
            characterName.gameObject.SetActive(true);
        }
        if (characterName != null)
            characterName.text = line.character.name + " <sprite index=0>";


        if (characterName2 != null)
            characterName2.text = line.character.name;


        if (portrait != null)
            portrait.sprite = line.character.portrait;
           

        if (typewriter != null)
        {
            typewriter.typeSound = line.character.typesound;
            typewriter.TriggerText(line.text);
        }
    }


    private void NextInput(InputAction.CallbackContext ctx)
    {
        if (!dialogueActive)
            return;


        if (typewriter != null && typewriter.IsTyping())
        {
            typewriter.FinishText();
            return;
        }


        NextDialogue();
    }


    private void NextDialogue()
    {
        currentIndex++;


        if (currentIndex >= dialogues.Count)
        {
            EndDialogue();
            return;
        }


        ShowDialogue();
    }


    public void EndDialogue()
    {

        dialogueActive = false;

        onDialogueEnd?.Invoke();
        if (cutscene)
        {
            cutscene_.EndCutscene();
        }
        if (animator != null)
            animator.Play("popup_dialogue_hide");
        choices.SetActive(false);
    }


    private void OnDestroy()
    {
        // Nettoyage du singleton quand on quitte le jeu
        if (instance == this)
            instance = null;
    }
}