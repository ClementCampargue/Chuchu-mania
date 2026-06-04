using UnityEngine;

public class SC_spawner : MonoBehaviour
{
    [Header("Liste des prefabs à spawner")]
    public GameObject[] prefabs;

    [Header("Points de spawn (si vide = transform du spawner)")]
    public Transform[] spawnPoints;

    [Header("Mode de spawn")]
    public bool spawnInOrder = false;
    public bool randomizeOrder = false;
    public bool playOnAwake = false;

    [Header("Temps entre chaque spawn")]
    public float spawnInterval = 2f;

    [Header("Nombre maximum d'objets (0 = illimité)")]
    public int maxSpawnCount = 0;

    private int currentSpawnCount = 0;
    private float timer;
    private int currentPrefabIndex = 0;

    private int lastSpawnPointIndex = -1;

    private void Start()
    {
        if (randomizeOrder && prefabs != null && prefabs.Length > 0)
        {
            Shuffle(prefabs);
        }

        if (playOnAwake)
        {
            timer = spawnInterval;
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
            currentPrefabIndex = (currentPrefabIndex + 1) % prefabs.Length;
        }
        else
        {
            prefabChoisi = prefabs[Random.Range(0, prefabs.Length)];
        }

        Transform spawnPoint = GetRandomSpawnPoint();

        Instantiate(prefabChoisi, spawnPoint.position, spawnPoint.rotation);
        currentSpawnCount++;
    }

    private Transform GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
            return transform;

        if (spawnPoints.Length == 1)
            return spawnPoints[0];

        int newIndex;

        do
        {
            newIndex = Random.Range(0, spawnPoints.Length);
        }
        while (newIndex == lastSpawnPointIndex);

        lastSpawnPointIndex = newIndex;

        return spawnPoints[newIndex];
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
}