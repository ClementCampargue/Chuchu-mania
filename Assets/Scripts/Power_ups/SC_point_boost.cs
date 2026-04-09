using System.Collections;
using UnityEngine;

public class SC_point_boost : MonoBehaviour
{

    public static SC_point_boost Instance;

    [Header("Boost Settings")]
    public float boostMultiplier = 2f; // Multiplie les points
    public float boostDuration = 5f;   // Durée du boost en secondes

    private bool isBoostActive = false;

    private void Awake()
    {
        // Singleton simple
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }


    public void ActivateBoost(float duration = 1f, float multiplier = 2f)
    {
        boostDuration = duration;
        boostMultiplier = multiplier;
        StartCoroutine(BoostCoroutine());
    }

    private IEnumerator BoostCoroutine()
    {
        if (isBoostActive) yield break;
        isBoostActive = true;
        yield return new WaitForSeconds(boostDuration);
        boostMultiplier = 1;

        isBoostActive = false;
    }
}