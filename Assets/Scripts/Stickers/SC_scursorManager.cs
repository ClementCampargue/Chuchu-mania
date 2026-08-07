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
        gameObject.SetActive(false);
        SetNormalCursor();
    }

    void Update()
    {
        // Cursor UI follow screen mouse position
        cursorRect.position = Input.mousePosition;
    }

    public void SetNormalCursor()
    {
        cursorRenderer.sprite = normalSprite;
    }

    public void SetHoverCursor()
    {
        cursorRenderer.sprite = hoverSprite;

    }

    public void SetGrabCursor()
    {

        cursorRenderer.sprite = grabSprite;
    }
}