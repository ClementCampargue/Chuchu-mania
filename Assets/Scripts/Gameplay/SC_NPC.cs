using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_NPC : MonoBehaviour
{
    [Header("Dialogue")]
    public SC_dialogue_system dialogue;
    public List<SO_dialogue_line> dialogues;

    [Header("Interaction")]
    public GameObject interaction_popup;
    public InputActionReference interactAction;

    private SC_player player;
    private bool playerInRange;

    public static bool dialogueActive = false;


    void Awake()
    {
        if (player == null)
            player = SC_player.instance;

        if (dialogue == null)
            dialogue = SC_dialogue_system.instance;
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

        if (dialogue != null)
            dialogue.onDialogueEnd -= EndTalking;

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

        if (dialogue == null)
            dialogue = SC_dialogue_system.instance;


        if (player == null || dialogue == null)
            return;


        if (playerInRange)
        {
       //     player.canJump = false;


            if (!dialogueActive)
                interaction_popup.SetActive(true);

            if (interactAction.action.ReadValue<Vector2>().y>0.5f && !dialogueActive)
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

            if (player != null)
                //  player.canJump = true;

                if (interaction_popup != null)
                interaction_popup.SetActive(false);
        }
    }


    void StartTalking()
    {
        if (dialogueActive || !player.isGrounded)
            return;


        dialogueActive = true;


        interaction_popup.SetActive(false);


        player.FaceTarget(transform);


        dialogue.dialogues = new List<SO_dialogue_line>(dialogues);

        dialogue.StartDialogue();


        player.rb.constraints = RigidbodyConstraints2D.FreezeAll;
        player.canMove = false;

        player.anim_.Play("Idle");


        dialogue.onDialogueEnd -= EndTalking;
        dialogue.onDialogueEnd += EndTalking;
    }



    void EndTalking()
    {
        player.enabled = false;
        Invoke(nameof(DelayEnd), 0.1f);
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