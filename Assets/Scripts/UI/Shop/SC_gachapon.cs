
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

    private SO_Sticker[] currentRewards;

    public AudioSource cancel;
    public AudioSource buy_;

    private SC_SaveManager SaveManager => SC_SaveManager.instance;

    private void Start()
    {
        LoadPity();
        UpdateAmountVisuals();
    }

    private void Update()
    {
        if (!opened)
            return;

        if (submit != null &&
            submit.action != null &&
            submit.action.WasPressedThisFrame())
        {
            buy();
        }

        if (quit != null &&
            quit.action != null &&
            quit.action.WasPerformedThisFrame())
        {
            if (cancel != null)
                cancel.Play();

            opened = false;
            Invoke(nameof(close_gacha), 0.05f);
        }

        HandleMove();
    }

    // ============================================================
    // NAVIGATION
    // ============================================================

    private void HandleMove()
    {
        if (move == null || move.action == null)
            return;

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

            update_amount(input.x > 0 ? 1 : -1);
            return;
        }

        moveTimer -= Time.deltaTime;

        if (moveTimer <= 0f)
        {
            moveTimer = repeatDelay;
            update_amount(input.x > 0 ? 1 : -1);
        }
    }

    public void update_amount(int amount_)
    {
        if (SaveManager == null || SaveManager.data == null)
            return;

        int newAmount = Mathf.Clamp(amount_to_buy + amount_, 1, 10);
        int newPrice = newAmount * price_for_one;

        if (newPrice > SaveManager.data.money)
            return;

        amount_to_buy = newAmount;
        UpdateAmountVisuals();
    }

    private void UpdateAmountVisuals()
    {
        if (amount != null)
            amount.text = amount_to_buy.ToString();

        if (price != null)
            price.text = (amount_to_buy * price_for_one) + "$";
    }

    // ============================================================
    // OPEN / CLOSE
    // ============================================================

    public void open_gacha()
    {
        if (SaveManager == null || SaveManager.data == null)
        {
            Debug.LogWarning("Gachapon : SaveManager ou les données de sauvegarde sont indisponibles.");
            return;
        }

        canbuy = true;
        opened = false;

        CancelInvoke(nameof(delay_open));
        CancelInvoke(nameof(close_gacha));

        amount_to_buy = 1;
        UpdateAmountVisuals();

        if (shop != null)
            shop.canquit = false;

        Invoke(nameof(delay_open), 0.1f);
    }

    private void delay_open()
    {
        opened = true;
    }

    public void close_gacha()
    {
        if (shop != null)
            shop.show_menu();
    }

    // ============================================================
    // ACHAT
    // ============================================================

    public void buy()
    {
        if (!canbuy)
            return;

        if (SaveManager == null || SaveManager.data == null)
        {
            Debug.LogWarning("Gachapon : impossible d'acheter sans données de sauvegarde.");
            return;
        }

        if (stickers == null ||
            stickers.stickers == null ||
            stickers.stickers.Count == 0)
        {
            Debug.LogWarning("Gachapon : aucun sticker disponible.");
            return;
        }

        int totalPrice = amount_to_buy * price_for_one;

        if (totalPrice < 0 || SaveManager.data.money < totalPrice)
            return;

        // Tirer toutes les récompenses avant de confirmer l'achat.
        // Si aucune récompense valide n'est disponible, rien n'est dépensé.
        SO_Sticker[] rewards = GetRandomStickers(amount_to_buy);

        if (rewards == null || rewards.Length != amount_to_buy)
        {
            Debug.LogWarning("Gachapon : impossible de générer les récompenses.");
            return;
        }

        foreach (SO_Sticker reward in rewards)
        {
            if (reward == null)
            {
                Debug.LogWarning("Gachapon : une récompense est invalide. Achat annulé.");
                return;
            }
        }

        currentRewards = rewards;

        if (shop != null && shop.anim != null)
            shop.anim.SetTrigger("open");

        if (buy_ != null)
            buy_.Play();

        opened = false;
        canbuy = false;

        capsulesOpened += amount_to_buy;
        SavePity();

        // Le système monétaire existant traite le paiement.
        if (SC_money_shop.instance != null)
            SC_money_shop.instance.future_price = totalPrice;
        else
            Debug.LogWarning("Gachapon : SC_money_shop.instance est null.");

        CancelInvoke(nameof(spawn_capsules));
        CancelInvoke(nameof(show_screen));

        Invoke(nameof(spawn_capsules), 1f);
        Invoke(nameof(show_screen), 2f);
    }

    // ============================================================
    // RANDOM STICKERS + PITY
    // ============================================================

    private SO_Sticker[] GetRandomStickers(int quantity)
    {
        if (quantity <= 0)
            return null;

        SO_Sticker[] rewards = new SO_Sticker[quantity];

        for (int i = 0; i < quantity; i++)
        {
            rewards[i] = GetRandomStickerWithPity();

            if (rewards[i] == null)
                return null;
        }

        return rewards;
    }

    private SO_Sticker GetRandomStickerWithPity()
    {
        if (SaveManager == null || SaveManager.data == null)
        {
            Debug.LogWarning("Gachapon : SaveManager indisponible pour vérifier les stickers débloqués.");
            return null;
        }

        if (stickers == null ||
            stickers.stickers == null ||
            stickers.stickers.Count == 0)
        {
            Debug.LogWarning("Aucun sticker disponible dans la liste.");
            return null;
        }

        List<SO_Sticker> validStickers = new List<SO_Sticker>();

        foreach (SO_Sticker sticker in stickers.stickers)
        {
            if (sticker != null)
                validStickers.Add(sticker);
        }

        if (validStickers.Count == 0)
        {
            Debug.LogWarning("Gachapon : la liste ne contient aucun sticker valide.");
            return null;
        }

        float pityMultiplier = GetPityMultiplier();
        float totalWeight = 0f;

        foreach (SO_Sticker sticker in validStickers)
        {
            totalWeight += GetStickerWeight(sticker, pityMultiplier);
        }

        if (totalWeight <= 0f)
            return validStickers[Random.Range(0, validStickers.Count)];

        float randomValue = Random.Range(0f, totalWeight);

        foreach (SO_Sticker sticker in validStickers)
        {
            randomValue -= GetStickerWeight(sticker, pityMultiplier);

            if (randomValue <= 0f)
                return sticker;
        }

        return validStickers[validStickers.Count - 1];
    }

    private float GetStickerWeight(SO_Sticker sticker, float pityMultiplier)
    {
        if (sticker == null || SaveManager == null)
            return 0f;

        // L'état de déblocage provient exclusivement de SC_SaveManager.
        bool isUnlocked = SaveManager.IsStickerUnlocked(sticker);

        if (isUnlocked)
            return keepUnlockedWeightAtOne ? 1f : 0.5f;

        return Mathf.Max(0f, pityMultiplier);
    }

    private float GetPityMultiplier()
    {
        float multiplier = 1f + (capsulesOpened * pityPerCapsule);

        return Mathf.Clamp(multiplier, 1f, maxPityMultiplier);
    }

    // ============================================================
    // PITY SAVE / LOAD
    // ============================================================

    private void LoadPity()
    {
        capsulesOpened = Mathf.Max(
            0,
            PlayerPrefs.GetInt(CAPSULES_OPENED_KEY, 0)
        );
    }

    private void SavePity()
    {
        PlayerPrefs.SetInt(CAPSULES_OPENED_KEY, capsulesOpened);
        PlayerPrefs.Save();
    }

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
        if (currentRewards == null || currentRewards.Length == 0)
        {
            Debug.LogWarning("Gachapon : aucune récompense à afficher.");
            canbuy = true;
            return;
        }

        if (SC_sticker_popup.instance == null)
        {
            Debug.LogWarning("Gachapon : SC_sticker_popup.instance est null.");
            canbuy = true;
            return;
        }

        SC_sticker_popup.instance.ShowStickers(currentRewards);
    }

    private IEnumerator SpawnCapsules()
    {
        if (points == null || points.Count == 0)
        {
            Debug.LogWarning("Gachapon : aucun point de destination configuré.");
            yield break;
        }

        if (capsule == null)
        {
            Debug.LogWarning("Gachapon : le prefab de capsule n'est pas assigné.");
            yield break;
        }

        int quantity = Mathf.Min(amount_to_buy, points.Count);

        for (int i = 0; i < quantity; i++)
        {
            if (points[i] == null)
                continue;

            GameObject newCapsule = Instantiate(
                capsule,
                transform.position,
                transform.rotation
            );

            yield return StartCoroutine(
                MoveCapsule(newCapsule.transform, points[i])
            );

            if (spawnDelay > 0f)
                yield return new WaitForSeconds(spawnDelay);
        }

        canbuy = true;
    }

    private IEnumerator MoveCapsule(Transform capsuleTransform, Transform target)
    {
        if (capsuleTransform == null || target == null)
            yield break;

        Vector3 startPosition = capsuleTransform.position;
        Vector3 targetPosition = target.position;

        if (capsuleMoveDuration <= 0f)
        {
            capsuleTransform.position = targetPosition;
            yield break;
        }

        float timer = 0f;

        while (timer < capsuleMoveDuration)
        {
            timer += Time.deltaTime;

            float t = Mathf.Clamp01(timer / capsuleMoveDuration);
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
