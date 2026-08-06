using TMPro;
using UnityEngine;

public class SC_tetx_mimic : MonoBehaviour
{
    public TMP_Text sourceText; // Le TMP qui contient le texte original
    public TMP_Text targetText; // Le TMP qui doit copier

    [Header("Options")]
    public int ignoreCharactersAtEnd = 0; // Nombre de caractères ignorés à la fin

    void Update()
    {
        if (sourceText != null && targetText != null)
        {
            string text = sourceText.text;

            if (ignoreCharactersAtEnd > 0 && text.Length > ignoreCharactersAtEnd)
            {
                text = text.Substring(0, text.Length - ignoreCharactersAtEnd);
            }

            targetText.text = text;
        }
    }
}