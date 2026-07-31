using UnityEngine;

public class SC_gravity_effector_revers : MonoBehaviour
{
    private PlatformEffector2D effector;

    private void Awake()
    {
        effector = GetComponent<PlatformEffector2D>();
    }

    private void OnEnable()
    {
        SC_gravity_flip.OnGravityChanged += UpdateEffector;
    }

    private void OnDisable()
    {
        SC_gravity_flip.OnGravityChanged -= UpdateEffector;
    }

    private void Start()
    {
        UpdateEffector(SC_gravity_flip.instance.gravity_up);
    }

    private void UpdateEffector(bool gravityUp)
    {
        effector.rotationalOffset = gravityUp ? 180f : 0f;
    }
}
