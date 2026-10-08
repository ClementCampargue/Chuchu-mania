using UnityEngine;
using System.Collections;

public class SC_multiple_spawning : MonoBehaviour
{
    [Header("Prefabs")]
    public GameObject normalPrefab;
    public GameObject specialPrefab1;
    public GameObject specialPrefab2;

    [Header("Positions de spawn")]
    public Transform[] spawnPositions = new Transform[3];

    [Header("Temps entre les cycles")]
    public float startDelay = 3f;
    public float cycleDelay = 5f;
    public float randomDelay = 1.5f;

    [Header("Délai entre les colonnes")]
    public float columnDelay = 0.3f;
    public float randomColumnDelay = 0.15f;

    [Header("Probabilités sur 100")]
    [Range(0f, 100f)]
    public float proba_spe_1 = 3f;

    [Range(0f, 100f)]
    public float proba_spe_2 = 5f;

    private void Start()
    {
        StartCoroutine(SpawnRoutine());
    }

    private IEnumerator SpawnRoutine()
    {
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // Tirage indépendant
            bool spawnSpecial1 = Random.Range(0f, 100f) < proba_spe_1;
            bool spawnSpecial2 = Random.Range(0f, 100f) < proba_spe_2;

            int special1Column = -1;
            int special2Column = -1;

            // Choisir une colonne aléatoire pour le spécial 1
            if (spawnSpecial1)
            {
                special1Column = Random.Range(0, spawnPositions.Length);
            }

            // Choisir une colonne aléatoire pour le spécial 2
            if (spawnSpecial2)
            {
                special2Column = Random.Range(0, spawnPositions.Length);

                // Éviter la même colonne si les deux apparaissent
                if (spawnSpecial1 && spawnPositions.Length > 1)
                {
                    while (special2Column == special1Column)
                    {
                        special2Column = Random.Range(0, spawnPositions.Length);
                    }
                }
            }

            // Spawn des colonnes
            for (int i = 0; i < spawnPositions.Length; i++)
            {
                GameObject prefabToSpawn = normalPrefab;

                if (i == special1Column)
                {
                    prefabToSpawn = specialPrefab1;
                }
                else if (i == special2Column)
                {
                    prefabToSpawn = specialPrefab2;
                }

                Instantiate(
                    prefabToSpawn,
                    spawnPositions[i].position,
                    spawnPositions[i].rotation
                );

                // Délai entre les colonnes
                if (i < spawnPositions.Length - 1)
                {
                    float delay = columnDelay +
                                  Random.Range(
                                      -randomColumnDelay,
                                      randomColumnDelay
                                  );

                    delay = Mathf.Max(0.01f, delay);

                    yield return new WaitForSeconds(delay);
                }
            }

            // Délai avant le prochain cycle
            float nextCycleDelay = cycleDelay +
                                   Random.Range(
                                       -randomDelay,
                                       randomDelay
                                   );

            nextCycleDelay = Mathf.Max(0.1f, nextCycleDelay);

            yield return new WaitForSeconds(nextCycleDelay);
        }
    }
}