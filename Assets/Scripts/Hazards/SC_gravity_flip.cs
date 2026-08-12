using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_gravity_flip : MonoBehaviour
{
    public float flipInterval = 5f; // Temps entre chaque inversion

    private bool gravityInverted = false;
    private SC_player player;
    public bool gravity_up = false;
    public static SC_gravity_flip instance;
    public static event Action<bool> OnGravityChanged;
    public Animator anim;
    private sc_health_system health;
    public List<GameObject> audio;
    private void Awake()
    {
        instance = this;
    }
    private void Start()
    {
        player = SC_player.instance;
        health = sc_health_system.instance;

        if (player != null)
        {
            StartCoroutine(GravityLoop());
        }

    }
    private void OnDisable()
    {foreach (GameObject gb in audio)
        
        {
            Destroy(gb);

        }
    }
    private void Update()
    {
        if(health.current_health == 0)
        {
            StopAllCoroutines();
            anim.enabled = false;
            flipInterval = 1000000000;
        }
    }

    private IEnumerator GravityLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(flipInterval);
            trigger_lights();
        }
    }
    public void trigger_lights()
    {
        if (anim.enabled)
        {
            anim.SetTrigger("switch");
        }
        else
        {
            anim.enabled = true;

        }
    }

    private void OnDestroy()
    {
        if (gravityInverted)
        {
            // Gravité inversée
            player.rb.gravityScale = -player.base_gravity;

            player.transform.localScale = new Vector3(
                player.transform.localScale.x,
                Mathf.Abs(player.transform.localScale.y),
                player.transform.localScale.z
            );
        }
    }
    public void FlipGravity()
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
        OnGravityChanged?.Invoke(gravity_up);
    }
}