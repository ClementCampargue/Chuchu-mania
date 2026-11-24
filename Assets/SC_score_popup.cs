using TMPro;
using UnityEngine;

public class SC_score_popup : MonoBehaviour
{
    public GameObject heart;
    public TextMeshPro text;

    public Color green;
    public Color yellow;
    public Color white;
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void display_text(bool heart_, int score, int creams)
    {
        if(creams == 10)
        {
            text.color = white;
        }
        else if (creams> 4)
        {
            text.color = yellow;
        }
        else
        {
            text.color = green;
        }
        heart.SetActive(heart_);
        text.text = score.ToString();
    }
}
