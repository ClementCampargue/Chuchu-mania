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
        // Attente avant le premier cycle
        yield return new WaitForSeconds(startDelay);

        while (true)
        {
            // Chaque spécial a maintenant son propre tirage indépendant
            bool spawnSpecial1 = Random.Range(0f, 100f) < proba_spe_1;
            bool spawnSpecial2 = Random.Range(0f, 100f) < proba_spe_2;

            // Liste des colonnes disponibles
            int[] specialColumns = new int[spawnPositions.Length];

            for (int i = 0; i < specialColumns.Length; i++)
            {
                specialColumns[i] = i;
            }

            // Mélange des colonnes
            ShuffleArray(specialColumns);

            // On attribue les colonnes aux spéciaux
            int special1Column = -1;
            int special2Column = -1;

            if (spawnSpecial1)
            {
                special1Column = specialColumns[0];
            }

            if (spawnSpecial2)
            {
                // Si les deux doivent apparaître,
                // on prend obligatoirement une autre colonne
                if (spawnSpecial1 && spawnPositions.Length > 1)
                {
                    special2Column = specialColumns[1];
                }
                else
                {
                    special2Column = specialColumns[0];
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

    private void ShuffleArray(int[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            int temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
}