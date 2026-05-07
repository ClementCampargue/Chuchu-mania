using System;
using UnityEngine;

public class SC_phases : MonoBehaviour
{
    [Header("Liste des phases (dans l'ordre)")]
    public GameObject[] phases;

    private int currentPhaseIndex = 0;
    public static SC_phases instance;

    public static event Action<int> OnPhaseChanged;

    private void Awake()
    {
        instance = this;
    }

    void Start()
    {
        ActivateOnlyCurrentPhase();
        OnPhaseChanged?.Invoke(currentPhaseIndex);
    }

    public void NextPhase()
    {
        int previousPhase = currentPhaseIndex;

        currentPhaseIndex++;

        if (currentPhaseIndex < phases.Length)
        {
            phases[currentPhaseIndex].SetActive(true);
            OnPhaseChanged?.Invoke(currentPhaseIndex);
        }
        else
        {
            Debug.Log("Toutes les phases sont terminées.");
        }
    }

    private void ActivateOnlyCurrentPhase()
    {
        for (int i = 0; i < phases.Length; i++)
        {
            phases[i].SetActive(i == currentPhaseIndex);
        }
    }
}
