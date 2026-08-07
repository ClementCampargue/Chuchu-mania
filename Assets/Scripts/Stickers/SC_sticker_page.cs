using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_sticker_page : MonoBehaviour
{
    private Button default_selected;
    public List<SO_Sticker> stickers;
    public Transform list_parent;
    public GameObject button;

    private void Start()
    {
        CreateButtons();
    }


    private void CreateButtons()
    {
        // Supprime les anciens boutons si nécessaire
        foreach (Transform child in list_parent)
        {
            Destroy(child.gameObject);
        }

        default_selected = null;

        // Crée un bouton pour chaque sticker
        foreach (SO_Sticker sticker in stickers)
        {
            GameObject go = Instantiate(button, list_parent, false);

            SC_sticker_button stickerButton = go.GetComponent<SC_sticker_button>();
            if (stickerButton != null)
            {
                stickerButton.sticker = sticker;
            }

            if (default_selected == null)
            {
                default_selected = stickerButton.button;
            }
        }
    }

    private void OnEnable()
    {
        if (default_selected != null)
        {
            default_selected.Select();
        }
    }
}