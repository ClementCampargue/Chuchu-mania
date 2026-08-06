using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_NPC : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueData[] dialogues;

    [Header("Dialogue progression")]
    public bool loopLastDialogue = true;
    private int currentDialogueIndex = 0;
    private bool hasFinishedDialogue = false;


    [Header("Interaction")]
    public GameObject interaction_popup;
    public InputActionReference interactAction;
    private DialogueManager dialogue;

    private SC_player player;
    private bool playerInRange;

    public static bool dialogueActive = false;


    void Awake()
    {
        if (player == null)
            player = SC_player.instance;
    }


    void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }


    void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();

        dialogueActive = false;
    }


    void OnDestroy()
    {
        dialogueActive = false;
    }


    void Start()
    {
        if (interaction_popup != null)
            interaction_popup.SetActive(false);
    }


    void Update()
    {
        if (player == null)
            player = SC_player.instance;


        if (playerInRange)
        {
            if (!dialogueActive && !hasFinishedDialogue)
                interaction_popup.SetActive(true);


            if (interactAction.action.ReadValue<Vector2>().y > 0.5f && !dialogueActive)
            {
                StartTalking();
            }
        }
    }


    private void OnTriggerStay2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = true;

            if (player == null)
                player = collision.GetComponent<SC_player>();
        }
    }


    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            playerInRange = false;

            if (interaction_popup != null)
                interaction_popup.SetActive(false);
        }
    }


    void StartTalking()
    {
        if (dialogueActive || !player.isGrounded)
            return;


        if (dialogues.Length == 0)
        {
            Debug.LogWarning("Aucun dialogue assigné !");
            return;
        }


        if (dialogue == null)
            dialogue = DialogueManager.instance;


        if (dialogue == null)
        {
            Debug.LogError("Aucun DialogueManager trouvé !");
            return;
        }


        // Choix du dialogue actuel
        dialogue.currentDialogue = dialogues[currentDialogueIndex];
        dialogue.npc = this;
        dialogue.StartDialogue();


        dialogueActive = true;


        interaction_popup.SetActive(false);


        player.FaceTarget(transform);

        player.rb.constraints = RigidbodyConstraints2D.FreezeAll;
        player.canMove = false;

        player.anim_.Play("Idle");
    }


    public void EndTalking()
    {
        // Passage au dialogue suivant
        NextDialogue();


        player.enabled = false;
        Invoke(nameof(DelayEnd), 0.1f);
    }


    void NextDialogue()
    {
        if (currentDialogueIndex < dialogues.Length - 1)
        {
            currentDialogueIndex++;
        }
        else
        {
            // Dernier dialogue atteint
            if (loopLastDialogue)
            {
                currentDialogueIndex = dialogues.Length - 1;
            }
            else
            {
                hasFinishedDialogue = true;
            }
        }
    }


    void DelayEnd()
    {
        dialogueActive = false;


        if (player != null)
        {
            player.enabled = true;
            player.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.canMove = true;
        }
    }
}