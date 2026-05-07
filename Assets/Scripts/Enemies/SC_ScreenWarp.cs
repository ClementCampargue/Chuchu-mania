using UnityEngine;

public class SC_ScreenWarp : MonoBehaviour
{
    [Header("Screen Limits")]
    public float leftLimit = -10f;
    public float rightLimit = 10f;

    [Header("Warning")]
    public float warningDistance = 2f;
    public float warningSpawnOffset = 2f;

    [Header("Prefabs")]
    public GameObject warningPrefab;

    private bool hasSpawnedWarning;

    void Update()
    {
        HandleWarning();
        HandleScreenWrap();
    }

    void HandleWarning()
    {
        Vector3 pos = transform.position;

        bool inRightWarningZone =
            pos.x > rightLimit - warningDistance;

        bool inLeftWarningZone =
            pos.x < leftLimit + warningDistance;

        // Spawn seulement une fois
        if (!hasSpawnedWarning)
        {
            // Approche du bord droit
            if (inRightWarningZone)
            {
                SpawnWarning(
                    new Vector3(
                        leftLimit + warningSpawnOffset,
                        pos.y,
                        pos.z
                    )
                );

                hasSpawnedWarning = true;
            }

            // Approche du bord gauche
            else if (inLeftWarningZone)
            {
                SpawnWarning(
                    new Vector3(
                        rightLimit - warningSpawnOffset,
                        pos.y,
                        pos.z
                    )
                );

                hasSpawnedWarning = true;
            }
        }

        // Reset quand l'ennemi quitte les zones
        if (!inRightWarningZone && !inLeftWarningZone)
        {
            hasSpawnedWarning = false;
        }
    }

    void SpawnWarning(Vector3 position)
    {
        if (warningPrefab == null)
            return;

        GameObject warning = Instantiate(
            warningPrefab,
            position,
            Quaternion.identity
        );

        warning.transform.localScale = transform.localScale;
    }

    void HandleScreenWrap()
    {
        Vector3 pos = transform.position;

        if (pos.x > rightLimit)
        {
            pos.x = leftLimit;
        }
        else if (pos.x < leftLimit)
        {
            pos.x = rightLimit;
        }

        transform.position = pos;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;

        Gizmos.DrawLine(
            new Vector3(leftLimit, -50, 0),
            new Vector3(leftLimit, 50, 0)
        );

        Gizmos.DrawLine(
            new Vector3(rightLimit, -50, 0),
            new Vector3(rightLimit, 50, 0)
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            new Vector3(leftLimit + warningDistance, -50, 0),
            new Vector3(leftLimit + warningDistance, 50, 0)
        );

        Gizmos.DrawLine(
            new Vector3(rightLimit - warningDistance, -50, 0),
            new Vector3(rightLimit - warningDistance, 50, 0)
        );
    }
}