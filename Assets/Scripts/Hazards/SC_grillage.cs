using UnityEngine;

public class SC_grillage : MonoBehaviour
{
    [Header("Limits")]
    public float minX;
    public float maxX;
    public float minY;
    public float maxY;

    [Header("Auto compute from collider")]
    public bool autoCompute = true;
    public BoxCollider2D box;

    private void Awake()
    {
        if (autoCompute)
        {
            if (box == null)
                box = GetComponent<BoxCollider2D>();

            if (box != null)
            {
                Bounds b = box.bounds;
                minX = b.min.x;
                maxX = b.max.x;
                minY = b.min.y;
                maxY = b.max.y;
            }
        }
    }

    public Vector2 ClampPosition(Vector2 pos)
    {
        return new Vector2(
            Mathf.Clamp(pos.x, minX, maxX),
            Mathf.Clamp(pos.y, minY, maxY)
        );
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireCube(
            new Vector3((minX + maxX) / 2f, (minY + maxY) / 2f, 0),
            new Vector3(maxX - minX, maxY - minY, 1)
        );
    }
}