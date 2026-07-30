using UnityEngine;

public class SC_firework : MonoBehaviour
{
    [Header("Projectile")]
    public GameObject projectilePrefab;

    [Header("Nombre de projectiles")]
    public int minProjectiles = 3;
    public int maxProjectiles = 8;

    [Header("Disposition")]
    public float rayon = 1f;
    public bool orienterVersExterieur = true;
    public Transform spawn_point;
    public void SpawnProjectiles()
    {
        int nombre = Random.Range(minProjectiles, maxProjectiles + 1);

        float angleStep = 360f / nombre;

        for (int i = 0; i < nombre; i++)
        {
            float angle = angleStep * i;

            // Conversion angle -> direction
            Vector2 direction = new Vector2(
                Mathf.Cos(angle * Mathf.Deg2Rad),
                Mathf.Sin(angle * Mathf.Deg2Rad)
            );

            Vector3 position = spawn_point.position + (Vector3)(direction * rayon);

            GameObject projectile = Instantiate(projectilePrefab, position, Quaternion.identity);

            if (orienterVersExterieur)
            {
                float rotation = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
                projectile.transform.rotation = Quaternion.Euler(0, 0, rotation);
            }


        }
    }


}