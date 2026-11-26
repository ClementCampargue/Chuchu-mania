using UnityEngine;
using System.Collections.Generic;

public class SC_guard_point : MonoBehaviour
{
    [Header("Connexions")]
    public List<SC_guard_point> connections = new List<SC_guard_point>();

    [Header("Étage")]
    public int floorId = 0;
    public bool isFloorReference = false;

    [Header("Gizmos")]
    public float pointSize = 0.15f;
    public Color pointColor = Color.white;
    public Color connectionColor = Color.cyan;

    private void OnDrawGizmos()
    {
        Gizmos.color = pointColor;
        Gizmos.DrawSphere(transform.position, pointSize);
        Gizmos.color = connectionColor;
        if (connections == null) return;
        foreach (SC_guard_point node in connections)
        {
            if (node == null) continue;
            Vector3 end = Application.isPlaying
                ? SC_ScreenWarp.NearestImage(transform.position, node.transform.position)
                : node.transform.position;
            Gizmos.DrawLine(transform.position, end);
        }
    }
}
