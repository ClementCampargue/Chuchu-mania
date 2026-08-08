using TMPro;
using UnityEngine;

public class SC_next_to_text : MonoBehaviour
{
    public TMP_Text textMesh;
    public Transform sprite;

    [Tooltip("Distance entre le sprite et le début du texte")]
    public float distance = 0.1f;

    void LateUpdate()
    {
        if (textMesh == null || sprite == null)
            return;

        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;

        if (textInfo.characterCount == 0)
            return;

        // Premier caractère visible
        TMP_CharacterInfo character = textInfo.characterInfo[0];

        // Position locale du début du premier caractère
        Vector3 debutTexte = new Vector3(
            character.bottomLeft.x,
            character.baseLine,
            0f
        );

        // Conversion de la position locale du texte vers le monde
        Vector3 positionMonde = textMesh.transform.TransformPoint(debutTexte);

        // Décalage vers la gauche
        Vector3 gauche = -textMesh.transform.right * distance;

        sprite.position = positionMonde + gauche;
    }
}
