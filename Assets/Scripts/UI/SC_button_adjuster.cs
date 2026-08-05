using TMPro;
using UnityEngine;

public class SC_button_adjuster : MonoBehaviour
{
    [SerializeField] private TMP_Text textMesh;
    [SerializeField] private Vector2 padding = new Vector2(0.2f, 0.2f);

    private BoxCollider2D boxCollider;

    private void Awake()
    {
        boxCollider = GetComponent<BoxCollider2D>();

        if (textMesh == null)
            textMesh = GetComponent<TMP_Text>();

        UpdateCollider();
    }

    private void LateUpdate()
    {
        UpdateCollider();
    }

    public void UpdateCollider()
    {
        if (textMesh == null)
            return;

        // Force la mise à jour du texte
        textMesh.ForceMeshUpdate();

        Vector2 size = textMesh.bounds.size;

        boxCollider.size = size + padding;
        boxCollider.offset = textMesh.bounds.center;
    }
}