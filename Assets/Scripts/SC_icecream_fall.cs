using UnityEngine;
using System.Collections;

public class SC_icecream_fall : MonoBehaviour
{
    public enum IceCreamType { Vanilla, Chocolate, Lemon, Milk, Strawberry }
    public IceCreamType type;

    public float gravity = 9.8f;
    public float bounceForce = 5f;
    public float blinkDuration = 2f;
    public float blinkInterval = 0.1f;

    private Vector3 velocity = Vector3.zero;
    public bool hasLanded = false;

    public bool isSelected = false;

    public SpriteRenderer spriteRenderer;
    public SC_juiciness juice;

    public Transform currentTargetPosition;
    public GameObject selected_sprite;
    void Update()
    {
        if (!hasLanded && currentTargetPosition != null)
        {
            velocity.y -= gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            if (transform.position.y <= currentTargetPosition.position.y)
            {
                transform.position = new Vector3(
                    transform.position.x,
                    currentTargetPosition.position.y,
                    transform.position.z
                );

                hasLanded = true;
                juice.PlayJuice();
            }
        }
    }

    public void Select()
    {
        if (isSelected) return;
        selected_sprite.SetActive(true);
        isSelected = true;
        transform.localScale = Vector3.one * 1.2f;
    }

    public void Deselect()
    {
        selected_sprite.SetActive(false);

        isSelected = false;
        transform.localScale = Vector3.one;
    }

    public void Eat()
    {
        if (!hasLanded && currentTargetPosition != null)
        {
            transform.position = new Vector3(
                transform.position.x,
                currentTargetPosition.position.y,
                transform.position.z
            );
            hasLanded = true;
        }

        juice.PlayJuice();
        BounceAndBlink();
    }

    public void BounceAndBlink()
    {
        StartCoroutine(BounceAndBlinkCoroutine());
    }

    IEnumerator BounceAndBlinkCoroutine()
    {
        float randomDir = Random.Range(-1f, 1f);
        velocity = new Vector3(randomDir * bounceForce, bounceForce, 0f);

        float elapsed = 0f;
        bool visible = true;

        while (elapsed < blinkDuration)
        {
            velocity.y -= gravity * Time.deltaTime;
            transform.position += velocity * Time.deltaTime;

            elapsed += Time.deltaTime;
            visible = (elapsed % blinkInterval) < (blinkInterval / 2);
            spriteRenderer.enabled = visible;

            yield return null;
        }

        gameObject.SetActive(false);
    }
}