
using UnityEngine;

public class ShaderTimeController : MonoBehaviour
{
    [SerializeField] private string shaderTimeProperty = "_ShaderTime";

    private float shaderTime;

    void Update()
    {
        if (Time.timeScale == 0f)
        {
            shaderTime += Time.unscaledDeltaTime;
        }

        Shader.SetGlobalFloat(shaderTimeProperty, shaderTime);
    }
}
