using UnityEngine;

public class SC_noise_anim : MonoBehaviour
{
    [SerializeField] private float amplitude = 0.5f;
    [SerializeField] private float frequency = 1f;

    private Vector3 originPosition;
    private Vector3 noiseOffset;

    private void Start()
    {
        originPosition = transform.position;

        // Seed différent pour chaque objet
        noiseOffset = new Vector3(
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f),
            Random.Range(0f, 1000f)
        );
    }

    private void Update()
    {
        float t = Time.time * frequency;

        float x = (Mathf.PerlinNoise(t + noiseOffset.x, 0f) - 0.5f) * 2f;
        float y = (Mathf.PerlinNoise(t + noiseOffset.y, 10f) - 0.5f) * 2f;
        float z = (Mathf.PerlinNoise(t + noiseOffset.z, 20f) - 0.5f) * 2f;

        Vector3 offset = new Vector3(x, y, z) * amplitude;

        transform.position = originPosition + offset;
    }
}