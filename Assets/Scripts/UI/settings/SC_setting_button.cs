using UnityEngine;
using UnityEngine.Events;

public class SC_setting_button : MonoBehaviour
{

    public SC_slider_ slider;
    public SC_list_text texts;

    public string parameter;

    void Start()
    {
        if (slider != null)
        {
            slider.SetValue(PlayerPrefs.GetFloat(parameter));
        }
        if (texts != null)
        {
            texts.SetIndex((int) PlayerPrefs.GetFloat(parameter));
        }
    }
    private void Update()
    {
        if (slider != null)
        {
            PlayerPrefs.SetFloat(parameter, slider.Value);
        }
        if (texts != null)
        {
            PlayerPrefs.SetFloat(parameter, texts.index);
        }
        PlayerPrefs.Save();

    }
}
