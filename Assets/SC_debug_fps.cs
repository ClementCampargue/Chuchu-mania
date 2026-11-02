
using UnityEngine;
using TMPro;

public class SC_debug_fps : MonoBehaviour
{
    public TextMeshPro fpsText;

    public float updateInterval = 0.5f;

    private float deltaTime = 0f;

    void Update()
    {
        deltaTime += (Time.unscaledDeltaTime - deltaTime) * 0.1f;

        if (Time.unscaledTime % updateInterval < Time.unscaledDeltaTime)
        {
            float fps = 1f / deltaTime;

            fpsText.text = Mathf.RoundToInt(fps) + " FPS";

            // Couleur selon les FPS
            if (fps >= 60)
            {
                fpsText.color = Color.green;
            }
            else if (fps >= 30)
            {
                fpsText.color = Color.yellow;
            }
            else
            {
                fpsText.color = Color.red;
            }
        }
    }
}
