using UnityEngine;

public class SC_bullet_spawner : MonoBehaviour
{
    [Header("Objet à Spawn")]
    public GameObject objetPrefab; // L'objet que tu veux spawn

    [Header("Paramètres de Spawn")]
    public float intervalle = 2f; // Intervalle entre chaque spawn en secondes
    public Transform pointSpawn; // Optionnel, où spawn l'objet. Si null, spawn à la position du Spawner

    private float timer;

    void Update()
    {
        // Incrémente le timer
        timer += Time.deltaTime;

        // Si le timer dépasse l'intervalle, spawn l'objet
        if (timer >= intervalle)
        {
            SpawnObjet();
            timer = 0f; // Reset le timer
        }
    }

    void SpawnObjet()
    {
        if (objetPrefab != null)
        {
            Vector3 spawnPosition = pointSpawn != null ? pointSpawn.position : transform.position;
            Instantiate(objetPrefab, spawnPosition, Quaternion.identity);
        }
        else
        {
            Debug.LogWarning("Aucun objet prefab assigné au Spawner !");
        }
    }
}