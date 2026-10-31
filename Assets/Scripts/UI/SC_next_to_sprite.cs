
using UnityEngine;

public class SC_next_to_sprite : MonoBehaviour
{
    public SpriteRenderer spriteRenderer;

    [Tooltip("Distance entre le sprite et l'objet")]
    public float distance = 0.1f;

    public bool right;

    private void Start()
    {

    }

    void LateUpdate()
    {
        if (spriteRenderer == null )
            return;

        Bounds bounds = spriteRenderer.bounds;

        Vector3 positionMonde;

        if (right)
        {
            // Bord droit du sprite
            positionMonde = new Vector3(
                bounds.max.x,
                bounds.center.y,
                bounds.center.z
            );
        }
        else
        {
            // Bord gauche du sprite
            positionMonde = new Vector3(
                bounds.min.x,
                bounds.center.y,
                bounds.center.z
            );
        }

        Vector3 direction = spriteRenderer.transform.right;

        Vector3 nouvellePosition = positionMonde
            + direction * (right ? distance : -distance);

        // Conserve la hauteur actuelle de l'objet
        nouvellePosition.y = transform.position.y;

        transform.position = nouvellePosition;
    }
}
