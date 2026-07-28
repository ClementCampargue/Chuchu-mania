using System.Collections;
using UnityEngine;

public class SC_gravity_flip : MonoBehaviour
{
    public float flipInterval = 5f; // Temps entre chaque inversion

    private bool gravityInverted = false;
    private SC_player player;
    public bool gravity_up = false;
    public static SC_gravity_flip instance;

    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        player = SC_player.instance;

        if (player != null)
        {
            StartCoroutine(GravityLoop());
        }
    }

    private IEnumerator GravityLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(flipInterval);
            FlipGravity();
        }
    }

    private void FlipGravity()
    {
        gravityInverted = !gravityInverted;
        gravity_up = !gravity_up;
        if (gravityInverted)
        {
            // Gravité inversée
            player.rb.gravityScale = -player.base_gravity;

            player.transform.localScale = new Vector3(
                player.transform.localScale.x,
                -Mathf.Abs(player.transform.localScale.y),
                player.transform.localScale.z
            );
        }
        else
        {
            // Gravité normale
            player.rb.gravityScale = player.base_gravity;

            player.transform.localScale = new Vector3(
                player.transform.localScale.x,
                Mathf.Abs(player.transform.localScale.y),
                player.transform.localScale.z
            );
        }
    }
}