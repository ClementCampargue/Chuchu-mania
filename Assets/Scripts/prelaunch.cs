using UnityEngine;
using UnityEngine.SceneManagement;

public class prelaunch : MonoBehaviour
{



    void Start()
    {
        Invoke("del", 0.01f);
    }
    void del()
    {
        if (PlayerPrefs.GetInt("firstlaunchlanguage") == 0)
        {
            PlayerPrefs.SetInt("firstlaunchlanguage", 1);
            SceneManager.LoadScene("First_launch_language");
        }
        else
        {
            SceneManager.LoadScene("Title_screen");
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
