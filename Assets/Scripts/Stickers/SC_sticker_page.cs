using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class SC_sticker_page : MonoBehaviour
{
    private Button default_selected;

    public SO_sticker_list stickers;
    public Transform list_parent;
    public GameObject button;
    public List<SC_sticker_button> buttons;

    private void Start()
    {
        buttons.Clear();
        CreateButtons();

        StartCoroutine(SelectDefaultButton());
    }

    private void CreateButtons()
    {
        foreach (Transform child in list_parent)
        {
            Destroy(child.gameObject);
        }

        default_selected = null;

        if (stickers == null || stickers.stickers == null)
        {
            Debug.LogWarning("Aucun sticker assigné !");
            return;
        }

        foreach (SO_Sticker sticker in stickers.stickers)
        {
            GameObject go = Instantiate(button, list_parent, false);

            SC_sticker_button stickerButton = go.GetComponent<SC_sticker_button>();

            if (stickerButton != null)
            {
                stickerButton.sticker = sticker;

                if (default_selected == null)
                {
                    default_selected = stickerButton.button;
                }

                buttons.Add(stickerButton);
            }
            else
            {
                Debug.LogWarning("Le prefab bouton n'a pas de SC_sticker_button !");
            }
        }
    }

    private IEnumerator SelectDefaultButton()
    {
        // Attend que Unity ait terminé de construire le Canvas
        yield return null;

        if (default_selected != null && default_selected.gameObject.activeInHierarchy)
        {
            EventSystem.current.SetSelectedGameObject(default_selected.gameObject);
            default_selected.Select();
        }
    }

    private void OnEnable()
    {
        if (default_selected != null)
        {
            EventSystem.current.SetSelectedGameObject(default_selected.gameObject);
            default_selected.Select();
        }
    }
}