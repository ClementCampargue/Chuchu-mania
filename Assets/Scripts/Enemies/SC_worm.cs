using UnityEngine;
using System.Collections.Generic;

public class SC_enemy_random_teleport : MonoBehaviour
{
    [Header("Teleport Points")]
    private List<Transform> teleportPoints;

    [Header("Teleport")]
    public float teleportInterval = 3f;

    [Header("Detection")]
    public Vector2 checkSize = new Vector2(1f, 1f);

    [Tooltip("Offset appliqué uniquement à la zone de détection")]
    public Vector2 detectionOffset = new Vector2(0f, 0f);

    [Tooltip("Objets empêchant le TP")]
    public LayerMask blockedLayers;
    [Header("Stun")]
    public Transform reactionCheck;
    public float reactionRadius = 0.5f;
    public LayerMask reactionLayer;

    public string reactionTrigger = "hit";
    public LayerMask playerLayer;

    private bool hasReacted = false;
    private float timer;
    public SC_juiciness juice;
    public Animator anim;
    private void Start()
    {
        teleportPoints = SC_worm_manager.manager.points;
        SC_worm_manager.manager.worms.Add(this);
        timer = Random.Range(teleportInterval/2, teleportInterval);
    }
    void Update()
    {
        CheckReaction();

        timer += Time.deltaTime;

        if (timer >= teleportInterval)
        {
            timer = 0f;
            teleport();
        }
    }
    void CheckReaction()
    {
        if (hasReacted)
            return;

        Collider2D hit =
            Physics2D.OverlapCircle(
                reactionCheck.position,
                reactionRadius,
                reactionLayer
            );

        Collider2D hit2 =
            Physics2D.OverlapCircle(
                reactionCheck.position,
                reactionRadius,
                playerLayer
            );

        if (hit != null )
        {
            hasReacted = true;
            juice.PlayJuice();
            anim.SetTrigger(reactionTrigger);

        }

        if ( hit2 != null && SC_icecream_eat_system.instance.isPowerUpActive)
        {
            hasReacted = true;
            juice.PlayJuice();
            anim.SetTrigger(reactionTrigger);
            SC_player.instance.anim.SetTrigger("Punch");

        }
    }
    private void OnDestroy()
    {
        if(!SC_icecream_eat_system.instance.isPowerUpActive)
        {
            SC_worm_manager.manager.worms.Remove(this);
        }
    }
    void teleport()
    {
        anim.SetTrigger("teleport");

    }

    public  void TeleportToRandomPoint()
    {
        if (teleportPoints.Count == 0)
            return;

        int randomStart = Random.Range(0, teleportPoints.Count);

        for (int i = 0; i < teleportPoints.Count; i++)
        {
            int index = (randomStart + i) % teleportPoints.Count;

            Transform targetPoint = teleportPoints[index];

            Vector2 checkPos =
                (Vector2)targetPoint.position + detectionOffset;

            // Check rectangle avec offset
            Collider2D blocked =
                Physics2D.OverlapBox(
                    checkPos,
                    checkSize,
                    0f,
                    blockedLayers
                );

            if (blocked == null)
            {
                transform.position = targetPoint.position;
                return;
            }
        }

        Debug.Log("Aucun point valide pour le téléport.");
    }

    void OnDrawGizmosSelected()
    {
        if (teleportPoints == null)
            return;

        Gizmos.color = Color.cyan;

        foreach (Transform point in teleportPoints)
        {
            if (point != null)
            {
                Vector2 checkPos =
                    (Vector2)point.position + detectionOffset;

                Gizmos.DrawWireCube(checkPos, checkSize);
            }
        }
        if (reactionCheck != null)
        {
            Gizmos.color = Color.magenta;

            Gizmos.DrawWireSphere(
                reactionCheck.position,
                reactionRadius
            );
        }
    }
}