using UnityEngine;

public class SC_destroy : MonoBehaviour
{

    public int delay_to_destroy= 0;
    void Start()
    {
        if(delay_to_destroy != 0)
        {
            Invoke("destroy", delay_to_destroy);
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void destroy()
    {
        Destroy(gameObject);
    }
}
