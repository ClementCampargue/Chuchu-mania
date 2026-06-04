using UnityEngine;
using UnityEngine.UI;

public class SC_phase_material : MonoBehaviour
{
    public RawImage image;
    public Material material;
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
            image.material = material;
        }

    }
}
