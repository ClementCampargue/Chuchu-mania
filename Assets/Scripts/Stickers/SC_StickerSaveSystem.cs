using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SC_StickerSaveSystem : MonoBehaviour
{
    public static SC_StickerSaveSystem instance;

    [Header("Prefabs")]
    public GameObject stickerPrefab;
    public Transform parent; // canvas container
    public List<Sprite> sprites; // toutes les sprites possibles

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

    void Awake()
    {
        instance = this;
    }

    void Start()
    {
        Load(); 
    }

    public void AutoSave()
    {
        Save();
    }

    public void Save()
    {
        SC_sticker_UI[] stickers = FindObjectsOfType<SC_sticker_UI>();
        Debug.Log("Sticker count: " + FindObjectsOfType<SC_sticker_UI>().Length);
        StickerSave save = new StickerSave();

        foreach (SC_sticker_UI st in stickers)
        {
            Image img = st.GetComponent<Image>();
            RectTransform rt = st.GetComponent<RectTransform>();

            StickerData data = new StickerData();

            data.spriteName = img.sprite != null ? img.sprite.name : "";

            data.posX = rt.anchoredPosition.x;
            data.posY = rt.anchoredPosition.y;

            data.rotZ = rt.localEulerAngles.z;

            data.scaleX = rt.localScale.x;
            data.scaleY = rt.localScale.y;

            data.siblingIndex = rt.GetSiblingIndex();

            save.stickers.Add(data);
        }

        PlayerPrefs.SetString(SAVE_KEY, JsonUtility.ToJson(save));
        PlayerPrefs.Save();
    }

    public void Load()
    {
        if (!PlayerPrefs.HasKey(SAVE_KEY))
            return;

        string json = PlayerPrefs.GetString(SAVE_KEY);
        StickerSave save = JsonUtility.FromJson<StickerSave>(json);

        foreach (StickerData data in save.stickers)
        {
            GameObject obj = Instantiate(stickerPrefab, parent);

            RectTransform rt = obj.GetComponent<RectTransform>();
            Image img = obj.GetComponent<Image>();

            rt.anchoredPosition = new Vector2(data.posX, data.posY);
            rt.localEulerAngles = new Vector3(0, 0, data.rotZ);
            rt.localScale = new Vector3(data.scaleX, data.scaleY, 1);

            img.sprite = sprites.Find(s => s.name == data.spriteName);
            img.SetNativeSize();
            img.maskable = true;
            obj.transform.SetSiblingIndex(data.siblingIndex);

            obj.GetComponent<SC_sticker_UI>().spawnedSticker = true;
        }
    }

    public void Clear()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
    }
}