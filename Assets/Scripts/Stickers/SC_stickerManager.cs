using System.Collections.Generic;
using UnityEngine;

public class SC_stickerManager : MonoBehaviour
{
    public static SC_stickerManager instance;

    [Header("References")]
    public List<SO_Sticker> stickerList;
    public GameObject button;
    public GameObject stickerPrefab;

    private int currentTopOrder = 0;

    [System.Serializable]
    public class StickerData
    {
        public string spriteName;

        public float posX;
        public float posY;
        public float posZ;

        public float scaleX;
        public float scaleY;
        public float scaleZ;

        public float rotZ;

        public int sortingOrder;
    }

    [System.Serializable]
    public class StickerSave
    {
        public List<StickerData> stickers = new List<StickerData>();
    }

    void Awake()
    {
        instance = this;

        // boutons
        for (int i = 0; i < stickerList.Count; i++)
        {
            Transform trs = Instantiate(button, transform).transform;

            trs.GetComponent<SC_sticker_button>().sticker = stickerList[i];
        }

        LoadStickers();
    }

    public int GetTopSortingOrder()
    {
        currentTopOrder++;
        return currentTopOrder;
    }

    public void SaveStickers()
    {
        SC_sticker[] all = FindObjectsOfType<SC_sticker>();

        StickerSave save = new StickerSave();

        foreach (SC_sticker st in all)
        {
            // sauvegarde seulement les stickers créés
            if (!st.spawnedSticker)
                continue;

            SpriteRenderer sr = st.GetComponent<SpriteRenderer>();

            StickerData data = new StickerData();

            data.spriteName = sr.sprite.name;

            data.posX = st.transform.position.x;
            data.posY = st.transform.position.y;
            data.posZ = st.transform.position.z;

            data.scaleX = st.transform.localScale.x;
            data.scaleY = st.transform.localScale.y;
            data.scaleZ = st.transform.localScale.z;

            data.rotZ = st.transform.eulerAngles.z;

            data.sortingOrder = sr.sortingOrder;

            save.stickers.Add(data);
        }

        string json = JsonUtility.ToJson(save);

        PlayerPrefs.SetString("stickers_save", json);

        PlayerPrefs.Save();
    }

    public void LoadStickers()
    {
        if (!PlayerPrefs.HasKey("stickers_save"))
            return;

        string json = PlayerPrefs.GetString("stickers_save");

        StickerSave save = JsonUtility.FromJson<StickerSave>(json);

        if (save == null)
            return;

        foreach (StickerData data in save.stickers)
        {
            GameObject obj = Instantiate(stickerPrefab);

            obj.transform.position = new Vector3(
                data.posX,
                data.posY,
                data.posZ
            );

            obj.transform.localScale = new Vector3(
                data.scaleX,
                data.scaleY,
                data.scaleZ
            );

            obj.transform.rotation = Quaternion.Euler(
                0,
                0,
                data.rotZ
            );

            SpriteRenderer sr = obj.GetComponent<SpriteRenderer>();

            // sprite
            foreach (SO_Sticker st in stickerList)
            {
                if (st.sticker_sprite.name == data.spriteName)
                {
                    sr.sprite = st.sticker_sprite;
                    break;
                }
            }

            sr.sortingOrder = data.sortingOrder;

            // material unique
            sr.material = new Material(sr.material);

            SC_sticker sticker = obj.GetComponent<SC_sticker>();

            sticker.spawnedSticker = true;

            currentTopOrder = Mathf.Max(currentTopOrder, data.sortingOrder);
        }
    }

    public void ClearSave()
    {
        PlayerPrefs.DeleteKey("stickers_save");
    }
}