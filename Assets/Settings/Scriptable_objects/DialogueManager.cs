using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class DialogueManager : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueData currentDialogue;

    [Header("UI")]
    public TMP_Text characterNameText;
    public SC_typewriter typewriter;
    public SpriteRenderer portrait;

    [Header("Choices")]
    public GameObject choicePanel;
    public SC_Button[] choiceButtons;
    public TMP_Text[] choiceTexts;

    [Header("Animation")]
    public Animator anim;

    [Header("NPC")]
    public SC_NPC npc;

    [Header("Input")]
    public InputActionReference action;


    private int currentLineIndex;
    private DialogueLine currentLine;

    private bool waitingInput;
    private bool playingChoiceAnswer;

    public static DialogueManager instance;

    public bool cutscene;


    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }


    private void OnEnable()
    {
        if (action != null)
            action.action.Enable();
    }


    private void OnDisable()
    {
        if (action != null)
            action.action.Disable();
    }


    private void Start()
    {
        choicePanel.SetActive(false);
    }


    private void Update()
    {
        if (action == null)
            return;


        if (action.action.WasPressedThisFrame())
        {
            if (waitingInput)
            {
                NextLine();
            }
        }
    }



    public void StartDialogue()
    {
        choicePanel.SetActive(false);

        if (anim != null)
        {
            anim.enabled = true;
            anim.Play("popup_dialogue_show");
        }

        if (currentDialogue != null)
        {
            StartDialogue(currentDialogue);
        }
    }



    public void StartDialogue(DialogueData dialogue)
    {
        currentDialogue = dialogue;
        currentLineIndex = 0;

        DisplayLine(currentDialogue.lines[currentLineIndex]);
    }



    private void DisplayLine(DialogueLine line)
    {
        StartCoroutine(DisplayLineRoutine(line));
    }



    private IEnumerator DisplayLineRoutine(DialogueLine line)
    {
        currentLine = line;

        waitingInput = false;


        if (characterNameText != null)
        {
            if (string.IsNullOrEmpty(line.character.name))
            {
                characterNameText.text = "";
            }
            else
            {
                characterNameText.text =
                    line.character.name + " <sprite index=0>";
            }
        }


        if (portrait != null)
            portrait.sprite = line.character.portrait;


        choicePanel.SetActive(false);


        typewriter.typeSound = line.character.typesound;

        typewriter.TriggerText(line.text);


        while (typewriter.IsTyping())
        {
            yield return null;
        }


        if (line.hasChoices)
        {
            ShowChoices(line.choices);
        }
        else
        {
            typewriter.SetWaitInputVisible(true);

            waitingInput = true;
        }
    }



    private void NextLine()
    {
        // Si le texte est encore en train de s'écrire
        if (typewriter.IsTyping())
        {
            typewriter.FinishText();

            typewriter.SetWaitInputVisible(true);

            waitingInput = true;

            return;
        }


        waitingInput = false;


        // Les réponses de choix avancent dans leur coroutine
        if (playingChoiceAnswer)
        {
            return;
        }


        AdvanceMainDialogue();
    }



    private void AdvanceMainDialogue()
    {
        currentLineIndex++;


        if (currentLineIndex >= currentDialogue.lines.Length)
        {
            EndDialogue();
            return;
        }


        DisplayLine(currentDialogue.lines[currentLineIndex]);
    }




    private void ShowChoices(Choice[] choices)
    {
        choicePanel.SetActive(true);

        typewriter.SetWaitInputVisible(false);


        for (int i = 0; i < choiceButtons.Length; i++)
        {
            int index = i;


            choiceButtons[i].onClick.RemoveAllListeners();


            if (i < choices.Length)
            {
                choiceButtons[i].gameObject.SetActive(true);

                choiceTexts[i].text = choices[i].choiceText;


                choiceButtons[i].onClick.AddListener(() =>
                {
                    SelectChoice(choices[index]);
                });
            }
            else
            {
                choiceButtons[i].gameObject.SetActive(false);
            }
        }
    }




    private void SelectChoice(Choice choice)
    {
        choicePanel.SetActive(false);


        if (!string.IsNullOrEmpty(choice.actionID))
        {
            DialogueActionManager.instance.Execute(choice.actionID);
        }


        StartCoroutine(PlayChoiceAnswer(choice));
    }





    private IEnumerator PlayChoiceAnswer(Choice choice)
    {
        playingChoiceAnswer = true;


        foreach (DialogueLine answer in choice.answer)
        {
            yield return DisplayLineRoutine(answer);


            // Si cette réponse ouvre un nouveau choix
            if (answer.hasChoices)
            {
                while (choicePanel.activeSelf)
                {
                    yield return null;
                }

                continue;
            }


            // Attente du joueur
            while (waitingInput)
            {
                yield return null;
            }
        }


        playingChoiceAnswer = false;


        AdvanceMainDialogue();
    }




    private void EndDialogue()
    {
        StopAllCoroutines();
        waitingInput = false;

        playingChoiceAnswer = false;


        choicePanel.SetActive(false);


        if (npc != null)
        {
            npc.EndTalking();
        }


        if (anim != null)
        {
            anim.Play("popup_dialogue_hide");
        }


        if (cutscene)
        {
            SC_cutscene.instance.EndCutscene();
        }
    }
}