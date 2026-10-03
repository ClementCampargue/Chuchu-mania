using UnityEngine;
using UnityEngine.UI;

public class SC_mimic_image : MonoBehaviour
{

    public Image img;
    private Image img2;
    void Start()
    {
        img2 = GetComponent<Image>();
    }

    private void OnEnable()
    {
        img2.sprite = img.sprite;

    }

    // Update is called once per frame
    void Update()
    {
    }
}
