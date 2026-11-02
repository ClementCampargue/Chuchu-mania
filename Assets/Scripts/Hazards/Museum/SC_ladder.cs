using UnityEngine;

public class SC_ladder : MonoBehaviour
{
    [Header("Limites de l'échelle")]
    public Transform topPoint;
    public Transform bottomPoint;

    [Header("Position")]
    public float centerOffsetX = 0f;

    public float CenterX => transform.position.x + centerOffsetX;

    public float TopY => topPoint.position.y;

    public float BottomY => bottomPoint.position.y;

    public float ClampY(float y)
    {
        return Mathf.Clamp(y, BottomY, TopY);
    }

    private void OnDrawGizmosSelected()
    {
        if (topPoint == null || bottomPoint == null)
            return;

        Gizmos.color = Color.green;
        Gizmos.DrawSphere(topPoint.position, 0.12f);

        Gizmos.color = Color.red;
        Gizmos.DrawSphere(bottomPoint.position, 0.12f);

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            new Vector3(CenterX, BottomY, 0f),
            new Vector3(CenterX, TopY, 0f)
        );
    }
}