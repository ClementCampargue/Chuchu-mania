using UnityEngine;

public class SC_girouette : MonoBehaviour
{
    public float rotationSpeed = 300f;
    public float rotationDuration = 10f;
    public float slowDownTime = 2f;

    [Header("Acceleration on hit")]
    public float hitBoost = 600f;        // boost immédiat
    public float boostDecay = 5f;        // vitesse de retour vers normale

    public bool isRotating = false;

    private float timer = 0f;
    private float currentSpeed = 0f;
    private float boostSpeed = 0f;
    private bool slowingDown = false;

    void Update()
    {
        if (isRotating)
        {
            timer += Time.deltaTime;

            // décrémente le boost progressivement
            boostSpeed = Mathf.Lerp(boostSpeed, 0f, Time.deltaTime * boostDecay);

            if (!slowingDown)
            {
                currentSpeed = rotationSpeed + boostSpeed;

                if (timer >= rotationDuration)
                {
                    slowingDown = true;
                }
            }
            else
            {
                currentSpeed = Mathf.Lerp(currentSpeed, 0f, Time.deltaTime / slowDownTime);

                if (currentSpeed < 2f)
                {
                    currentSpeed = 0f;
                    isRotating = false;
                    slowingDown = false;
                }
            }

            transform.Rotate(0f, 0f, currentSpeed * Time.deltaTime);
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            StartRotation();
            AddBoost();
        }
    }

    void AddBoost()
    {
        // gros coup d’accélération instantané
        boostSpeed += hitBoost;
    }

    void StartRotation()
    {
        isRotating = true;
        timer = 0f;
        slowingDown = false;
        currentSpeed = rotationSpeed;
    }
}