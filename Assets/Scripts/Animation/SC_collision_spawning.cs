using UnityEngine;

public class SC_collision_spawning : MonoBehaviour
{
    [SerializeField] private GameObject prefabOnEnter;
    [SerializeField] private GameObject prefabOnExit;

    private void OnCollisionEnter2D(Collision2D collision)
    {
        if (prefabOnEnter != null && collision.contactCount > 0)
        {
            Vector2 point = collision.GetContact(0).point;

            Instantiate(
                prefabOnEnter,
                point,
                Quaternion.identity
            );
        }
    }
   
    private void OnCollisionExit2D(Collision2D collision)
    {
        if (prefabOnExit != null && collision.contactCount > 0)
        {
            Vector2 point = collision.GetContact(0).point;

            Instantiate(
                prefabOnExit,
                point,
                Quaternion.identity
            );
        }
    }
    private void OnTriggerEnter2D(Collider2D collision)
    {

        Instantiate(
            prefabOnEnter,
            collision.transform.position,
            Quaternion.identity
        );
    }
    
    private void OnTriggerExit2D(Collider2D collision)
    {

        if (prefabOnExit != null)
        {

            Instantiate(
                prefabOnExit,
                collision.transform.position,
                Quaternion.identity
            );
        }
    }


}