using UnityEngine;
using UnityEngine.UI;

public class SC_scursorManager : MonoBehaviour
{
    public static SC_scursorManager instance;

    [Header("Sprites curseur")]
    public Sprite normalSprite;
    public Sprite hoverSprite;
    public Sprite grabSprite;

    [Header("UI")]
    public Image cursorRenderer;     
    public RectTransform cursorRect; 

    public bool grabbing;
    public GameObject fader;

    void Awake()
    {
        instance = this;

        Cursor.visible = false;

        SetNormalCursor();
    }

    void Update()
    {
        // Cursor UI follow screen mouse position
        cursorRect.position = Input.mousePosition;
    }

    public void SetNormalCursor()
    {
        if (fader != null)
            fader.SetActive(false);

        grabbing = false;
        cursorRenderer.sprite = normalSprite;
    }

    public void SetHoverCursor()
    {
        if (!grabbing)
        {
            cursorRenderer.sprite = hoverSprite;
        }
    }

    public void SetGrabCursor()
    {
        if (fader != null)
            fader.SetActive(true);

        grabbing = true;
        cursorRenderer.sprite = grabSprite;
    }
}