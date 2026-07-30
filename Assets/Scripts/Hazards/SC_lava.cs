using System.Collections;
using UnityEngine;

public class SC_lava : MonoBehaviour
{
    [Header("Knockback")]
    public float launchForceY = 12f;
    public float launchForceX = 4f;

    [Header("Control reduction")]
    public float reducedControlTime = 1.2f;
    public float airControlMultiplier = 0.3f;

    SC_player player;
    private void Start()
    {
        player = SC_player.instance;
    }
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        Launch(player);

    }

    void Launch(SC_player player)
    {
        Debug.Log("lava");
        float dir = Mathf.Sign(player.transform.position.x - transform.position.x);
        if (dir == 0) dir = 1;

        Vector2 launchVelocity = new Vector2(dir * launchForceX, launchForceY * transform.localScale.y);

        player.LavaHit(
            launchVelocity,
            airControlMultiplier,
            reducedControlTime
        );
    }
}