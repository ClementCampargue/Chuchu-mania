using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(EdgeCollider2D))]
public class SC_WaveMesh : MonoBehaviour
{
    [Header("Wave Settings")]
    public float amplitude = 0.2f;
    public float frequency = 2f;
    public float speed = 2f;

    [Header("Collider Settings")]
    public int colliderResolution = 30;
    public float colliderYThreshold = -0.5f; // ajuste selon ton mesh

    private MeshFilter meshFilter;
    private EdgeCollider2D edgeCollider;

    private Mesh originalMesh;
    private Mesh deformedMesh;

    private Vector3[] originalVertices;
    private Vector3[] deformedVertices;

    private Vector2[] colliderBasePoints;

    void Start()
    {
        meshFilter = GetComponent<MeshFilter>();
        edgeCollider = GetComponent<EdgeCollider2D>();

        originalMesh = meshFilter.mesh;

        deformedMesh = Instantiate(originalMesh);
        meshFilter.mesh = deformedMesh;

        originalVertices = originalMesh.vertices;
        deformedVertices = new Vector3[originalVertices.Length];

        SetupEdgeCollider();
    }

    void Update()
    {
        float time = Time.time * speed;

        // --- Mesh deformation ---
        for (int i = 0; i < originalVertices.Length; i++)
        {
            Vector3 v = originalVertices[i];

            float wave = Mathf.Sin(v.x * frequency + time) * amplitude;

            deformedVertices[i] = new Vector3(
                v.x,
                v.y + wave,
                v.z
            );
        }

        deformedMesh.vertices = deformedVertices;
        deformedMesh.RecalculateNormals();
        deformedMesh.RecalculateBounds();

        // --- Collider update ---
        UpdateCollider(time);
    }

    void SetupEdgeCollider()
    {
        // On filtre les vertices du bas du mesh
        System.Collections.Generic.List<Vector2> points = new System.Collections.Generic.List<Vector2>();

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        foreach (var v in originalVertices)
        {
            if (v.y <= colliderYThreshold)
            {
                minX = Mathf.Min(minX, v.x);
                maxX = Mathf.Max(maxX, v.x);
            }
        }

        for (int i = 0; i < colliderResolution; i++)
        {
            float t = (float)i / (colliderResolution - 1);

            float x = Mathf.Lerp(minX, maxX, t);

            // on prend le y de base du mesh
            float y = colliderYThreshold;

            points.Add(new Vector2(x, y));
        }

        colliderBasePoints = points.ToArray();
        edgeCollider.points = colliderBasePoints;
    }

    void UpdateCollider(float time)
    {
        if (colliderBasePoints == null) return;

        Vector2[] points = new Vector2[colliderBasePoints.Length];

        for (int i = 0; i < colliderBasePoints.Length; i++)
        {
            Vector2 p = colliderBasePoints[i];

            float wave = Mathf.Sin(p.x * frequency + time) * amplitude;

            points[i] = new Vector2(p.x, p.y + wave);
        }

        edgeCollider.points = points;
    }
}