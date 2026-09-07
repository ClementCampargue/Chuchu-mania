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

    // ============================================================
    // PITY SYSTEM
    // ============================================================

    [Header("Pity System")]

    [Tooltip("Nombre total de capsules ouvertes. Sauvegardé avec PlayerPrefs.")]
    public int capsulesOpened = 0;

    [Tooltip("Augmentation du poids des stickers non débloqués par capsule ouverte.")]
    public float pityPerCapsule = 0.05f;

    [Tooltip("Multiplicateur maximum appliqué aux stickers non débloqués.")]
    public float maxPityMultiplier = 10f;

    [Tooltip("Si activé, les stickers déjà débloqués ont toujours un poids de 1.")]
    public bool keepUnlockedWeightAtOne = true;

    private const string CAPSULES_OPENED_KEY = "Gachapon_CapsulesOpened";

    // Les récompenses tirées sont conservées ici pour éviter de les reroll
    private SO_Sticker[] currentRewards;

    // ============================================================
    // UPDATE
    // ============================================================

    void Start()
    {
        LoadPity();
    }

    void Update()
    {
        if (!opened)
            return;

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

    // ============================================================
    // NAVIGATION
    // ============================================================

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

    // ============================================================
    // OPEN / CLOSE
    // ============================================================

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

    // ============================================================
    // ACHAT
    // ============================================================

    public void buy()
    {
        if (!canbuy)
            return;

        // Vérification de l'argent
        int totalPrice = amount_to_buy * price_for_one;

        if (SC_money_shop.instance.money < totalPrice)
            return;

        shop.anim.SetTrigger("open");

        opened = false;
        canbuy = false;

        // IMPORTANT :
        // On tire les récompenses UNE SEULE FOIS.
        currentRewards = GetRandomStickers(amount_to_buy);

        // On compte les capsules ouvertes
        capsulesOpened += amount_to_buy;
        SavePity();

        // Paiement
        SC_money_shop.instance.Buy(totalPrice);

        Invoke("spawn_capsules", 1f);
        Invoke("show_screen", 2f);
    }

    // ============================================================
    // RANDOM STICKERS + PITY
    // ============================================================

    private SO_Sticker[] GetRandomStickers(int amount)
    {
        SO_Sticker[] rewards = new SO_Sticker[amount];

        for (int i = 0; i < amount; i++)
        {
            rewards[i] = GetRandomStickerWithPity();
        }

        return rewards;
    }

    private SO_Sticker GetRandomStickerWithPity()
    {
        if (stickers == null ||
            stickers.stickers == null ||
            stickers.stickers.Count == 0)
        {
            Debug.LogWarning("Aucun sticker disponible dans la liste.");
            return null;
        }

        float pityMultiplier = GetPityMultiplier();

        // --------------------------------------------------------
        // Calcul du poids total
        // --------------------------------------------------------

        float totalWeight = 0f;

        foreach (SO_Sticker sticker in stickers.stickers)
        {
            if (sticker == null)
                continue;

            float weight = GetStickerWeight(sticker, pityMultiplier);
            totalWeight += weight;
        }

        if (totalWeight <= 0f)
            return stickers.stickers[Random.Range(0, stickers.stickers.Count)];

        // --------------------------------------------------------
        // Tirage pondéré
        // --------------------------------------------------------

        float randomValue = Random.Range(0f, totalWeight);

        foreach (SO_Sticker sticker in stickers.stickers)
        {
            if (sticker == null)
                continue;

            float weight = GetStickerWeight(sticker, pityMultiplier);

            randomValue -= weight;

            if (randomValue <= 0f)
                return sticker;
        }

        // Sécurité
        return stickers.stickers[stickers.stickers.Count - 1];
    }

    private float GetStickerWeight(SO_Sticker sticker, float pityMultiplier)
    {
        // Sticker déjà débloqué
        if (sticker.unlocked)
        {
            return keepUnlockedWeightAtOne ? 1f : 0.5f;
        }

        // Sticker non débloqué :
        // son poids augmente avec le pity.
        return pityMultiplier;
    }

    private float GetPityMultiplier()
    {
        /*
         * Exemple avec pityPerCapsule = 0.05 :
         *
         * 0 capsule  -> x1
         * 10 capsules -> x1.5
         * 20 capsules -> x2
         * 50 capsules -> x3.5
         * 100 capsules -> x6
         *
         * Le maximum est limité par maxPityMultiplier.
         */

        float multiplier = 1f + (capsulesOpened * pityPerCapsule);

        multiplier = Mathf.Clamp(
            multiplier,
            1f,
            maxPityMultiplier
        );

        return multiplier;
    }

    // ============================================================
    // PITY SAVE / LOAD
    // ============================================================

    private void LoadPity()
    {
        capsulesOpened = PlayerPrefs.GetInt(
            CAPSULES_OPENED_KEY,
            0
        );
    }

    private void SavePity()
    {
        PlayerPrefs.SetInt(
            CAPSULES_OPENED_KEY,
            capsulesOpened
        );

        PlayerPrefs.Save();
    }

    // Utile pour tester le système dans l'inspecteur
    [ContextMenu("Reset Pity")]
    public void ResetPity()
    {
        capsulesOpened = 0;

        PlayerPrefs.DeleteKey(CAPSULES_OPENED_KEY);
        PlayerPrefs.Save();

        Debug.Log("Pity reset.");
    }

    // ============================================================
    // CAPSULES
    // ============================================================

    public void spawn_capsules()
    {
        StartCoroutine(SpawnCapsules());
    }

    public void show_screen()
    {
        // On utilise les récompenses déjà tirées dans buy().
        // Cela évite de tirer de nouveaux stickers.
        if (currentRewards == null)
        {
            Debug.LogWarning("Aucune récompense trouvée.");
            return;
        }

        SC_sticker_popup.instance.ShowStickers(currentRewards);
    }

    IEnumerator SpawnCapsules()
    {
        int quantity = Mathf.Min(amount_to_buy, points.Count);

        for (int i = 0; i < quantity; i++)
        {
            GameObject newCapsule = Instantiate(
                capsule,
                transform.position,
                transform.rotation
            );

            Transform target = points[i];

            yield return StartCoroutine(
                MoveCapsule(newCapsule.transform, target)
            );

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
