using UnityEngine;

public class SC_script_phase_enable : MonoBehaviour
{
    public MonoBehaviour scriptToActivate;
    public float phase;
    public float delay;
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
        if (phaseIndex == phase) 
        {
            Invoke("enabl", delay);
        }

    }

    void enabl()
    {
        scriptToActivate.enabled = true;
        this.enabled = false;
    }
}