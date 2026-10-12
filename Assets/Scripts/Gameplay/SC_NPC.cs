using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Events;

public class SC_NPC : MonoBehaviour
{
    [System.Serializable]
    public class DialogueEventEntry
    {
        [Tooltip("Index de la ligne de dialogue (commence à 0).")]
        public int dialogueIndex;

        [Tooltip("Coche : début de la ligne. Décoche : fin du typewriter.")]
        public bool onStart;

        [Tooltip("Actions à exécuter pour cette ligne.")]
        public UnityEvent action = new UnityEvent();
    }

    [Header("Dialogue")]
    public DialogueData[] dialogues;

    [Header("Dialogue Events")]
    public List<DialogueEventEntry> dialogueEvents =
        new List<DialogueEventEntry>();

    [Header("Dialogue Finished")]
    [Tooltip("Se déclenche lorsque le dernier dialogue est entièrement terminé par le typewriter.")]
    public UnityEvent onDialogueFinished = new UnityEvent();

    [Header("Dialogue progression")]
    public bool loopLastDialogue = true;

    private int currentDialogueIndex = 0;
    private bool hasFinishedDialogue = false;
    private bool interactWasPressed = false;

    [Header("Interaction")]
    public GameObject interaction_popup;
    public InputActionReference interactAction;

    private DialogueManager dialogue;
    private SC_player player;
    private bool playerInRange;

    public static bool dialogueActive = false;

    [Header("Player Settings")]
    public bool disable_player_on_start = true;
    public bool enable_player_on_end = true;
    public bool player_look_at_npc = true;
    public bool hide_dialogue_box_end = true;
    public bool player_onground_required = true;

    private void Awake()
    {
        if (player == null)
            player = SC_player.instance;
    }

    private void OnEnable()
    {
        if (interactAction != null)
            interactAction.action.Enable();
    }

    private void OnDisable()
    {
        if (interactAction != null)
            interactAction.action.Disable();

        dialogueActive = false;

        if (interaction_popup != null)
            interaction_popup.SetActive(false);
    }

    private void OnDestroy()
    {
        dialogueActive = false;
    }

    private void Start()
    {
        if (interaction_popup != null)
            interaction_popup.SetActive(false);
    }

    private void Update()
    {
        if (player == null)
            player = SC_player.instance;

        if (!playerInRange || interactAction == null)
            return;

        Vector2 stick = interactAction.action.ReadValue<Vector2>();

        bool interactPressed = stick.y > 0.9f;

        if (interactPressed && !interactWasPressed)
        {
            if (player != null &&
                !dialogueActive &&
                player.enabled &&
                player.canMove &&
                (!player_onground_required ||
                 (player.isGrounded &&
                  player.wasGrounded &&
                  Mathf.Abs(player.rb.linearVelocityX) < 0.1f &&
                  Mathf.Abs(player.rb.linearVelocityY) < 0.1f)))
            {
                StartTalking();
            }
        }

        interactWasPressed = interactPressed;

        if (interaction_popup != null)
        {
            interaction_popup.SetActive(
                !dialogueActive && !hasFinishedDialogue
            );
        }
    }

    private void OnTriggerStay2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerInRange = true;

        if (player == null)
            player = collision.GetComponent<SC_player>();
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (!collision.CompareTag("Player"))
            return;

        playerInRange = false;

        if (interaction_popup != null)
            interaction_popup.SetActive(false);
    }

    public void StartTalking()
    {
        if (dialogueActive)
            return;

        if (player == null)
            player = SC_player.instance;

        if (player == null)
            return;

        if (player_onground_required && !player.isGrounded)
            return;

        if (dialogues == null ||
            dialogues.Length == 0 ||
            currentDialogueIndex >= dialogues.Length)
        {
            Debug.LogWarning("Aucun dialogue assigné !", this);
            return;
        }

        if (dialogue == null)
            dialogue = DialogueManager.instance;

        if (dialogue == null)
        {
            Debug.LogError("Aucun DialogueManager trouvé !", this);
            return;
        }

        // Configure le dialogue avant de démarrer le typewriter.
        dialogue.currentDialogue =
            dialogues[currentDialogueIndex];

        dialogue.npc = this;

        dialogueActive = true;

        if (interaction_popup != null)
            interaction_popup.SetActive(false);

        if (player_look_at_npc)
            player.FaceTarget(transform);

        if (disable_player_on_start)
        {
            player.canMove = false;
            player.rb.linearVelocity = Vector2.zero;
            player.anim.Play("Idle");
        }

        dialogue.StartDialogue();
    }

    // =========================================================
    // DIALOGUE EVENTS
    // =========================================================

    // Appelé par SC_typewriter au début ou à la fin d'une ligne.
    public void TriggerDialogueEvents(
        int lineIndex,
        bool onStart)
    {
        for (int i = 0;
             i < dialogueEvents.Count;
             i++)
        {
            DialogueEventEntry entry =
                dialogueEvents[i];

            if (entry == null)
                continue;

            if (entry.dialogueIndex != lineIndex)
                continue;

            if (entry.onStart != onStart)
                continue;

            entry.action?.Invoke();
        }
    }

    // =========================================================
    // FIN DU DIALOGUE ENTIER
    // =========================================================

    /// <summary>
    /// Appelé par SC_typewriter lorsqu'une ligne vient
    /// d'être entièrement écrite.
    ///
    /// Cette méthode vérifie si nous sommes réellement
    /// arrivés à la fin de tous les dialogues du NPC.
    /// </summary>
    public void CheckDialogueFinished()
    {
        // Déjà terminé : on ne rappelle pas l'event.
        if (hasFinishedDialogue)
            return;

        // Si le dernier dialogue boucle,
        // il n'y a pas de fin définitive.
        if (loopLastDialogue)
            return;

        // Vérification de sécurité.
        if (dialogues == null ||
            dialogues.Length == 0)
        {
            return;
        }

        // Pas encore sur le dernier DialogueData.
        if (currentDialogueIndex != dialogues.Length - 1)
            return;

        // Le dialogue entier est terminé.
        hasFinishedDialogue = true;

        if (interaction_popup != null)
            interaction_popup.SetActive(false);

        // Déclenche l'UnityEvent dans l'Inspector.
        onDialogueFinished?.Invoke();
    }

    // =========================================================
    // END TALKING
    // =========================================================

    public void EndTalking()
    {
        NextDialogue();

        if (dialogue != null &&
            dialogue.anim != null &&
            hide_dialogue_box_end)
        {
            dialogue.anim.Play("popup_dialogue_hide");
        }

        if (player != null)
            player.jumpBufferCounter = 0;

        if (disable_player_on_start && player != null)
            player.enabled = false;

        CancelInvoke(nameof(DelayEnd));

        Invoke(nameof(DelayEnd), 0.1f);
    }

    private void NextDialogue()
    {
        if (dialogues == null ||
            dialogues.Length == 0)
        {
            return;
        }

        if (currentDialogueIndex < dialogues.Length - 1)
        {
            currentDialogueIndex++;
        }
        else if (loopLastDialogue)
        {
            currentDialogueIndex =
                dialogues.Length - 1;
        }
        else
        {
            hasFinishedDialogue = true;

            if (interaction_popup != null)
                interaction_popup.SetActive(false);
        }
    }

    private void DelayEnd()
    {
        dialogueActive = false;

        if (player != null &&
            enable_player_on_end)
        {
            player.enabled = true;

            player.rb.constraints =
                RigidbodyConstraints2D.FreezeRotation;

            player.canMove = true;
        }
    }
}