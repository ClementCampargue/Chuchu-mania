using UnityEngine;
using UnityEngine.UI;

public class SC_alpha_cut_button : MonoBehaviour
{
    private Image img;

    private void Awake()
    {
        img = GetComponent<Image>();
    }

    private void Start()
    {
        // Par défaut, le sticker ne reçoit les interactions
        // que sur ses pixels entièrement opaques.
        cut();
    }

    public void uncut()
    {
        if (img == null)
            return;

        img.alphaHitTestMinimumThreshold = 0f;
    }

    public void cut()
    {
        if (img == null)
            return;

        img.alphaHitTestMinimumThreshold = 1f;
    }
}