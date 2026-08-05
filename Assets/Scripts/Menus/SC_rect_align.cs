using UnityEngine;
using UnityEngine.UI;

public class SC_rect_align : MonoBehaviour
{
    [SerializeField] private RectTransform textRect;
    [SerializeField] private RectTransform targetRect;


    [SerializeField] private Vector2 offset = new Vector2(20f, 0f);

    public void UpdatePosition()
    {
        targetRect.anchoredPosition = textRect.anchoredPosition + offset;
    }

    private void Start()
    {
        UpdatePosition();
    }
}