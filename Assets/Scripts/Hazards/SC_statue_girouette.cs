using UnityEngine;

public class SC_statue_girouette : MonoBehaviour
{
    public SC_girouette[] girouettes;

    private bool alreadyTriggered = false;
    public Animator anim;
    public Transform spawn_pos;
    public GameObject collectible;
    void Update()
    {
        if (alreadyTriggered)
            return;

        bool allRotating = true;

        foreach (SC_girouette g in girouettes)
        {
            if (!g.isRotating)
            {
                allRotating = false;
                break;
            }
        }

        if (allRotating)
        {
            alreadyTriggered = true;
            TriggerEvent();
        }
    }

    void TriggerEvent()
    {
        anim.enabled =true;
    }

    public void instantiate_collectible()
    {
        Instantiate(collectible, spawn_pos.position, Quaternion.identity);
    } 
}
