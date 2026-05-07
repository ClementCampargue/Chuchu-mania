using UnityEngine;
using System.Collections;

public class SC_bullet_spawner : MonoBehaviour
{
    [Header("Objet à Spawn")]
    public GameObject objetPrefab;

    [Header("Paramètres de Spawn")]
    public float intervalle = 2f;

    [Header("Délai après le Juice")]
    public float delaiAvantTir = 1f;

    public Transform pointSpawn;

    private float timer;
    private bool estEnTrainDeTirer = false;

    public SC_juiciness juice_fire;

    void Update()
    {
        if (!SC_level_intro.gameStarted) return;
        timer += Time.deltaTime;

        if (timer >= intervalle && !estEnTrainDeTirer)
        {
            StartCoroutine(TirAvecDelai());
            timer = 0f;
        }
    }

    IEnumerator TirAvecDelai()
    {
        estEnTrainDeTirer = true;

        // Joue le juice
        if (juice_fire != null)
        {
            juice_fire.PlayJuice();
        }

        // Attend avant de tirer
        yield return new WaitForSeconds(delaiAvantTir);

        SpawnObjet();

        estEnTrainDeTirer = false;
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