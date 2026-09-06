using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_gachapon : MonoBehaviour
{
    public SO_sticker_list stickers;
    public SC_shop_manager shop;

    public InputActionReference submit;
    public InputActionReference move;
    public InputActionReference quit;
    public bool opened;

    public TextMeshPro price;
    public TextMeshPro amount;

    public int amount_to_buy = 1;
    public int price_for_one;

    [Header("Navigation")]
    public float firstRepeatDelay = 0.4f;
    public float repeatDelay = 0.1f;

    private float moveTimer;
    private bool isMoving;

    [Header("Capsules")]
    public List<Transform> points;
    public GameObject capsule;
    public GameObject capsule_rare;

    [Tooltip("Temps entre chaque capsule")]
    public float spawnDelay = 0.25f;

    [Tooltip("Temps de déplacement d'une capsule")]
    public float capsuleMoveDuration = 0.5f;

    public bool canbuy = true;

    void Update()
    {
        if (!opened) return;

        if (submit.action.WasPressedThisFrame())
        {
            buy();
        }

        if (quit.action.WasPerformedThisFrame())
        {
            opened = false;
            Invoke("close_gacha", 0.05f);
        }

        HandleMove();
    }

    void HandleMove()
    {
        Vector2 input = move.action.ReadValue<Vector2>();

        if (Mathf.Abs(input.x) < 0.5f)
        {
            isMoving = false;
            moveTimer = 0f;
            return;
        }

        if (!isMoving)
        {
            isMoving = true;
            moveTimer = firstRepeatDelay;

            if (input.x > 0)
                update_amount(1);
            else if (input.x < 0)
                update_amount(-1);

            return;
        }

        moveTimer -= Time.deltaTime;

        if (moveTimer <= 0f)
        {
            moveTimer = repeatDelay;

            if (input.x > 0)
                update_amount(1);
            else if (input.x < 0)
                update_amount(-1);
        }
    }

    public void update_amount(int amount_)
    {
        if (amount_ > 0)
        {
            int newAmount = Mathf.Clamp(amount_to_buy + amount_, 1, 10);
            int newPrice = newAmount * price_for_one;

            if (SC_money_shop.instance.money < newPrice)
                return;
        }

        amount_to_buy += amount_;
        amount_to_buy = Mathf.Clamp(amount_to_buy, 1, 10);

        amount.text = amount_to_buy.ToString();
        price.text = (amount_to_buy * price_for_one).ToString() + "$";
    }

    public void open_gacha()
    {
        canbuy = true;

        Invoke("delay_open", 0.1f);
        shop.canquit = false;
        amount_to_buy = 1;
        update_amount(0);
    }

    void delay_open()
    {
        opened = true;
    }

    public void close_gacha()
    {
        shop.show_menu();
    }

    public void buy()
    {
        if (!canbuy)
            return;
        shop.anim.SetTrigger("open");
        // Vérification de l'argent
        int totalPrice = amount_to_buy * price_for_one;

        if (SC_money_shop.instance.money < totalPrice)
            return;
        opened = false;

        canbuy = false;
        GetRandomStickers(amount_to_buy);
        SC_money_shop.instance.Buy(totalPrice);
        Invoke("spawn_capsules",1);
        Invoke("show_screen", 2);

    }
    private SO_Sticker[] GetRandomStickers(int amount)
    {
        SO_Sticker[] rewards = new SO_Sticker[amount];

        for (int i = 0; i < amount; i++)
        {
            int randomIndex = Random.Range(0, stickers.stickers.Count);
            rewards[i] = stickers.stickers[randomIndex];
        }

        return rewards;
    }

    public void spawn_capsules()
    {

        StartCoroutine(SpawnCapsules());
    }

    public void show_screen()
    {
        SO_Sticker[] rewards = GetRandomStickers(amount_to_buy);

        SC_sticker_popup.instance.ShowStickers(rewards);
    }

    IEnumerator SpawnCapsules()
    {
        int quantity = Mathf.Min(amount_to_buy, points.Count);

        for (int i = 0; i < quantity; i++)
        {
            // Spawn au centre de l'objet
            GameObject newCapsule = Instantiate(
                capsule,
                transform.position,
                transform.rotation
            );

            // Destination
            Transform target = points[i];

            // Déplacement vers le point
            yield return StartCoroutine(
                MoveCapsule(newCapsule.transform, target)
            );

            // Petite pause avant la suivante
            yield return new WaitForSeconds(spawnDelay);
        }

        canbuy = true;
    }

    IEnumerator MoveCapsule(Transform capsuleTransform, Transform target)
    {
        Vector3 startPosition = capsuleTransform.position;
        Vector3 targetPosition = target.position;

        float timer = 0f;

        while (timer < capsuleMoveDuration)
        {
            timer += Time.deltaTime;

            float t = timer / capsuleMoveDuration;

            // Petite accélération/décélération
            t = Mathf.SmoothStep(0f, 1f, t);

            capsuleTransform.position = Vector3.Lerp(
                startPosition,
                targetPosition,
                t
            );

            yield return null;
        }

        capsuleTransform.position = targetPosition;
    }
}