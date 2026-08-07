using UnityEngine;
using UnityEngine.UI;

public class SC_alpha_cut_button : MonoBehaviour
{
    void Start()
    {
        Image img = GetComponent<Image>();

        // 0 = tout est cliquable
        // 1 = seuls les pixels totalement opaques sont cliquables
        img.alphaHitTestMinimumThreshold = 1f;
    }
}