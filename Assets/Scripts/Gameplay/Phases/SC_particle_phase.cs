using UnityEngine;

public class SC_particle_phase : MonoBehaviour
{
    public ParticleSystem ps;
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
        // phase 2 -> 3 (index 2 -> 3 ou adapte selon ton système)
        if (phaseIndex == phase)
        {
            ps.Stop();
        }
    }
}