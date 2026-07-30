using System.Collections.Generic;
using UnityEngine;

public class SC_Cycle_spawn : MonoBehaviour
{
    public GameObject prefab;
    public float spawnInterval = 2f;
    public int maxObjects = 10;
    public int spawnPerWave = 3;

    float timer;

    void Update()
    {
        timer += Time.deltaTime;

        if (timer < spawnInterval)
            return;

        timer = 0f;

        List<Transform> freePoints = new List<Transform>();
        int currentObjects = 0;

        foreach (Transform point in transform)
        {
            if (point.childCount > 0)
                currentObjects++;
            else
                freePoints.Add(point);
        }

        int toSpawn = Mathf.Min(spawnPerWave, maxObjects - currentObjects, freePoints.Count);

        for (int i = 0; i < toSpawn; i++)
        {
            int index = Random.Range(0, freePoints.Count);
            Transform point = freePoints[index];

            Instantiate(prefab, point.position, point.rotation, point);

            freePoints.RemoveAt(index);
        }
    }
}