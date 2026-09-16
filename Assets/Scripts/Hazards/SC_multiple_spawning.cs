using UnityEngine;
using System.Collections;

public class SC_multiple_spawning : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject normalPrefab;
    public GameObject specialPrefab1; // Probabilité selon proba_spe_1
    public GameObject specialPrefab2; // Probabilité selon proba_spe_2

    [Header("Positions de spawn")]
    public Transform[] spawnPositions = new Transform[3];

    [Header("Temps entre les cycles")]
    public float startDelay = 3f;
    public float cycleDelay = 5f;
    public float randomDelay = 1.5f;

    [Header("Délai entre les colonnes")]
    public float columnDelay = 0.3f;
    public float randomColumnDelay = 0.15f;

    [Header("Probas")]
    public float proba_spe_1 = 3f;
    public float proba_spe_2 = 5f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        // Attente avant le premier cycle
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // Détermine le type de spécial pour CE cycle
            int specialType = GetSpecialType();

            // Choisit aléatoirement la colonne qui contiendra le spécial
            int specialColumn = -1;

            if (specialType != 0)
            {
                specialColumn = Random.Range(0, spawnPositions.Length);
            }

            // Spawn des 3 colonnes
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject prefabToSpawn = normalPrefab;

                // Si cette colonne est celle du spécial
                if (i == specialColumn)
                {
                    if (specialType == 1)
                    {
                        prefabToSpawn = specialPrefab1;
                    }
                    else if (specialType == 2)
                    {
                        prefabToSpawn = specialPrefab2;
                    }
                }

                // Spawn
                Instantiate(
                    prefabToSpawn,
                    spawnPositions[i].position,
                    spawnPositions[i].rotation
                );

                // Petit délai avant la colonne suivante
                if (i < spawnPositions.Length - 1)
                {
                    float delay = columnDelay +
                                  Random.Range(-randomColumnDelay, randomColumnDelay);

                    delay = Mathf.Max(0.01f, delay);

                    yield return new WaitForSeconds(delay);
                }
            }

            // Temps avant le prochain cycle
            float nextCycleDelay = cycleDelay +
                                   Random.Range(-randomDelay, randomDelay);

            nextCycleDelay = Mathf.Max(0.1f, nextCycleDelay);

            yield return new WaitForSeconds(nextCycleDelay);
        }
    }

    private int GetSpecialType()
    {
        // Spécial 2
        if (Random.Range(0f, proba_spe_2) < 1f)
        {
            return 2;
        }

        // Spécial 1
        if (Random.Range(0f, proba_spe_1) < 1f)
        {
            return 1;
        }

        // Aucun spécial
        return 0;
    }
}