using System.Collections.Generic;
using UnityEngine;

public class SC_menu_layout : MonoBehaviour
{
    public enum LayoutType
    {
        Horizontal,
        Vertical,
        Grid
    }

    [Header("Layout")]
    [SerializeField] private LayoutType layoutType = LayoutType.Horizontal;
    [SerializeField] private bool revert = false;

    [Header("Spacing")]
    [SerializeField] private float spacing = 2f;
    [SerializeField] private float rowSpacing = 2f;

    [Header("Grid")]
    [SerializeField] private int columns = 3;

    [Header("Options")]
    [SerializeField] private bool includeInactive = false;
    [SerializeField] private bool centerLayout = true;

    // Position de référence du groupe.
    private Vector3 layoutOrigin;

    private void Awake()
    {
        layoutOrigin = transform.localPosition;
    }

    private void OnEnable()
    {
        Rebuild();
    }

    [ContextMenu("Rebuild Layout")]
    public void Rebuild()
    {
        List<Transform> children = GetChildren();

        if (children.Count == 0)
            return;

        switch (layoutType)
        {
            case LayoutType.Horizontal:
                BuildHorizontal(children);
                break;

            case LayoutType.Vertical:
                BuildVertical(children);
                break;

            case LayoutType.Grid:
                BuildGrid(children);
                break;
        }
    }

    private List<Transform> GetChildren()
    {
        List<Transform> children = new();

        foreach (Transform child in transform)
        {
            if (!includeInactive &&
                !child.gameObject.activeInHierarchy)
            {
                continue;
            }

            children.Add(child);
        }

        return children;
    }

    private int GetLayoutIndex(int index, int count)
    {
        return revert
            ? count - 1 - index
            : index;
    }


    // =========================================================
    // HORIZONTAL
    // =========================================================

    private void BuildHorizontal(List<Transform> children)
    {
        float offset = 0f;

        if (centerLayout)
        {
            offset =
                (children.Count - 1) *
                spacing *
                0.5f;
        }

        for (int i = 0; i < children.Count; i++)
        {
            int layoutIndex =
                GetLayoutIndex(i, children.Count);

            Transform child = children[i];

            Vector3 position =
                child.localPosition;

            // On modifie UNIQUEMENT X.
            position.x =
                layoutOrigin.x +
                layoutIndex * spacing -
                offset;

            child.localPosition = position;
        }
    }


    // =========================================================
    // VERTICAL
    // =========================================================

    private void BuildVertical(List<Transform> children)
    {
        float offset = 0f;

        if (centerLayout)
        {
            offset =
                (children.Count - 1) *
                spacing *
                0.5f;
        }

        for (int i = 0; i < children.Count; i++)
        {
            int layoutIndex =
                GetLayoutIndex(i, children.Count);

            Transform child = children[i];

            Vector3 position =
                child.localPosition;

            // IMPORTANT :
            // On conserve le X original du bouton.
            // Seul Y est modifié.
            position.y =
                layoutOrigin.y +
                layoutIndex * spacing -
                offset;

            child.localPosition = position;
        }
    }


    // =========================================================
    // GRID
    // =========================================================

    private void BuildGrid(List<Transform> children)
    {
        columns = Mathf.Max(1, columns);

        int rows =
            Mathf.CeilToInt(
                (float)children.Count /
                columns
            );

        for (int i = 0; i < children.Count; i++)
        {
            int layoutIndex =
                GetLayoutIndex(i, children.Count);

            int column =
                layoutIndex % columns;

            int row =
                layoutIndex / columns;

            float x =
                column * spacing;

            float y =
                row * rowSpacing;

            if (centerLayout)
            {
                // =============================================
                // CENTRAGE HORIZONTAL DE LA LIGNE
                // =============================================

                int itemsInRow =
                    Mathf.Min(
                        columns,
                        children.Count -
                        row * columns
                    );

                float rowWidth =
                    (itemsInRow - 1) *
                    spacing;

                x -= rowWidth * 0.5f;


                // =============================================
                // CENTRAGE VERTICAL
                // =============================================

                float totalHeight =
                    (rows - 1) *
                    rowSpacing;

                y -= totalHeight * 0.5f;
            }

            Transform child =
                children[i];

            Vector3 position =
                child.localPosition;

            position.x =
                layoutOrigin.x + x;

            position.y =
                layoutOrigin.y + y;

            child.localPosition = position;
        }
    }


    // =========================================================
    // CHILDREN CHANGED
    // =========================================================

    private void OnTransformChildrenChanged()
    {
        Rebuild();
    }
}