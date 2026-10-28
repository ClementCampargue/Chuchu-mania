
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

        int index = right ? textInfo.characterCount - 1 : 0;

        // Ignore les espaces et caractères invisibles aux extrémités
        if (right)
        {
            while (index >= 0 &&
                   (!textInfo.characterInfo[index].isVisible))
            {
                index--;
            }
        }
        else
        {
            while (index < textInfo.characterCount &&
                   (!textInfo.characterInfo[index].isVisible))
            {
                index++;
            }
        }

        if (index < 0 || index >= textInfo.characterCount)
            return;

        TMP_CharacterInfo character = textInfo.characterInfo[index];

        Vector3 pointLocal;

        if (right)
        {
            pointLocal = character.topRight;
        }
        else
        {
            pointLocal = character.bottomLeft;
        }

        Vector3 positionMonde =
            textMesh.transform.TransformPoint(pointLocal);

        Vector3 direction = textMesh.transform.right;

        Vector3 nouvellePosition = positionMonde
            + direction * (right ? distance : -distance);

        // Conserve la hauteur actuelle du sprite
        nouvellePosition.y = sprite.position.y;

        sprite.position = nouvellePosition;
    }
}
