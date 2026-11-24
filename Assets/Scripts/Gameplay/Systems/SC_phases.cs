using System;
using UnityEngine;

public class SC_phases : MonoBehaviour
{
    public AudioSource audio_;
    [Header("Liste des phases (dans l'ordre)")]
    public GameObject[] phases; 
    public GameObject cosmicmode;
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
        SC_player.instance.hud_.SetTrigger("new");
        int previousPhase = currentPhaseIndex;
        audio_.Play();
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
    public void cosmic()
    {
        Invoke("delay_cosmic", SC_player.instance.transformFreezeTime);
    }
    void delay_cosmic()
    {
        cosmicmode.SetActive(true);

    }
    private void ActivateOnlyCurrentPhase()
    {
        for (int i = 0; i < phases.Length; i++)
        {
            phases[i].SetActive(i == currentPhaseIndex);
        }
    }
}
