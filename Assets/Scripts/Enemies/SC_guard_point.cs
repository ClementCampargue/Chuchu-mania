using UnityEngine;
using System.Collections.Generic;

public class SC_guard_point : MonoBehaviour
{
    [Header("Points connectés")]
    public List<SC_guard_point> connections = new List<SC_guard_point>();

    [Header("Gizmo")]
    public float pointSize = 0.15f;
    public Color pointColor = Color.white;
    public Color connectionColor = Color.cyan;

    private void OnDrawGizmos()
    {
        // Dessine le point
        Gizmos.color = pointColor;
        Gizmos.DrawSphere(transform.position, pointSize);

        // Dessine les connexions
        Gizmos.color = connectionColor;

        foreach (SC_guard_point node in connections)
        {
            if (node == null)
                continue;

            Gizmos.DrawLine(
                transform.position,
                node.transform.position
            );
        }
    }
}
