using UnityEngine;

public class SC_anti_flip : MonoBehaviour
{
    public Transform parent;
    public Transform objet;

    void Update()
    {
        objet.localScale = new Vector3(
            1f / parent.localScale.x,
            objet.localScale.y,
            objet.localScale.z
        );
    }
}
