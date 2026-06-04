using UnityEngine;

public class SC_spawner : MonoBehaviour
{
    [Header("Liste des prefabs à spawner")]
    public GameObject[] prefabs;

    [Header("Mode de spawn")]
    public bool spawnInOrder = false;
    public bool RandomizeOrder = false;
    public bool PlayOnAwake = false;

    [Header("Temps entre chaque spawn")]
    public float spawnInterval = 2f;

    [Header("Nombre maximum d'objets (0 = illimité)")]
    public int maxSpawnCount = 0;

    private int currentSpawnCount = 0;
    private float timer;
    private int currentPrefabIndex = 0;

    private void Start()
    {
        if (RandomizeOrder)
        {
            Shuffle(prefabs);
        }
        if (PlayOnAwake)
        {
            timer = spawnInterval;
        }
    }
    private void Shuffle(GameObject[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);

            GameObject temp = array[i];
            array[i] = array[randomIndex];
            array[randomIndex] = temp;
        }
    }
    private void Update()
    {
        timer += Time.deltaTime;

        if (timer >= spawnInterval)
        {
            timer = 0f;

            if (maxSpawnCount == 0 || currentSpawnCount < maxSpawnCount)
            {
                SpawnObject();
            }
        }
    }

    private void SpawnObject()
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            Debug.LogWarning("Aucun prefab assigné au spawner !");
            return;
        }

        GameObject prefabChoisi;

        if (spawnInOrder)
        {
            prefabChoisi = prefabs[currentPrefabIndex];

            // Passe au suivant et revient au début à la fin de la liste
            currentPrefabIndex = (currentPrefabIndex + 1) % prefabs.Length;
        }
        else
        {
            prefabChoisi = prefabs[Random.Range(0, prefabs.Length)];
        }

        Instantiate(prefabChoisi, transform.position, transform.rotation);
        currentSpawnCount++;
    }
}