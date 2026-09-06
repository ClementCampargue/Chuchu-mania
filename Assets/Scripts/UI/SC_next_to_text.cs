using TMPro;
using UnityEngine;

public class SC_next_to_text : MonoBehaviour
{
    public TMP_Text textMesh;
    public Transform sprite;

    [Tooltip("Distance entre le sprite et le texte")]
    public float distance = 0.1f;

    public bool right;

    void LateUpdate()
    {
        if (textMesh == null || sprite == null)
            return;

        textMesh.ForceMeshUpdate();

        TMP_TextInfo textInfo = textMesh.textInfo;

        if (textInfo.characterCount == 0)
            return;

        TMP_CharacterInfo character;

        if (right)
        {
            // Dernier caractère du texte
            character = textInfo.characterInfo[textInfo.characterCount - 1];

            // Fin du texte
            Vector3 finTexte = new Vector3(
                character.topRight.x,
                character.baseLine,
                0f
            );

            Vector3 positionMonde =
                textMesh.transform.TransformPoint(finTexte);

            // Sprite à droite de la fin du texte
            Vector3 nouvellePosition =
                positionMonde + textMesh.transform.right * distance;

            // Garde le Y du sprite
            nouvellePosition.y = sprite.position.y;

            sprite.position = nouvellePosition;
        }
        else
        {
            // Premier caractère du texte
            character = textInfo.characterInfo[0];

            // Début du texte
            Vector3 debutTexte = new Vector3(
                character.bottomLeft.x,
                character.baseLine,
                0f
            );

            Vector3 positionMonde =
                textMesh.transform.TransformPoint(debutTexte);

            // Sprite à gauche du début du texte
            Vector3 nouvellePosition =
                positionMonde - textMesh.transform.right * distance;

            // Garde le Y du sprite
            nouvellePosition.y = sprite.position.y;

            sprite.position = nouvellePosition;
        }
    }
}