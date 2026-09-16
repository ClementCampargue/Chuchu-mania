using UnityEngine;

public class SC_destroy : MonoBehaviour
{

    public int delay_to_destroy= 0;

    public string destroy_oncollisiion_tag;
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

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (destroy_oncollisiion_tag != string.Empty)
        {
            if (collision.CompareTag(destroy_oncollisiion_tag))
            {
                Destroy(gameObject);
            }
        }
    }
}
