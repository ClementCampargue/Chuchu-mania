using UnityEngine;

public class SC_sticker_button : MonoBehaviour
{
    [Header("Scale settings")]
    public float hoverScale = 1.2f;
    public float scaleSpeed = 10f;

    public SpriteRenderer sprite;
    public Transform pivot;
    private Vector3 originalScale;
    private Vector3 targetScale;
    public SO_Sticker sticker;
    public Material locked;
    private Material unlocked;

    public GameObject hover;
    public GameObject Sticker_prefab;
    private void Start()
    {
        originalScale = pivot.transform.localScale;
        targetScale = originalScale;
        sprite.sprite = sticker.sticker_sprite;
  
    }
    private void OnEnable()
    {
        originalScale = pivot.transform.localScale;
        targetScale = originalScale;
        sprite.sprite = sticker.sticker_sprite;
        unlocked = sprite.material;
        if (!sticker.unlocked)
        {
            sprite.material = locked;   
        }
    }

    private void Update()
    {
        pivot.transform.localScale = Vector3.Lerp(pivot.transform.localScale, targetScale, Time.deltaTime * scaleSpeed);
        if (!sticker.unlocked)
        {
            sprite.material = locked;
        }
        else
        {
            sprite.material = unlocked;
        }
    }

    private void OnMouseEnter()
    {
        if (sticker.unlocked)
        {
            targetScale = originalScale * hoverScale;
            hover.SetActive(true);
        }
    }

    private void OnMouseExit()
    {
        targetScale = originalScale;
        hover.SetActive(false);
    }

    private void OnMouseDown()
    {
        if (sticker.unlocked)
        {
            SpriteRenderer spr = Instantiate(Sticker_prefab, SC_scursorManager.instance.transform.position, Quaternion.identity).GetComponent<SpriteRenderer>();
            spr.sprite = sticker.sticker_sprite;
            spr.gameObject.GetComponent<SC_sticker>().RefreshCollider();
            hover.SetActive(false);
        }
    }
}