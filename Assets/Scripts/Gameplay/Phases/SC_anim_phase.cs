using UnityEngine;

public class SC_anim_phase : MonoBehaviour
{
    public Animator animator;
    public float phase;

    private void OnEnable()
    {
        SC_phases.OnPhaseChanged += HandlePhaseChanged;
    }

    private void OnDisable()
    {
        SC_phases.OnPhaseChanged -= HandlePhaseChanged;
    }

    void HandlePhaseChanged(int phaseIndex)
    {
        if (phaseIndex == phase) // exemple : passage à la phase 3 (index 2)
        {
            animator.enabled = true;
            animator.SetTrigger("phase");
        }

    }
}
