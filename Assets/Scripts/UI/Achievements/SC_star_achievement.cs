using UnityEngine;
using System.Collections.Generic;

public class SC_star_achievement : MonoBehaviour
{
    [System.Serializable]
    public class Connection
    {
        public GameObject objectA;
        public GameObject objectB;
    }

    [Header("Condition")]
    [SerializeField] private bool debloque = false;
    [SerializeField] private bool red = false;

    [Header("Connexions")]
    [SerializeField] private List<Connection> connections = new List<Connection>();

    [Header("Lignes")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float distanceFromCenter = 0.5f;
    [SerializeField] private Material lineMaterial;


    public SpriteRenderer spr;
    public Sprite default_;
    public Sprite unlocked_;
    public Sprite red_;

    private List<LineRenderer> lines = new List<LineRenderer>();

    void Start()
    {
        UpdateLines();
    }

    void Update()
    {
        if(debloque)
        {
            spr.sprite = unlocked_;
        }
        else if (red)
        {
            spr.sprite = red_;
        }
        else
        {
            spr.sprite = default_;
        }

        if (debloque)
        {
            UpdateLines();
        }
        else
        {
            ClearLines();
        }
    }

    private void UpdateLines()
    {
        ClearLines();

        if (!debloque)
            return;

        foreach (Connection connection in connections)
        {
            if (connection.objectA == null || connection.objectB == null)
                continue;

            CreateLine(connection.objectA, connection.objectB);
        }
    }

    private void CreateLine(GameObject objectA, GameObject objectB)
    {
        GameObject lineObject = new GameObject("AchievementLine");

        lineObject.transform.SetParent(transform);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sortingOrder = 10;
        line.positionCount = 2;
        line.useWorldSpace = true;

        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (lineMaterial != null)
            line.material = lineMaterial;

        Vector3 posA = objectA.transform.position;
        Vector3 posB = objectB.transform.position;

        Vector3 direction = (posB - posA).normalized;

        // Départ de la ligne à une certaine distance du centre de A
        Vector3 startPosition = posA + direction * distanceFromCenter;

        // Arrivée de la ligne à une certaine distance du centre de B
        Vector3 endPosition = posB - direction * distanceFromCenter;

        line.SetPosition(0, startPosition);
        line.SetPosition(1, endPosition);

        lines.Add(line);
    }

    private void ClearLines()
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        lines.Clear();
    }
}
