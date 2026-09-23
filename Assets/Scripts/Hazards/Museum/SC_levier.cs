using System.Collections.Generic;
using UnityEngine;

public class SC_levier : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private Animator animator;
    [SerializeField] private string animationBool = "Activated";

    private bool activated = false;
    private void Start()
    {
        if (animator == null)
            animator = GetComponent<Animator>();

        // Enregistrement auprès de l'instance du système d'alarme
        if (SC_alarm_system.Instance != null)
        {
            SC_alarm_system.Instance.RegisterLever(this);
        }
    }

    private void OnDestroy()
    {
        if (SC_alarm_system.Instance != null)
        {
            SC_alarm_system.Instance.UnregisterLever(this);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if(collision.tag == "Player")
        {
            ResetLever();
        }
    }
    public void ActivateLever()
    {
        if (activated)
            return;

        activated = true;
        animator.enabled = true;
        if (animator != null)
        {
            animator.SetBool(animationBool, true);
        }


    }

    public void ResetLever()
    {
        activated = false;

        if (animator != null)
        {
            animator.SetBool(animationBool, false);
        }
        SC_alarm_system.Instance.LeverActivated(this);
    }

    public bool IsActivated()
    {
        return activated;
    }
}

