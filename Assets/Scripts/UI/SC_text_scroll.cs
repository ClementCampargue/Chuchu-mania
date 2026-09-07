using TMPro;
using UnityEngine;

public class SC_text_scroll : MonoBehaviour
{
    [Header("Références")]
    [SerializeField] private TextMeshPro text;

    [Header("Défilement")]
    [SerializeField] private float speed = 2f;
    [SerializeField] private float spacing = 0f;

    private TextMeshPro copy;
    private float textWidth;

    private void Start()
    {
        if (text == null)
            text = GetComponent<TextMeshPro>();

        // Force TMP à mettre à jour le mesh
        text.ForceMeshUpdate();

        // Mesure réelle du texte dans le monde
        textWidth = text.GetComponent<Renderer>().bounds.size.x;

        // Création de la deuxième copie
        copy = Instantiate(text, text.transform.parent);
        copy.name = text.name + "_LoopCopy";

        // Position de la copie juste après le texte original
        copy.transform.position =
            text.transform.position +
            text.transform.right * (textWidth + spacing);
    }

    private void Update()
    {
        Vector3 movement = -text.transform.right * speed * Time.deltaTime;

        text.transform.position += movement;
        copy.transform.position += movement;

        // Distance entre les deux textes
        float distance = Vector3.Distance(
            text.transform.position,
            copy.transform.position
        );

        // Quand un texte est suffisamment passé derrière,
        // on le replace derrière l'autre.
        if (distance >= textWidth + spacing)
        {
            if (Vector3.Dot(
                copy.transform.position - text.transform.position,
                text.transform.right) > 0)
            {
                text.transform.position =
                    copy.transform.position +
                    text.transform.right * (textWidth + spacing);
            }
            else
            {
                copy.transform.position =
                    text.transform.position +
                    text.transform.right * (textWidth + spacing);
            }
        }
    }
}
