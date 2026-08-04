using System.Collections;
using UnityEngine;

public class SC_spotlight : MonoBehaviour
{
    public float delai = 3f;

    private bool joueurDansZone = false;
    public Animator anim;
    public GameObject bonus;
    private void OnEnable()
    {
        Invoke(nameof(VerifierJoueur), delai);
    }

    private void OnDisable()
    {
        CancelInvoke(nameof(VerifierJoueur));
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            joueurDansZone = true;
        }
    }

    private void OnTriggerExit2D(Collider2D collision)
    {
        if (collision.CompareTag("Player"))
        {
            joueurDansZone = false;
        }
    }

    public void VerifierJoueur()
    {
        if (joueurDansZone)
        {
            JoueurDetecte();
        }
        else
        {
            JoueurNonDetecte();
        }
    }



    private void JoueurDetecte()
    {
        Debug.Log("Le joueur est dans la zone !");
        anim.SetTrigger("Inside");
        Instantiate(bonus, transform.position, Quaternion.identity);

    }

    private void JoueurNonDetecte()
    {
        Debug.Log("Le joueur n'est pas dans la zone !");
       // SC_player.instance.TakeDamage(1,transform.position);
        anim.SetTrigger("Outside");
    }

}