using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

[RequireComponent(typeof(TMP_Text))]
public class TMPTextEffects : MonoBehaviour
{
    private TMP_Text tmp;
    private TMP_TextInfo textInfo;

    private string cleanText;
    private Dictionary<int, EffectState> charStates = new Dictionary<int, EffectState>();

    private bool initialized;
    void Awake()
    {
        tmp = GetComponent<TMP_Text>();
    }

    void OnEnable()
    {
        Initialize();
        StartCoroutine(Animate());
    }

    void OnDisable()
    {
        StopAllCoroutines();
    }


    void Initialize()
    {
        if (initialized) return;
        initialized = true;

        BuildCleanTextAndStates(tmp.text);

        tmp.text = cleanText;

        tmp.ForceMeshUpdate();
        textInfo = tmp.textInfo;
    }

    IEnumerator Animate()
    {
        while (true)
        {
            // IMPORTANT : ne jamais Forcer la recréation du mesh ici
            textInfo = tmp.textInfo;

            int charCount = textInfo.characterCount;

            for (int i = 0; i < charCount; i++)
            {
                var charInfo = textInfo.characterInfo[i];
                if (!charInfo.isVisible) continue;

                EffectState fx = GetState(i);

                ApplyEffects(i, charInfo, fx);
            }

            ApplyMesh();

            yield return null;
        }
    }

    // ================= EFFECTS =================

    void ApplyEffects(int i, TMP_CharacterInfo charInfo, EffectState fx)
    {
        int matIndex = charInfo.materialReferenceIndex;
        int vertIndex = charInfo.vertexIndex;

        var meshInfo = textInfo.meshInfo[matIndex];
        var vertices = meshInfo.vertices;
        var colors = meshInfo.colors32;

        float time = Time.time;

        Vector3 v0 = vertices[vertIndex + 0];
        Vector3 v1 = vertices[vertIndex + 1];
        Vector3 v2 = vertices[vertIndex + 2];
        Vector3 v3 = vertices[vertIndex + 3];

        Vector3 center = (v0 + v1 + v2 + v3) * 0.25f;

        Vector3 offset = Vector3.zero;

        // 🌊 WAVE
        if (fx.wave)
            offset.y += Mathf.Sin(time * waveFrequency + i * waveDelay) * waveAmplitude / 1000;

        // 📳 SHAKE
        if (fx.shake)
        {
            offset.x += Mathf.Sin(time * shakeFrequency + i) * shakeAmplitude / 1000;
            offset.y += Mathf.Cos(time * shakeFrequency + i) * shakeAmplitude / 1000;
        }

        // 🔁 SCALE
        float scale = fx.scale
            ? 1f + Mathf.Sin(time * scaleFrequency + i * scaleDelay) * scaleAmount / 1000
            : 1f;

        for (int j = 0; j < 4; j++)
        {
            Vector3 orig = vertices[vertIndex + j];
            Vector3 dir = orig - center;
            vertices[vertIndex + j] = center + dir * scale + offset;
        }

        if (fx.glow)
        {
            // Oscillation entre 0 et 1
            float g = (Mathf.Sin(time * glowFrequency + i * glowDelay) + 1f) * 0.5f;

            Color targetColor = glowColor; // deuxième couleur

            Color glow = Color.Lerp(glowColor2, targetColor, g);

            // On garde l'alpha d'origine du texte
            byte originalAlpha = colors[vertIndex].a;
            glow.a = originalAlpha;

            Color32 finalColor = glow;

            colors[vertIndex + 0] = finalColor;
            colors[vertIndex + 1] = finalColor;
            colors[vertIndex + 2] = finalColor;
            colors[vertIndex + 3] = finalColor;
        } 
    }
        void ApplyMesh()
    {
        // 🔥 LA SEULE BONNE FAÇON avec TMP runtime
        tmp.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices | TMP_VertexDataUpdateFlags.Colors32);
    }

    // ================= TAG PARSER =================

    void BuildCleanTextAndStates(string input)
    {
        cleanText = "";
        charStates.Clear();

        Stack<EffectState> stack = new Stack<EffectState>();
        stack.Push(new EffectState());

        int index = 0;

        for (int i = 0; i < input.Length; i++)
        {
            if (input[i] == '<')
            {
                int end = input.IndexOf('>', i);
                if (end == -1) break;

                string tag = input.Substring(i + 1, end - i - 1).ToLower();

                if (tag.StartsWith("/"))
                {
                    if (stack.Count > 1)
                        stack.Pop();
                }
                else
                {
                    EffectState fx = stack.Peek().Clone();
                    ApplyTag(ref fx, tag);
                    stack.Push(fx);
                }

                i = end;
                continue;
            }

            cleanText += input[i];
            charStates[index] = stack.Peek().Clone();
            index++;
        }
    }

    EffectState GetState(int i)
    {
        if (charStates.TryGetValue(i, out EffectState fx))
            return fx;

        return new EffectState();
    }

    void ApplyTag(ref EffectState fx, string tag)
    {
        if (tag == "wave") fx.wave = true;
        else if (tag == "/wave") fx.wave = false;

        else if (tag == "shake") fx.shake = true;
        else if (tag == "/shake") fx.shake = false;

        else if (tag == "scale") fx.scale = true;
        else if (tag == "/scale") fx.scale = false;

        else if (tag == "glow") fx.glow = true;
        else if (tag == "/glow") fx.glow = false;
    }

    // ================= EFFECT STATE =================

    class EffectState
    {
        public bool wave, shake, scale, glow;

        public EffectState Clone()
        {
            return (EffectState)MemberwiseClone();
        }
    }

    // ================= SETTINGS =================

    [Header("WAVE")]
    public float waveAmplitude = 5f;
    public float waveFrequency = 2f;
    public float waveDelay = 0.1f;

    [Header("SHAKE")]
    public float shakeAmplitude = 2f;
    public float shakeFrequency = 20f;

    [Header("SCALE")]
    public float scaleAmount = 0.2f;
    public float scaleFrequency = 2f;
    public float scaleDelay = 0.05f;

    [Header("GLOW")]
    public Color glowColor = Color.cyan;
    public Color glowColor2 = Color.yellow;
    public float glowFrequency = 2f;
    public float glowDelay = 0.05f;
}