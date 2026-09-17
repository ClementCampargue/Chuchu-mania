using UnityEngine;

public class SC_trail_collision : MonoBehaviour
{
    private TrailRenderer trail;
    private EdgeCollider2D collider2D;

    void Start()
    {
        trail = GetComponent<TrailRenderer>();
        collider2D = GetComponent<EdgeCollider2D>();
    }

    void Update()
    {
        int count = trail.positionCount;

        if (count < 2)
            return;

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
