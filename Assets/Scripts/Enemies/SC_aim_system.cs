
using UnityEngine;

[ExecuteAlways]
public class SC_aim_system : MonoBehaviour
{
    [Header("Points")]
    public Transform startPoint;
    public Vector3 targetPoint;

    [Header("Sprite")]
    public SpriteRenderer spriteRenderer;

    [Header("Target")]
    public Transform targetTransform;

    [Header("Settings")]
    public float tileSize = 1f;
    public bool updateEveryFrame = true;

    private void Update()
    {
        if (updateEveryFrame)
            UpdateSprite();
    }

    private void UpdateSprite()
    {
        if (startPoint == null || targetPoint == null || spriteRenderer == null)
            return;

        Vector3 direction = targetPoint - startPoint.position;
        float distance = direction.magnitude;

        // Position au milieu des deux points
        transform.position = (startPoint.position + targetPoint) / 2f;

        // Rotation vers la cible
        // float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        // transform.rotation = Quaternion.Euler(0f, 0f, angle);

        // Taille du sprite
        transform.localScale = new Vector3(
            1f,
            transform.localScale.y,
            transform.localScale.z
        );

        // Tiling du SpriteRenderer
        spriteRenderer.drawMode = SpriteDrawMode.Tiled;

        spriteRenderer.size = new Vector2(
            distance,
            tileSize
        );

        // Placement du transform secondaire sur la cible
        if (targetTransform != null)
        {
            targetTransform.position = targetPoint;
        }
    }
}
