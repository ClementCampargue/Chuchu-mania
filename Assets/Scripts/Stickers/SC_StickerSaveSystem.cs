using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_StickerSaveSystem : MonoBehaviour
{
    public static SC_StickerSaveSystem instance;

    // =========================================================
    // PREFABS
    // =========================================================

    [Header("Prefabs")]
    public GameObject stickerPrefab;
    public Transform parent;

    // =========================================================
    // STICKER DATABASE
    // =========================================================

    [Header("Sticker Database")]
    public SO_sticker_list stickers;

    // =========================================================
    // DELETE ZONE
    // =========================================================

    [Header("Delete Zone")]
    public RectTransform deleteZone;

    // =========================================================
    // COUNT
    // =========================================================

    [Header("Sticker Count")]
    public int stickerCount;

    // =========================================================
    // SAVE KEY
    // =========================================================

    private const string SAVE_KEY =
        "ui_stickers_save";

    // =========================================================
    // SAVE DATA
    // =========================================================

    [System.Serializable]
    public class StickerData
    {
        public string spriteName;

        public string specialMatName;

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
        public List<StickerData> stickers =
            new List<StickerData>();
    }

    // =========================================================
    // LIFECYCLE
    // =========================================================

    private void Awake()
    {
        if (instance != null &&
            instance != this)
        {
            Destroy(gameObject);
            return;
        }

        instance = this;
    }

    private void OnDisable()
    {
        // Évite les sauvegardes inutiles si l'objet
        // n'est pas l'instance principale.
        if (instance == this)
        {
            AutoSave();
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            AutoSave();
        }
    }

    private void Start()
    {
        // -----------------------------------------------------
        // TRASH ZONE
        // -----------------------------------------------------

        if (deleteZone == null)
        {
            GameObject trashObject =
                GameObject.Find("TrashZone");

            if (trashObject != null)
            {
                deleteZone =
                    trashObject.GetComponent<RectTransform>();
            }
        }

        // -----------------------------------------------------
        // LOAD
        // -----------------------------------------------------

        Load();

        UpdateStickerCount();
    }

    // =========================================================
    // UPDATE COUNT
    // =========================================================

    public void UpdateStickerCount()
    {
        if (parent == null)
        {
            stickerCount = 0;
            return;
        }

        int count = 0;

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform child =
                parent.GetChild(i);

            if (child == null)
                continue;

            SC_sticker_UI stickerUI =
                child.GetComponent<SC_sticker_UI>();

            if (stickerUI == null)
            {
                stickerUI =
                    child.GetComponentInChildren<SC_sticker_UI>();
            }

            if (stickerUI != null)
            {
                count++;
            }
        }

        stickerCount =
            count;
    }

    // =========================================================
    // AUTO SAVE
    // =========================================================

    public void AutoSave()
    {
        Save();
    }

    // =========================================================
    // SAVE
    // =========================================================

    public void Save()
    {
        if (parent == null)
        {
            Debug.LogError(
                "SC_StickerSaveSystem : Parent n'est pas assigné."
            );

            stickerCount = 0;
            return;
        }

        StickerSave save =
            new StickerSave();

        // -----------------------------------------------------
        // PARCOURS DES STICKERS
        // -----------------------------------------------------

        for (int i = 0;
             i < parent.childCount;
             i++)
        {
            Transform child =
                parent.GetChild(i);

            if (child == null)
                continue;

            SC_sticker_UI stickerUI =
                child.GetComponent<SC_sticker_UI>();

            if (stickerUI == null)
            {
                stickerUI =
                    child.GetComponentInChildren<SC_sticker_UI>();
            }

            if (stickerUI == null)
                continue;

            // -------------------------------------------------
            // RECT
            // -------------------------------------------------

            RectTransform rt =
                stickerUI.GetComponent<RectTransform>();

            if (rt == null)
            {
                rt =
                    stickerUI.GetComponentInChildren<RectTransform>();
            }

            // -------------------------------------------------
            // IMAGE
            // -------------------------------------------------

            Image img =
                stickerUI.GetComponent<Image>();

            if (img == null)
            {
                img =
                    stickerUI.GetComponentInChildren<Image>();
            }

            if (rt == null ||
                img == null)
            {
                continue;
            }

            // -------------------------------------------------
            // DELETE ZONE
            // -------------------------------------------------

            Vector2 screenPos =
                RectTransformUtility.WorldToScreenPoint(
                    null,
                    rt.position
                );

            if (deleteZone != null &&
                RectTransformUtility.RectangleContainsScreenPoint(
                    deleteZone,
                    screenPos
                ))
            {
                Debug.Log(
                    "Sticker dans la TrashZone : suppression."
                );

                Destroy(child.gameObject);

                continue;
            }

            StickerData data =
                new StickerData();

            // -------------------------------------------------
            // SPRITE
            // -------------------------------------------------

            data.spriteName =
                img.sprite != null
                    ? img.sprite.name
                    : "";

            // -------------------------------------------------
            // SPECIAL MATERIAL
            // -------------------------------------------------

            SO_Sticker stickerData =
                FindSticker(
                    data.spriteName
                );

            data.specialMatName =
                stickerData != null &&
                stickerData.special_mat != null
                    ? stickerData.special_mat.name
                    : "";

            // -------------------------------------------------
            // POSITION
            // -------------------------------------------------

            data.posX =
                rt.anchoredPosition.x;

            data.posY =
                rt.anchoredPosition.y;

            // -------------------------------------------------
            // ROTATION
            // -------------------------------------------------
            //
            // IMPORTANT :
            // On utilise stickerRotationZ et non
            // rt.localEulerAngles.z.
            //
            // Cela évite de sauvegarder le tilt Balatro.
            // -------------------------------------------------

            data.rotZ =
                stickerUI.GetSaveRotationZ();

            // -------------------------------------------------
            // SCALE
            // -------------------------------------------------
            //
            // IMPORTANT :
            // On utilise stickerScale et non
            // rt.localScale.
            //
            // Cela évite de sauvegarder le scale punch
            // du drag.
            // -------------------------------------------------

            data.scaleX =
                stickerUI.GetSaveScaleX();

            data.scaleY =
                stickerUI.GetSaveScaleY();

            // -------------------------------------------------
            // SIBLING INDEX
            // -------------------------------------------------

            data.siblingIndex =
                i;

            save.stickers.Add(
                data
            );
        }

        // =====================================================
        // JSON
        // =====================================================

        string json =
            JsonUtility.ToJson(
                save
            );

        PlayerPrefs.SetString(
            SAVE_KEY,
            json
        );

        PlayerPrefs.Save();

        // =====================================================
        // COUNT
        // =====================================================

        stickerCount =
            save.stickers.Count;

        Debug.Log(
            "Stickers sauvegardés : " +
            stickerCount
        );
    }

    // =========================================================
    // LOAD
    // =========================================================

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
        {
            Debug.Log(
                "Aucune sauvegarde de stickers trouvée."
            );

            UpdateStickerCount();

            return;
        }

        if (stickerPrefab == null)
        {
            Debug.LogError(
                "SC_StickerSaveSystem : StickerPrefab n'est pas assigné."
            );

            return;
        }

        if (parent == null)
        {
            Debug.LogError(
                "SC_StickerSaveSystem : Parent n'est pas assigné."
            );

            return;
        }

        if (stickers == null)
        {
            Debug.LogError(
                "SC_StickerSaveSystem : SO_sticker_list n'est pas assigné."
            );

            return;
        }

        // =====================================================
        // JSON
        // =====================================================

        string json =
            PlayerPrefs.GetString(
                SAVE_KEY
            );

        StickerSave save =
            JsonUtility.FromJson<StickerSave>(
                json
            );

        if (save == null ||
            save.stickers == null)
        {
            Debug.LogWarning(
                "La sauvegarde des stickers est invalide."
            );

            return;
        }

        // =====================================================
        // LOADED OBJECTS
        // =====================================================

        List<GameObject> loadedObjects =
            new List<GameObject>();

        // =====================================================
        // CREATE STICKERS
        // =====================================================

        foreach (StickerData data in save.stickers)
        {
            if (data == null)
                continue;

            // -------------------------------------------------
            // INSTANTIATE
            // -------------------------------------------------

            GameObject prefabInstance =
                Instantiate(
                    stickerPrefab,
                    parent
                );

            if (prefabInstance == null)
                continue;

            // -------------------------------------------------
            // FIND STICKER OBJECT
            // -------------------------------------------------

            GameObject stickerObject =
                prefabInstance;

            if (prefabInstance.transform.childCount > 0)
            {
                stickerObject =
                    prefabInstance.transform
                        .GetChild(0)
                        .gameObject;
            }

            // -------------------------------------------------
            // COMPONENTS
            // -------------------------------------------------

            RectTransform rt =
                stickerObject.GetComponent<RectTransform>();

            Image img =
                stickerObject.GetComponent<Image>();

            SC_sticker_UI stickerUI =
                stickerObject.GetComponent<SC_sticker_UI>();

            // -------------------------------------------------
            // FALLBACK SEARCH
            // -------------------------------------------------

            if (rt == null)
            {
                rt =
                    stickerObject.GetComponentInChildren<RectTransform>();
            }

            if (img == null)
            {
                img =
                    stickerObject.GetComponentInChildren<Image>();
            }

            if (stickerUI == null)
            {
                stickerUI =
                    stickerObject.GetComponentInChildren<SC_sticker_UI>();
            }

            if (rt == null ||
                img == null)
            {
                Debug.LogWarning(
                    "Le sticker chargé n'a pas de RectTransform ou Image."
                );

                Destroy(prefabInstance);

                continue;
            }

            // =================================================
            // SPRITE
            // =================================================

            Sprite loadedSprite =
                FindSprite(
                    data.spriteName
                );

            if (loadedSprite != null)
            {
                img.sprite =
                    loadedSprite;

                img.maskable =
                    true;
            }
            else
            {
                Debug.LogWarning(
                    "Sprite introuvable : " +
                    data.spriteName
                );
            }

            // =================================================
            // MATERIAL
            // =================================================

            Material specialMat =
                FindSpecialMaterial(
                    data.specialMatName
                );

            if (specialMat != null)
            {
                // On crée une instance pour éviter que plusieurs
                // stickers modifient le même material.
                img.material =
                    new Material(
                        specialMat
                    );
            }

            // =================================================
            // INITIALIZE SC_STICKER_UI
            // =================================================

            if (stickerUI != null)
            {
                stickerUI.InitializeLoadedSticker(
                    new Vector2(
                        data.posX,
                        data.posY
                    ),
                    data.rotZ,
                    data.scaleX,
                    data.scaleY
                );
            }
            else
            {
                // -------------------------------------------------
                // FALLBACK SANS SC_STICKER_UI
                // -------------------------------------------------

                rt.anchoredPosition =
                    new Vector2(
                        data.posX,
                        data.posY
                    );

                rt.localRotation =
                    Quaternion.Euler(
                        0f,
                        0f,
                        data.rotZ
                    );

                rt.localScale =
                    new Vector3(
                        data.scaleX,
                        data.scaleY,
                        1f
                    );
            }

            // -------------------------------------------------
            // STORE PREFAB
            // -------------------------------------------------

            loadedObjects.Add(
                prefabInstance
            );
        }

        // =====================================================
        // RESTORE HIERARCHY ORDER
        // =====================================================

        for (int i = 0;
             i < loadedObjects.Count;
             i++)
        {
            GameObject obj =
                loadedObjects[i];

            if (obj == null)
                continue;

            obj.transform.SetSiblingIndex(
                i
            );
        }

        // =====================================================
        // COUNT
        // =====================================================

        UpdateStickerCount();

        Debug.Log(
            "Stickers chargés : " +
            stickerCount
        );
    }

    // =========================================================
    // FIND SPRITE
    // =========================================================

    private Sprite FindSprite(
        string spriteName)
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

            if (sticker.sticker_sprite.name ==
                spriteName)
            {
                return sticker.sticker_sprite;
            }
        }

        return null;
    }

    // =========================================================
    // FIND STICKER
    // =========================================================

    private SO_Sticker FindSticker(
        string spriteName)
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

            if (sticker.sticker_sprite.name ==
                spriteName)
            {
                return sticker;
            }
        }

        return null;
    }

    // =========================================================
    // FIND SPECIAL MATERIAL
    // =========================================================

    private Material FindSpecialMaterial(
        string materialName)
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

            if (sticker.special_mat.name ==
                materialName)
            {
                return sticker.special_mat;
            }
        }

        return null;
    }

    // =========================================================
    // CLEAR
    // =========================================================

    public void Clear()
    {
        PlayerPrefs.DeleteKey(
            SAVE_KEY
        );

        PlayerPrefs.Save();

        stickerCount =
            0;

        Debug.Log(
            "Sauvegarde des stickers supprimée."
        );
    }

    // =========================================================
    // HAS SAVE
    // =========================================================

    public bool HasSave()
    {
        return PlayerPrefs.HasKey(
            SAVE_KEY
        );
    }
}