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

        // Nom du Material spécial
        public string specialMatName;

        public float posX;
        public float posY;

        public float rotZ;

        public float scaleX;
        public float scaleY;

        // Ordre du sticker dans la hiérarchie
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

    private void OnDisable()
    {
        AutoSave();
    }

    private void OnDestroy()
    {
        AutoSave();
    }

    private void Start()
    {
        // Cherche automatiquement la TrashZone
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
        if (parent == null)
        {
            Debug.LogError("Parent n'est pas assigné.");
            return;
        }

        StickerSave save = new StickerSave();

        /*
         * IMPORTANT :
         * On parcourt directement les enfants du parent.
         * Cela garantit que les stickers sont récupérés
         * dans l'ordre exact de la hiérarchie.
         */
        for (int i = 0; i < parent.childCount; i++)
        {
            Transform child = parent.GetChild(i);

            if (child == null)
                continue;

            SC_sticker_UI stickerUI =
                child.GetComponent<SC_sticker_UI>();

            /*
             * Si le SC_sticker_UI est sur un enfant du prefab,
             * on cherche aussi dans les enfants.
             */
            if (stickerUI == null)
            {
                stickerUI =
                    child.GetComponentInChildren<SC_sticker_UI>();
            }

            if (stickerUI == null)
                continue;

            RectTransform rt =
                stickerUI.GetComponent<RectTransform>();

            Image img =
                stickerUI.GetComponent<Image>();

            if (rt == null)
            {
                rt = stickerUI.GetComponentInChildren<RectTransform>();
            }

            if (img == null)
            {
                img = stickerUI.GetComponentInChildren<Image>();
            }

            if (rt == null || img == null)
                continue;

            // Position écran du centre du sticker
            Vector2 screenPos =
                RectTransformUtility.WorldToScreenPoint(
                    null,
                    rt.position
                );

            /*
             * Si le sticker est dans la TrashZone,
             * on le détruit et on ne le sauvegarde pas.
             */
            if (deleteZone != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    deleteZone,
                    screenPos
                ))
            {
                Debug.Log(
                    "Sticker supprimé par le SaveSystem."
                );

                Destroy(child.gameObject);
                continue;
            }

            StickerData data = new StickerData();

            // ==========================================
            // SPRITE
            // ==========================================

            data.spriteName =
                img.sprite != null
                    ? img.sprite.name
                    : "";

            // ==========================================
            // SPECIAL MATERIAL
            // ==========================================

            SO_Sticker stickerData =
                FindSticker(data.spriteName);

            data.specialMatName =
                stickerData != null &&
                stickerData.special_mat != null
                    ? stickerData.special_mat.name
                    : "";

            // ==========================================
            // POSITION
            // ==========================================

            data.posX = rt.anchoredPosition.x;
            data.posY = rt.anchoredPosition.y;

            // ==========================================
            // ROTATION
            // ==========================================

            data.rotZ = rt.localEulerAngles.z;

            // ==========================================
            // SCALE
            // ==========================================

            data.scaleX = rt.localScale.x;
            data.scaleY = rt.localScale.y;

            /*
             * On sauvegarde l'ordre actuel.
             *
             * Comme on parcourt parent.GetChild(i),
             * cet index correspond exactement à l'ordre
             * des stickers dans la hiérarchie.
             */
            data.siblingIndex = i;

            save.stickers.Add(data);
        }

        string json =
            JsonUtility.ToJson(save);

        PlayerPrefs.SetString(
            SAVE_KEY,
            json
        );

        PlayerPrefs.Save();

        Debug.Log(
            "Stickers sauvegardés : " +
            save.stickers.Count
        );
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log(
                "Aucune sauvegarde de stickers trouvée."
            );

            return;
        }

        if (stickerPrefab == null)
        {
            Debug.LogError(
                "StickerPrefab n'est pas assigné."
            );

            return;
        }

        if (parent == null)
        {
            Debug.LogError(
                "Parent n'est pas assigné."
            );

            return;
        }

        if (stickers == null)
        {
            Debug.LogError(
                "SO_sticker_list n'est pas assigné."
            );

            return;
        }

        string json =
            PlayerPrefs.GetString(SAVE_KEY);

        StickerSave save =
            JsonUtility.FromJson<StickerSave>(json);

        if (save == null ||
            save.stickers == null)
        {
            Debug.LogWarning(
                "La sauvegarde des stickers est invalide."
            );

            return;
        }

        /*
         * On garde une liste des objets créés.
         * Cela permet de restaurer l'ordre APRÈS
         * avoir créé tous les stickers.
         */
        List<GameObject> loadedObjects =
            new List<GameObject>();

        foreach (StickerData data in save.stickers)
        {
            // ==========================================
            // CRÉATION DU PREFAB
            // ==========================================

            GameObject prefabInstance =
                Instantiate(
                    stickerPrefab,
                    parent
                );

            /*
             * Ton prefab semble contenir le sticker
             * comme premier enfant.
             */
            GameObject stickerObject =
                prefabInstance;

            if (prefabInstance.transform.childCount > 0)
            {
                stickerObject =
                    prefabInstance.transform
                        .GetChild(0)
                        .gameObject;
            }

            RectTransform rt =
                stickerObject.GetComponent<RectTransform>();

            Image img =
                stickerObject.GetComponent<Image>();

            if (rt == null || img == null)
            {
                Debug.LogWarning(
                    "Le sticker chargé n'a pas de " +
                    "RectTransform ou d'Image."
                );

                Destroy(prefabInstance);
                continue;
            }

            // ==========================================
            // POSITION
            // ==========================================

            rt.anchoredPosition =
                new Vector2(
                    data.posX,
                    data.posY
                );

            // ==========================================
            // ROTATION
            // ==========================================

            rt.localEulerAngles =
                new Vector3(
                    0f,
                    0f,
                    data.rotZ
                );

            // ==========================================
            // SCALE
            // ==========================================

            SC_sticker_UI stickerUI =
           stickerObject.GetComponent<SC_sticker_UI>();

            if (stickerUI != null)
            {
                stickerUI.spawnedSticker = true;

                stickerUI.SetSavedScale(
                    data.scaleX,
                    data.scaleY
                );
            }
            else
            {
                rt.localScale =
                    new Vector3(
                        data.scaleX,
                        data.scaleY,
                        1f
                    );
            }
            // ==========================================
            // RECHERCHE DU SPRITE
            // ==========================================

            Sprite loadedSprite =
                FindSprite(data.spriteName);

            if (loadedSprite != null)
            {
                img.sprite = loadedSprite;
                img.maskable = true;
            }
            else
            {
                Debug.LogWarning(
                    "Sprite introuvable : " +
                    data.spriteName
                );
            }

            // ==========================================
            // RECHERCHE DU SPECIAL MATERIAL
            // ==========================================

            Material specialMat =
                FindSpecialMaterial(data.specialMatName);

            if (specialMat != null)
            {
                img.material = specialMat;
            }

            // ==========================================
            // STICKER UI
            // ==========================================

            /*

            if (stickerUI != null)
            {
                stickerUI.spawnedSticker = true;
            }

            /*
             * On stocke le PREFAB parent.
             * C'est lui qui doit être déplacé dans
             * la hiérarchie.
             */
            loadedObjects.Add(prefabInstance);
        }

        /*
         * ==========================================
         * RESTAURATION DE L'ORDRE
         * ==========================================
         *
         * Les stickers sont dans save.stickers
         * dans le même ordre que lors du Save().
         *
         * On remet donc les prefabs dans cet ordre.
         */
        for (int i = 0; i < loadedObjects.Count; i++)
        {
            if (loadedObjects[i] == null)
                continue;

            loadedObjects[i]
                .transform
                .SetSiblingIndex(i);
        }

        Debug.Log(
            "Stickers chargés : " +
            loadedObjects.Count
        );
    }

    // ==========================================
    // FIND SPRITE
    // ==========================================

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

    // ==========================================
    // FIND STICKER
    // ==========================================

    private SO_Sticker FindSticker(string spriteName)
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
                return sticker;
            }
        }

        return null;
    }

    // ==========================================
    // FIND SPECIAL MATERIAL
    // ==========================================

    private Material FindSpecialMaterial(string materialName)
    {
        if (string.IsNullOrEmpty(materialName))
            return null;

        if (stickers == null ||
            stickers.stickers == null)
            return null;

        foreach (SO_Sticker sticker in stickers.stickers)
        {
            if (sticker == null)
                continue;

            if (sticker.special_mat == null)
                continue;

            if (sticker.special_mat.name == materialName)
            {
                return sticker.special_mat;
            }
        }

        return null;
    }

    // ==========================================
    // CLEAR
    // ==========================================

    public void Clear()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.Save();

        Debug.Log(
            "Sauvegarde des stickers supprimée."
        );
    }

    // ==========================================
    // HAS SAVE
    // ==========================================

    public bool HasSave()
    {
        return PlayerPrefs.HasKey(SAVE_KEY);
    }
}
