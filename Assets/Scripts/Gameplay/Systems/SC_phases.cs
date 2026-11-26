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
    public SC_starbit_spawning spawning;
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
        if (SC_icecream_eat_system.instance.isPowerUpActive)
        {
            return;
        }
        int previousPhase = currentPhaseIndex;
        currentPhaseIndex++;

        if (currentPhaseIndex < phases.Length)
        {
            audio_.Play();
            phases[currentPhaseIndex].SetActive(true);
            OnPhaseChanged?.Invoke(currentPhaseIndex);
            SC_player.instance.hud_.SetTrigger("new");
        }
        else
        {
            Debug.Log("Toutes les phases sont terminées.");
        }
    }
    public void cosmic()
    {
        spawning.DestroyAllCollectibles();

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
