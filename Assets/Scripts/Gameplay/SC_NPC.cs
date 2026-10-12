using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class SC_NPC : MonoBehaviour
{
    [Header("Dialogue")]
    public DialogueData[] dialogues;

    [Header("Dialogue progression")]
    public bool loopLastDialogue = true;
    private int currentDialogueIndex = 0;
    private bool hasFinishedDialogue = false;
    private bool interactWasPressed = false;

    [Header("Events")]
    [Tooltip("Appelé lorsque le dialogue se termine.")]
    public UnityEvent event_;
    public bool event_onstart;
    public int dialogue_to_trigger_event;

    [Header("Interaction")]
    public GameObject interaction_popup;
    public InputActionReference interactAction;
    private DialogueManager dialogue;

    private SC_player player;
    private bool playerInRange;

    public static bool dialogueActive = false;

    public bool disable_player_on_start = true;
    public bool enable_player_on_end = true;
    public bool player_look_at_npc = true;
    public bool hide_dialogue_box_end = true;
    public bool player_onground_required = true;
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

        if (!playerInRange || interactAction == null)
            return;

        Vector2 stick = interactAction.action.ReadValue<Vector2>();

        bool interactPressed = stick.y > 0.9f;

        if (interactPressed && !interactWasPressed)
        {
            if (!dialogueActive &&
                player.enabled &&
                player.canMove &&
                player.isGrounded &&
                player.wasGrounded &&
                Mathf.Abs(player.rb.linearVelocityX) < 0.1f &&
                Mathf.Abs(player.rb.linearVelocityY) < 0.1f)
            {
                StartTalking();
            }
        }

        interactWasPressed = interactPressed;

        if (!dialogueActive && !hasFinishedDialogue)
        {
            if (interaction_popup != null)
                interaction_popup.SetActive(true);
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


    public void StartTalking()
    {

        if (dialogueActive || !player.isGrounded && player_onground_required)
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

        dialogue.currentDialogue = dialogues[currentDialogueIndex];
        dialogue.npc = this;
        dialogue.StartDialogue();

        dialogueActive = true;

        if (interaction_popup != null)
            interaction_popup.SetActive(false);
        if(player_look_at_npc)
        {
            player.FaceTarget(transform);
        }
        if (disable_player_on_start)
        {
            player.canMove = false;
            player.rb.linearVelocity = Vector2.zero;
            player.anim.Play("Idle");
        }
        dialogue.typewriter.SetEvent(event_, dialogue_to_trigger_event, event_onstart);

    }


    public void EndTalking()
    {

        NextDialogue();
        if (dialogue.anim != null && hide_dialogue_box_end)
        {
            dialogue.anim.Play("popup_dialogue_hide");
        }
        player.jumpBufferCounter = 0;
        if (disable_player_on_start)
        {
            player.enabled = false;
        }
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

        if (player != null && enable_player_on_end)
        {
            player.enabled = true;
            player.rb.constraints = RigidbodyConstraints2D.FreezeRotation;
            player.canMove = true;
        }
    }
}