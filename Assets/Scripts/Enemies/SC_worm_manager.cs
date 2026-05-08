using System.Collections.Generic;
using UnityEngine;

public class SC_worm_manager : MonoBehaviour
{
    public List<Transform> points = new List<Transform>();
    public static SC_worm_manager manager;

    public List<SC_enemy_random_teleport> worms;

    [Header("Spawn")]
    public GameObject Prize;
    public Transform spawnPoint;
    private bool canwin;
    private void Awake()
    {
        manager = this;

        // Ajouter tous les enfants dans la liste
        foreach (Transform child in transform)
        {
            points.Add(child);
        }Invoke("delay_start", 1);
    }
    void delay_start()
    {
        canwin = true;
    }
    void Update()
    {
        // Si aucun worm dans la liste
        if (worms.Count == 0 && canwin)
        {
            Spawn_prize();
        }
    }

    void Spawn_prize()
    {
        GameObject newWorm = Instantiate(Prize, spawnPoint.position, Quaternion.identity);
        this.enabled = false;
    }
}