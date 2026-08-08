using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_StickerSaveSystem : MonoBehaviour
{
    public static SC_StickerSaveSystem instance;

    [Header("Prefabs")]
    public GameObject stickerPrefab;
    public Transform parent;

    [Header("Sticker Database")]
    public SO_sticker_list stickers;

    [Header("Delete Zone")]
    public RectTransform deleteZone;

    private const string SAVE_KEY = "ui_stickers_save";

    [System.Serializable]
    public class StickerData
    {
        public string spriteName;

        public float posX;
        public float posY;

        public float rotZ;

        public float scaleX;
        public float scaleY;

        public int siblingIndex;
    }

    [System.Serializable]
    public class StickerSave
    {
        public List<StickerData> stickers = new List<StickerData>();
    }

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void Start()
    {
        // Cherche automatiquement la TrashZone si elle n'est pas assignée
        if (deleteZone == null)
        {
            GameObject trashObject = GameObject.Find("TrashZone");

            if (trashObject != null)
            {
                deleteZone = trashObject.GetComponent<RectTransform>();
            }
        }

        Load();
    }

    public void AutoSave()
    {
        Save();
    }

    public void Save()
    {
        SC_sticker_UI[] stickerObjects =
            FindObjectsOfType<SC_sticker_UI>();

        StickerSave save = new StickerSave();

        foreach (SC_sticker_UI stickerUI in stickerObjects)
        {
            if (stickerUI == null)
                continue;

            RectTransform rt = stickerUI.GetComponent<RectTransform>();
            Image img = stickerUI.GetComponent<Image>();

            if (rt == null || img == null)
                continue;

            // Position écran du centre du sticker
            Vector2 screenPos =
                RectTransformUtility.WorldToScreenPoint(
                    null,
                    rt.position
                );

            // Si le sticker est dans la TrashZone,
            // on le détruit et on ne le sauvegarde pas.
            if (deleteZone != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    deleteZone,
                    screenPos
                ))
            {
                Debug.Log("Sticker supprimé par le SaveSystem.");

                Destroy(stickerUI.gameObject);
                continue;
            }

            StickerData data = new StickerData();

            // Nom du sprite
            data.spriteName =
                img.sprite != null
                    ? img.sprite.name
                    : "";

            // Position UI
            data.posX = rt.anchoredPosition.x;
            data.posY = rt.anchoredPosition.y;

            // Rotation
            data.rotZ = rt.localEulerAngles.z;

            // Scale
            data.scaleX = rt.localScale.x;
            data.scaleY = rt.localScale.y;

            // Ordre dans le Canvas
            data.siblingIndex = rt.GetSiblingIndex();

            save.stickers.Add(data);
        }

        string json = JsonUtility.ToJson(save);

        PlayerPrefs.SetString(SAVE_KEY, json);
        PlayerPrefs.Save();

        Debug.Log("Stickers sauvegardés : " + save.stickers.Count);
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log("Aucune sauvegarde de stickers trouvée.");
            return;
        }

        if (stickerPrefab == null)
        {
            Debug.LogError("StickerPrefab n'est pas assigné.");
            return;
        }

        if (parent == null)
        {
            Debug.LogError("Parent n'est pas assigné.");
            return;
        }

        if (stickers == null)
        {
            Debug.LogError("SO_sticker_list n'est pas assigné.");
            return;
        }

        string json = PlayerPrefs.GetString(SAVE_KEY);

        StickerSave save =
            JsonUtility.FromJson<StickerSave>(json);

        if (save == null || save.stickers == null)
        {
            Debug.LogWarning("La sauvegarde des stickers est invalide.");
            return;
        }

        foreach (StickerData data in save.stickers)
        {
            // Création du prefab
            GameObject prefabInstance =
                Instantiate(stickerPrefab, parent);

            // Ton prefab semble contenir le sticker
            // comme premier enfant.
            GameObject stickerObject = prefabInstance;

            if (prefabInstance.transform.childCount > 0)
            {
                stickerObject =
                    prefabInstance.transform.GetChild(0).gameObject;
            }

            RectTransform rt =
                stickerObject.GetComponent<RectTransform>();

            Image img =
                stickerObject.GetComponent<Image>();

            if (rt == null || img == null)
            {
                Debug.LogWarning(
                    "Le sticker chargé n'a pas de RectTransform ou d'Image."
                );

                Destroy(prefabInstance);
                continue;
            }

            // Position
            rt.anchoredPosition =
                new Vector2(
                    data.posX,
                    data.posY
                );

            // Rotation
            rt.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    data.rotZ
                );

            // Scale
            rt.localScale =
                new Vector3(
                    data.scaleX,
                    data.scaleY,
                    1f
                );

            // Recherche du Sprite dans la liste SO
            Sprite loadedSprite = FindSprite(data.spriteName);

            if (loadedSprite != null)
            {
                img.sprite = loadedSprite;
                img.maskable = true;
            }
            else
            {
                Debug.LogWarning(
                    "Sprite introuvable : " + data.spriteName
                );
            }

            // Restaurer l'ordre
            stickerObject.transform.SetSiblingIndex(
                data.siblingIndex
            );

            // Indiquer que le sticker vient d'une sauvegarde
            SC_sticker_UI stickerUI =
                stickerObject.GetComponent<SC_sticker_UI>();

            if (stickerUI != null)
            {
                stickerUI.spawnedSticker = true;
            }
        }

        Debug.Log(
            "Stickers chargés : " +
            save.stickers.Count
        );
    }

    private Sprite FindSprite(string spriteName)
    {
        if (string.IsNullOrEmpty(spriteName))
            return null;

        if (stickers == null ||
            stickers.stickers == null)
            return null;

        foreach (SO_Sticker sticker in stickers.stickers)
        {
            if (sticker == null)
                continue;

            if (sticker.sticker_sprite == null)
                continue;

            if (sticker.sticker_sprite.name == spriteName)
            {
                return sticker.sticker_sprite;
            }
        }

        return null;
    }

    public void Clear()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();

        Debug.Log("Sauvegarde des stickers supprimée.");
    }

    public bool HasSave()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }
}