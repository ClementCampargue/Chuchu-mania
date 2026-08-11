using UnityEngine;

public class SC_shape_movement : MonoBehaviour
{
    public enum ShapeType
    {
        Circle,
        RoundedSquare,
        Triangle,
        Figure8,
        Line
    }

    public bool randomStartOnShape = true;

    [Header("Shape Settings")]
    public ShapeType shape = ShapeType.RoundedSquare;
    public float radius = 2f;
    public float speed = 2f;

    [Range(0.01f, 0.5f)]
    public float resolution = 0.05f;

    [Header("Line Settings")]
    [Tooltip("Rotation de la ligne en degrés. 0 = horizontale, 90 = verticale.")]
    public float lineRotation = 0f;

    [Header("Gizmos")]
    public bool showGizmos = true;
    public Color gizmosColor = Color.cyan;

    private float t;
    private Vector3 origin;

    private Vector2[] points;
    private float[] distances;
    private float totalLength;

    public SC_enemy_damage damage;

    void Start()
    {
        origin = transform.position;
        BuildCurve();

        if (randomStartOnShape)
        {
            t = Random.Range(0f, totalLength);
        }
    }

    void Update()
    {
        if (damage != null && damage.isKnockedBack)
        {
            enabled = false;
            return;
        }

        t += Time.deltaTime * speed;

        Vector2 pos = EvaluateUniform(t);

        transform.position = origin + new Vector3(pos.x, pos.y, 0f);
    }

    void BuildCurve()
    {
        int count = Mathf.Max(10, Mathf.CeilToInt(1f / resolution));

        points = new Vector2[count];
        distances = new float[count];

        Vector2 prev = EvaluateRaw(0f);
        float dist = 0f;

        for (int i = 0; i < count; i++)
        {
            float time = (float)i / (count - 1) * Mathf.PI * 2f;

            Vector2 p = EvaluateRaw(time);

            if (i > 0)
                dist += Vector2.Distance(prev, p);

            points[i] = p;
            distances[i] = dist;

            prev = p;
        }

        totalLength = Mathf.Max(dist, 0.0001f);
    }

    Vector2 EvaluateUniform(float time)
    {
        float tNorm = time % totalLength;

        for (int i = 1; i < distances.Length; i++)
        {
            if (distances[i] >= tNorm)
            {
                float t0 = distances[i - 1];
                float t1 = distances[i];

                float lerp = Mathf.InverseLerp(t0, t1, tNorm);

                return Vector2.Lerp(
                    points[i - 1],
                    points[i],
                    lerp
                );
            }
        }

        return points[^1];
    }

    Vector2 EvaluateRaw(float time)
    {
        switch (shape)
        {
            case ShapeType.Circle:
                return Circle(time);

            case ShapeType.RoundedSquare:
                return SuperShape(time, 4f);

            case ShapeType.Triangle:
                return Triangle(time);

            case ShapeType.Figure8:
                return Figure8(time);

            case ShapeType.Line:
                return Line(time);

            default:
                return Vector2.zero;
        }
    }

    Vector2 Circle(float t)
    {
        return new Vector2(
            Mathf.Cos(t),
            Mathf.Sin(t)
        ) * radius;
    }

    Vector2 SuperShape(float t, float n)
    {
        float cos = Mathf.Cos(t);
        float sin = Mathf.Sin(t);

        float x = Mathf.Sign(cos) *
                  Mathf.Pow(Mathf.Abs(cos), 2f / n) *
                  radius;

        float y = Mathf.Sign(sin) *
                  Mathf.Pow(Mathf.Abs(sin), 2f / n) *
                  radius;

        return new Vector2(x, y);
    }

    Vector2 Triangle(float t)
    {
        float angle = t % (Mathf.PI * 2f);

        Vector2[] v =
        {
            new Vector2(0, radius),
            new Vector2(-radius, -radius),
            new Vector2(radius, -radius)
        };

        float section = Mathf.PI * 2f / 3f;

        int i = Mathf.FloorToInt(angle / section);

        Vector2 a = v[i % 3];
        Vector2 b = v[(i + 1) % 3];

        float lerp = (angle % section) / section;

        return Vector2.Lerp(a, b, lerp);
    }

    Vector2 Figure8(float t)
    {
        return new Vector2(
            Mathf.Sin(t),
            Mathf.Sin(t) * Mathf.Cos(t)
        ) * radius;
    }

    Vector2 Line(float t)
    {
        float value = (Mathf.Sin(t) + 1f) * 0.5f;

        // Ligne horizontale
        Vector2 line = new Vector2(
            Mathf.Lerp(-radius, radius, value),
            0f
        );

        // Rotation de la ligne
        float angle = lineRotation * Mathf.Deg2Rad;

        float cos = Mathf.Cos(angle);
        float sin = Mathf.Sin(angle);

        return new Vector2(
            line.x * cos - line.y * sin,
            line.x * sin + line.y * cos
        );
    }

    void OnDrawGizmos()
    {
        if (!showGizmos)
            return;

        Gizmos.color = gizmosColor;

        Vector3 prev = transform.position;

        int steps = 100;

        for (int i = 0; i <= steps; i++)
        {
            float t = (float)i / steps * Mathf.PI * 2f;

            Vector2 p = EvaluateRaw(t);

            Vector3 world = Application.isPlaying
                ? origin + new Vector3(p.x, p.y, 0)
                : transform.position + new Vector3(p.x, p.y, 0);

            Gizmos.DrawLine(prev, world);

            prev = world;
        }
    }
}
