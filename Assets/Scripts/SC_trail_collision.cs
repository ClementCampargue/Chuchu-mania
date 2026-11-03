using UnityEngine;

public class SC_trail_collision : MonoBehaviour
{
    private TrailRenderer trail;
    private EdgeCollider2D collider2D;

    void Awake()
    {
        trail = GetComponent<TrailRenderer>();
        collider2D = GetComponent<EdgeCollider2D>();
    }

    private void OnEnable()
    {
        collider2D.enabled = false;

        if (trail != null)
            trail.Clear();

        if (collider2D != null)
            collider2D.points = new Vector2[0];

        collider2D.enabled = true;
    }
    private void OnDisable()
    {
        if (collider2D != null)
        {
            collider2D.enabled = false;
            collider2D.points = new Vector2[0];
        }

        if (trail != null)
            trail.Clear();
    }

    void Update()
    {
        if (!trail || !collider2D || !collider2D.enabled)
            return;

        int count = trail.positionCount;

        if (count < 2)
        {
            collider2D.points = new Vector2[0];
            return;
        }

        Vector3[] positions = new Vector3[count];
        trail.GetPositions(positions);

        Vector2[] points = new Vector2[count];

        for (int i = 0; i < count; i++)
        {
            points[i] = transform.InverseTransformPoint(positions[i]);
        }

        collider2D.points = points;
    }
}