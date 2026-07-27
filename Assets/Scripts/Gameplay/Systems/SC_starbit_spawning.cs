using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_starbit_spawning : MonoBehaviour
{
    private List<Transform> spawnPoints = new List<Transform>();
    public int numberToSpawn = 10;
    public GameObject[] collectiblePrefabs;

    public Transform player;
    public float minDistanceFromPlayer = 5f;

    public float delay_respawn = 3f;

    [Header("D�lai entre chaque spawn")]
    public float delayBetweenSpawns = 0.5f;

    private List<GameObject> currentCollectibles = new List<GameObject>();
    public static SC_starbit_spawning instance;

    private void Awake()
    {
        instance = this;
    }
    void Start()
    {        player = SC_player.instance.transform;

        foreach (Transform child in transform)
        {
            spawnPoints.Add(child);
        }
    }
    public void Spawn_collectibles()
    {
        StartCoroutine(SpawnCollectibles());
    }

    IEnumerator SpawnCollectibles()
    {
        currentCollectibles.Clear();

        List<Transform> availablePoints = new List<Transform>(spawnPoints);

        for (int i = 0; i < numberToSpawn; i++)
        {
            if (availablePoints.Count == 0)
                yield break;

            Transform spawnPoint = null;
            int safety = 0;

            while (availablePoints.Count > 0 && safety < 50)
            {
                int pointIndex = Random.Range(0, availablePoints.Count);
                Transform candidate = availablePoints[pointIndex];

                float distance = Vector3.Distance(player.position, candidate.position);

                if (distance >= minDistanceFromPlayer)
                {
                    spawnPoint = candidate;
                    availablePoints.RemoveAt(pointIndex);
                    break;
                }
                else
                {
                    availablePoints.RemoveAt(pointIndex);
                }

                safety++;
            }

            if (spawnPoint == null)
                yield break;

            int prefabIndex = Random.Range(0, collectiblePrefabs.Length);
            GameObject prefab = collectiblePrefabs[prefabIndex];

            GameObject obj = Instantiate(prefab, spawnPoint.position, Quaternion.identity);

            obj.GetComponent<SC_starbit>().spawner = this;

            currentCollectibles.Add(obj);

            // D�lai avant le prochain spawn
            yield return new WaitForSeconds(delayBetweenSpawns);
        }
    }

    public void OnCollectiblePicked(GameObject obj)
    {
        currentCollectibles.Remove(obj);

        if (currentCollectibles.Count == 0)
        {
            Invoke(nameof(StartRespawn), delay_respawn);
        }
    }

    void StartRespawn()
    {
        StartCoroutine(SpawnCollectibles());
    }
}