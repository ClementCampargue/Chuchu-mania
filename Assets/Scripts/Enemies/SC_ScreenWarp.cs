using UnityEngine;

[DefaultExecutionOrder(100)]
public class SC_ScreenWarp : MonoBehaviour
{
    private Vector2 limit;

    [Header("Warning")]
    public float warningDistance = 2f;
    public float warningSpawnOffset = 2f;

    [Header("Prefabs")]
    public GameObject warningPrefab;

    private bool hasSpawnedWarning;

    public static float LeftLimit
    {
        get { return SC_level_master.instance != null ? SC_level_master.instance.limits.x : -10f; }
    }

    public static float RightLimit
    {
        get { return SC_level_master.instance != null ? SC_level_master.instance.limits.y : 10f; }
    }

    public static float Width => Mathf.Max(0.001f, RightLimit - LeftLimit);

    // Renvoie la copie de target la plus proche de from, en tenant compte du wrap horizontal.
    public static Vector3 NearestImage(Vector3 from, Vector3 target)
    {
        float width = Width;
        float dx = target.x - from.x;
        if (dx > width * 0.5f) target.x -= width;
        else if (dx < -width * 0.5f) target.x += width;
        return target;
    }

    public static Vector3 WrappedDelta(Vector3 from, Vector3 to)
    {
        return NearestImage(from, to) - from;
    }

    public static float WrappedDistance(Vector3 a, Vector3 b)
    {
        return WrappedDelta(a, b).magnitude;
    }

    public static Vector3 MoveTowards(Vector3 from, Vector3 to, float maxDistance)
    {
        return Vector3.MoveTowards(from, NearestImage(from, to), maxDistance);
    }

    private void Start()
    {
        if (SC_level_master.instance == null)
        {
            Debug.LogError("SC_ScreenWarp : SC_level_master.instance est introuvable.", this);
            enabled = false;
            return;
        }
        limit = SC_level_master.instance.limits;
    }

    private void Update()
    {
        if (SC_level_master.instance != null) limit = SC_level_master.instance.limits;
        HandleWarning();
        HandleScreenWrap();
    }

    private void HandleWarning()
    {
        Vector3 pos = transform.position;
        bool inRightWarningZone = pos.x > limit.y - warningDistance;
        bool inLeftWarningZone = pos.x < limit.x + warningDistance;

        if (!hasSpawnedWarning)
        {
            if (inRightWarningZone)
            {
                SpawnWarning(new Vector3(limit.x + warningSpawnOffset, pos.y, pos.z));
                hasSpawnedWarning = true;
            }
            else if (inLeftWarningZone)
            {
                SpawnWarning(new Vector3(limit.y - warningSpawnOffset, pos.y, pos.z));
                hasSpawnedWarning = true;
            }
        }

        if (!inRightWarningZone && !inLeftWarningZone) hasSpawnedWarning = false;
    }

    private void SpawnWarning(Vector3 position)
    {
        if (warningPrefab == null) return;
        GameObject warning = Instantiate(warningPrefab, position, Quaternion.identity);
        warning.transform.localScale = transform.localScale;
    }

    private void HandleScreenWrap()
    {
        Vector3 pos = transform.position;
        if (pos.x > limit.y) pos.x = limit.x;
        else if (pos.x < limit.x) pos.x = limit.y;
        transform.position = pos;
    }

    private void OnDrawGizmosSelected()
    {
        Vector2 bounds = SC_level_master.instance != null ? SC_level_master.instance.limits : limit;
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(bounds.x, -50, 0), new Vector3(bounds.x, 50, 0));
        Gizmos.DrawLine(new Vector3(bounds.y, -50, 0), new Vector3(bounds.y, 50, 0));
        Gizmos.color = Color.yellow;
        Gizmos.DrawLine(new Vector3(bounds.x + warningDistance, -50, 0), new Vector3(bounds.x + warningDistance, 50, 0));
        Gizmos.DrawLine(new Vector3(bounds.y - warningDistance, -50, 0), new Vector3(bounds.y - warningDistance, 50, 0));
    }
}
