using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class SC_sprite_grid : MonoBehaviour
{
    public int columns = 5;
    public Vector2 spacing;

    public bool autoUpdate = true;

    public Vector2 offset;

    void Update()
    {
        if (!autoUpdate) return;

#if UNITY_EDITOR
        Arrange();
#endif
    }

    public void Arrange()
    {
        int index = 0;

        foreach (Transform child in transform)
        {
            int x = index % columns;
            int y = index / columns;

            Vector3 pos = new Vector3(
                x * spacing.x + offset.x,
                -y * spacing.y + offset.y,
                0f
            );

#if UNITY_EDITOR
            Undo.RecordObject(child, "Grid Arrange");
#endif

            child.localPosition = pos;

            index++;
        }
    }

    [ContextMenu("Arrange Grid")]
    public void ArrangeManual()
    {
        Arrange();
    }
}