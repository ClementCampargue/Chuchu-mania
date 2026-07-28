using UnityEngine;

public class SC_gravity_base_anim : MonoBehaviour
{
    private SC_gravity_flip gravity;
    private bool up = false;
    public Animator anim;
    void Start()
    {
        gravity = SC_gravity_flip.instance;
    }

    // Update is called once per frame
    void Update()
    {
        if (!gravity.gravity_up && up)
        {
            up = false;
            anim.SetTrigger("down");
        }
        else if(gravity.gravity_up && !up)
        {
            up = true;
            anim.SetTrigger("up");
        }
    }
}
