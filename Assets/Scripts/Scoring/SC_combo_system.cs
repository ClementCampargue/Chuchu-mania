using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using static SC_icecream_fall;

public class SC_combo_system : MonoBehaviour
{
    [Header("Combo Requirements")]
    public int minUniform = 4;
    public int minAlternance = 4;
    public int minSandwich = 3;

    public static SC_combo_system Instance;

    public List<IceCreamType> currentCombo = new List<IceCreamType>();
    public TextMeshPro text; // TextMeshPro 3D
    private Color originalColor;

    public enum ComboType
    {
        Full,
        Alternance,
        Sandwich,
        Suite
    }

    public ComboType combotype;

    private float nextComboBonusMultiplier = 1f;

    private void Awake()
    {
        Instance = this;

        if (text != null)
        {
            originalColor = text.color;
            text.gameObject.SetActive(false); // cache le texte au départ
        }
    }

    public void ResetCombo()
    {
        currentCombo.Clear();
    }

    public void AddToCombo(IceCreamType type)
    {
        currentCombo.Add(type);

        if (currentCombo.Count >= 3)
        {
            CheckCombos();
        }
    }

    void CheckCombos()
    {
        if (IsSuite())
        {
            combotype = ComboType.Suite;
            SC_point_boost.Instance.ActivateBoost();
            nextComboBonusMultiplier += 0.5f;
            ShowComboText("SUITE !");
        }
        else if (IsUniform() && currentCombo.Count >= minUniform)
        {
            combotype = ComboType.Full;
            SC_freeze_screen.Instance.FreezeAllExceptPlayer();
            ApplyBonus(1f);
            ShowComboText("UNIFORME !");
        }
        else if (IsAlternating() && currentCombo.Count >= minAlternance)
        {
            combotype = ComboType.Alternance;
            SC_magnet_power_up.Instance.ActivateMagnet(5);
            ApplyBonus(1f);
            ShowComboText("ALTERNÉ !");
        }
        else if (IsSandwich() && currentCombo.Count >= minSandwich)
        {
            combotype = ComboType.Sandwich;
            SC_player.instance.TriggerInvincibility(2);
            ApplyBonus(1f);
            ShowComboText("SANDWICH !");
        }
    }

    void ShowComboText(string comboMessage)
    {
        if (text == null) return;

        text.text = comboMessage;
        text.gameObject.SetActive(true);
        text.transform.localScale = Vector3.zero;
        text.color = new Color(originalColor.r, originalColor.g, originalColor.b, 1f); // reset alpha

        StopAllCoroutines(); // stop les animations précédentes
        StartCoroutine(AnimateComboText());
    }

    private IEnumerator AnimateComboText()
    {
        float duration = 0.5f;
        float elapsed = 0f;

        Vector3 startScale = Vector3.zero;
        Vector3 endScale = Vector3.one * 1.2f;

        // Scale up
        while (elapsed < duration)
        {
            text.transform.localScale = Vector3.Lerp(startScale, endScale, elapsed / duration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.transform.localScale = endScale;

        // Petit rebond de retour à 1
        float bounceDuration = 0.2f;
        elapsed = 0f;
        while (elapsed < bounceDuration)
        {
            text.transform.localScale = Vector3.Lerp(endScale, Vector3.one, elapsed / bounceDuration);
            elapsed += Time.deltaTime;
            yield return null;
        }
        text.transform.localScale = Vector3.one;

        // Attendre 0.5 sec avant fade
        yield return new WaitForSeconds(0.5f);

        // Fade out
        float fadeDuration = 0.5f;
        elapsed = 0f;
        Color c = text.color;
        while (elapsed < fadeDuration)
        {
            float alpha = Mathf.Lerp(1f, 0f, elapsed / fadeDuration);
            text.color = new Color(c.r, c.g, c.b, alpha);
            elapsed += Time.deltaTime;
            yield return null;
        }

        text.color = new Color(c.r, c.g, c.b, 0f);
        text.gameObject.SetActive(false);
    }

    // --- Méthodes de combo identiques ---
    bool IsSuite()
    {
        if (currentCombo.Count < 5) return false;

        int startIndex = currentCombo.Count - 5;
        HashSet<IceCreamType> lastFive = new HashSet<IceCreamType>();

        for (int i = startIndex; i < currentCombo.Count; i++)
        {
            if (lastFive.Contains(currentCombo[i]))
                return false;
            lastFive.Add(currentCombo[i]);
        }

        return true;
    }

    bool IsUniform()
    {
        IceCreamType first = currentCombo[0];
        foreach (var t in currentCombo)
        {
            if (t != first)
                return false;
        }
        return true;
    }

    bool IsAlternating()
    {
        if (currentCombo.Count < 3) return false;

        for (int i = 2; i < currentCombo.Count; i++)
        {
            if (currentCombo[i] != currentCombo[i - 2])
                return false;
        }
        return true;
    }

    bool IsSandwich()
    {
        if (currentCombo.Count < 5) return false;

        int midStart = (currentCombo.Count - 3) / 2;
        IceCreamType middleType = currentCombo[midStart];

        for (int i = midStart; i < midStart + 3; i++)
        {
            if (currentCombo[i] != middleType)
                return false; // les 3 boules du milieu ne sont pas identiques
        }

        return true;
    }

    void ApplyBonus(float multiplier)
    {
        float totalMultiplier = multiplier * nextComboBonusMultiplier;

        int bonus = Mathf.RoundToInt(100 * totalMultiplier);
        SC_score.Instance.AddScore(bonus);

        currentCombo.Clear();
        nextComboBonusMultiplier = 1f;
    }

    public float ConsumeNextComboBonus()
    {
        float bonus = nextComboBonusMultiplier;
        nextComboBonusMultiplier = 1f;
        return bonus;
    }
}