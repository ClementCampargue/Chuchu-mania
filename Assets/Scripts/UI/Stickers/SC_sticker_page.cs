
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_sticker_page : MonoBehaviour
{
    private SC_Button default_selected;

    [Header("Stickers")]
    public SO_sticker_list stickers;
    public Transform list_parent;
    public GameObject button;

    [Header("Grid Settings")]
    [Min(1)]
    public int columnCount = 4;

    public float spacingX = 1.5f;
    public float spacingY = 1.5f;

    public List<SC_sticker_button> buttons = new List<SC_sticker_button>();

    public SC_menu_navigation nav;

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

        buttons.Clear();
        default_selected = null;

        if (stickers == null || stickers.stickers == null)
        {
            Debug.LogWarning("Aucun sticker assigné !");
            return;
        }

        for (int i = 0; i < stickers.stickers.Count; i++)
        {
            SO_Sticker sticker = stickers.stickers[i];

            GameObject go = Instantiate(button, list_parent);

            // Calcul de la position dans la grille
            int column = i % columnCount;
            int row = i / columnCount;

            float x = column * spacingX;
            float y = -row * spacingY;

            go.transform.localPosition = new Vector3(x, y, 0);

            SC_sticker_button stickerButton =
                go.GetComponent<SC_sticker_button>();

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
                Debug.LogWarning(
                    "Le prefab bouton n'a pas de SC_sticker_button !"
                );
            }
        }

        // Centre automatiquement la grille
        float totalWidth = (columnCount - 1) * spacingX;

        foreach (Transform child in list_parent)
        {
            Vector3 pos = child.localPosition;
            pos.x -= totalWidth / 2f;
            child.localPosition = pos;
        }
        nav.RefreshButtons();
        nav.SelectFirstAvailable();
    }



    private IEnumerator SelectDefaultButton()
    {
        yield return null;
        SelectDefault();
    }

    private void SelectDefault()
    {
        if (default_selected == null)
            return;

        if (!default_selected.gameObject.activeInHierarchy)
            return;

        default_selected.Select();
    }

    private void OnEnable()
    {
        StartCoroutine(SelectDefaultNextFrame());
    }

    private IEnumerator SelectDefaultNextFrame()
    {
        yield return null;
        SelectDefault();
    }
}
