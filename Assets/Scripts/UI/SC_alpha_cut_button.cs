using UnityEngine;
using UnityEngine.UI;

public class SC_alpha_cut_button : MonoBehaviour
{
    Image img;
    void Start()
    {
        img = GetComponent<Image>();
        cut();
    }

    public void uncut()
    {
        img.alphaHitTestMinimumThreshold = 0f;
    }
    public void cut()
    {
        img.alphaHitTestMinimumThreshold = 1f;
    }
}