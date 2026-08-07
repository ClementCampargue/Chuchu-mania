using UnityEngine;

public class SC_RectTransformToCorner : MonoBehaviour
{
    public RectTransform target;
    public RectTransform parentImage;

    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    public Corner corner;

    public Vector2 offset = Vector2.zero;

    void Start()
    {if(target == null)
        {
            target = GetComponent<RectTransform>();
        }
        MoveToCorner();
    }

    public void MoveToCorner()
    {
        target.SetParent(parentImage);

        target.anchorMin = GetAnchor();
        target.anchorMax = GetAnchor();

        target.anchoredPosition = offset;
    }

    Vector2 GetAnchor()
    {
        switch (corner)
        {
            case Corner.TopLeft:
                return new Vector2(0, 1);

            case Corner.TopRight:
                return new Vector2(1, 1);

            case Corner.BottomLeft:
                return new Vector2(0, 0);

            case Corner.BottomRight:
                return new Vector2(1, 0);
        }

        return Vector2.zero;
    }
}