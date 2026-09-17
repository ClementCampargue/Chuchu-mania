using UnityEngine;

public class SC_trail_particles : MonoBehaviour
{
    public TrailRenderer trail;
    public ParticleSystem particles;

    [Header("Emission")]
    [Range(0f, 1f)]
    public float emissionAmount = 0.5f;

    void LateUpdate()
    {
        int count = trail.positionCount;

        if (count < 2)
            return;

        var positions = new Vector3[count];
        trail.GetPositions(positions);

        var emitParams = new ParticleSystem.EmitParams();

        for (int i = 0; i < count; i++)
        {
            // Position de la particule
            emitParams.position = positions[i];

            // Pas de vitesse
            emitParams.velocity = Vector3.zero;

            // Probabilité d'émission
            if (Random.value < emissionAmount)
            {
                particles.Emit(emitParams, 1);
            }
        }
    }
}
