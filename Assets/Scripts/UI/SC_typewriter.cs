using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SC_typewriter : MonoBehaviour
{
    private bool isTyping = false;

    [Header("Text Settings")]
    public TMP_Text textMeshPro;

    [TextArea]
    private string fullText;

    public float typeSpeed = 0.05f;

    [Header("Shake (Movement)")]
    public float shakeAmplitude = 5f;
    public float shakeFrequency = 0.05f;

    [Header("Wave")]
    public float waveAmplitude = 5f;
    public float waveFrequency = 5f;

    [Header("Scale")]
    public float scaleAmplitude = 0.2f;
    public float scaleFrequency = 5f;

    [Header("Color")]
    public Color baseColor = Color.white;
    public Color glowColor = Color.yellow;
    public float glowSpeed = 5f;

    [Header("Pop Settings")]
    public float popScale = 1.5f;
    public float popDuration = 0.1f;

    [Header("Sound Settings")]
    public AudioSource audioSource;
    public AudioClip typeSound;

    [Header("UI")]
    public GameObject wait_input_logo;

    private TMP_TextInfo textInfo;

    // Vertices originaux du texte
    private Vector3[][] originalVertices;

    // Offset actuel de chaque lettre qui utilise le shake
    private Dictionary<int, Vector3> shakeOffsets = new();

    // Lettres déjà révélées
    private HashSet<int> revealedChars = new();

    // Lettres affectées par chaque effet
    private HashSet<int> shakeChars = new();
    private HashSet<int> waveChars = new();
    private HashSet<int> scaleChars = new();
    private HashSet<int> colorChars = new();

    // Pauses
    private List<(int index, float duration)> pauseChars = new();

    private float shakeTimer;

    private Coroutine typeCoroutine;
    private Coroutine animateCoroutine;

    private void Awake()
    {
        // On lance UNE SEULE fois l'animation permanente.
        animateCoroutine = StartCoroutine(AnimateText());
    }

    private void OnDisable()
    {
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        textMeshPro.text = "";
        isTyping = false;
    }

    // =========================================================
    // TRIGGER TEXT
    // =========================================================

    public void TriggerText(string text)
    {
        textMeshPro.enabled = true;

        // Si une frappe est déjà en cours,
        // on termine immédiatement le texte actuel.
        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        fullText = text;

        ParseTags();

        typeCoroutine = StartCoroutine(TypeText());
    }

    // =========================================================
    // TYPEWRITER
    // =========================================================

    private IEnumerator TypeText()
    {
        if (wait_input_logo != null)
            wait_input_logo.SetActive(false);

        isTyping = true;

        int charCount = textInfo.characterCount;

        for (int i = 0; i < charCount; i++)
        {
            // -------------------------------------------------
            // PAUSE
            // -------------------------------------------------

            foreach (var pause in pauseChars)
            {
                if (pause.index == i)
                {
                    if (audioSource != null)
                        audioSource.Pause();

                    yield return new WaitForSeconds(pause.duration / 10f);

                    if (audioSource != null)
                        audioSource.UnPause();
                }
            }

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Color32[] vertexColors =
                textInfo.meshInfo[matIndex].colors32;

            // -------------------------------------------------
            // REVEAL LETTER
            // -------------------------------------------------

            for (int j = 0; j < 4; j++)
                vertexColors[vertIndex + j].a = 255;

            revealedChars.Add(i);

            // Initialise le shake AVANT que l'animation
            // commence pour cette lettre.
            if (shakeChars.Contains(i))
            {
                InitShakeOffset(i);
            }

            textMeshPro.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Colors32
            );

            // -------------------------------------------------
            // POP
            // -------------------------------------------------

            StartCoroutine(PopLetter(i));

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        typeCoroutine = null;

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    // =========================================================
    // SHAKE INITIALIZATION
    // =========================================================

    private void InitShakeOffset(int index)
    {
        if (shakeOffsets.ContainsKey(index))
            return;

        shakeOffsets[index] = new Vector3(
            Random.Range(-shakeAmplitude, shakeAmplitude),
            Random.Range(-shakeAmplitude, shakeAmplitude),
            0f
        );
    }

    // =========================================================
    // WAIT INPUT
    // =========================================================

    public void SetWaitInputVisible(bool visible)
    {
        if (wait_input_logo != null)
            wait_input_logo.SetActive(visible);
    }

    // =========================================================
    // COMPLETE TYPING
    // =========================================================

    public void CompleteTyping()
    {
        if (!isTyping)
            return;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        isTyping = false;

        RevealAllCharacters();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    // =========================================================
    // FINISH TEXT
    // =========================================================

    public void FinishText()
    {
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        isTyping = false;

        RevealAllCharacters();

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }

    // =========================================================
    // REVEAL ALL
    // =========================================================

    private void RevealAllCharacters()
    {
        if (textInfo == null)
            return;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Color32[] colors =
                textInfo.meshInfo[matIndex].colors32;

            for (int j = 0; j < 4; j++)
                colors[vertIndex + j].a = 255;

            revealedChars.Add(i);

            // Prépare également les shakes
            if (shakeChars.Contains(i))
                InitShakeOffset(i);
        }

        textMeshPro.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );
    }

    // =========================================================
    // IS TYPING
    // =========================================================

    public bool IsTyping()
    {
        return isTyping;
    }

    // =========================================================
    // PARSE TAGS
    // =========================================================

    private void ParseTags()
    {
        shakeChars.Clear();
        waveChars.Clear();
        scaleChars.Clear();
        colorChars.Clear();

        pauseChars.Clear();

        revealedChars.Clear();
        shakeOffsets.Clear();

        string raw = fullText;
        string clean = "";

        Stack<string> activeTags = new();

        int visibleIndex = 0;

        for (int i = 0; i < raw.Length; i++)
        {
            // -------------------------------------------------
            // TAG
            // -------------------------------------------------

            if (raw[i] == '<')
            {
                int end = raw.IndexOf('>', i);

                if (end == -1)
                    continue;

                string tag =
                    raw.Substring(i + 1, end - i - 1);

                // -------------------------------------------------
                // PAUSE
                // -------------------------------------------------

                if (tag.StartsWith("p="))
                {
                    if (float.TryParse(
                        tag.Replace("p=", ""),
                        out float time))
                    {
                        pauseChars.Add(
                            (visibleIndex, time)
                        );
                    }

                    i = end;
                    continue;
                }

                // -------------------------------------------------
                // OPEN TAG
                // -------------------------------------------------

                if (!tag.StartsWith("/"))
                {
                    activeTags.Push(tag);
                }

                // -------------------------------------------------
                // CLOSE TAG
                // -------------------------------------------------

                else if (activeTags.Count > 0)
                {
                    activeTags.Pop();
                }

                i = end;
                continue;
            }

            // -------------------------------------------------
            // APPLY ACTIVE TAGS
            // -------------------------------------------------

            foreach (string tag in activeTags)
            {
                switch (tag)
                {
                    case "sh":
                        shakeChars.Add(visibleIndex);
                        break;

                    case "w":
                        waveChars.Add(visibleIndex);
                        break;

                    case "sc":
                        scaleChars.Add(visibleIndex);
                        break;

                    case "c":
                        colorChars.Add(visibleIndex);
                        break;

                    default:

                        if (tag.StartsWith("p="))
                        {
                            if (float.TryParse(
                                tag.Replace("p=", ""),
                                out float time))
                            {
                                pauseChars.Add(
                                    (visibleIndex, time)
                                );
                            }
                        }

                        break;
                }
            }

            // -------------------------------------------------
            // ADD CHARACTER
            // -------------------------------------------------

            clean += raw[i];
            visibleIndex++;
        }

        // -------------------------------------------------
        // CREATE TMP TEXT
        // -------------------------------------------------

        textMeshPro.text = clean;

        textMeshPro.ForceMeshUpdate();

        textInfo = textMeshPro.textInfo;

        // -------------------------------------------------
        // SAVE ORIGINAL VERTICES
        // -------------------------------------------------

        originalVertices =
            new Vector3[textInfo.meshInfo.Length][];

        for (int i = 0; i < textInfo.meshInfo.Length; i++)
        {
            originalVertices[i] =
                (Vector3[])textInfo.meshInfo[i].vertices.Clone();
        }

        // -------------------------------------------------
        // HIDE ALL CHARACTERS
        // -------------------------------------------------

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Color32[] vertexColors =
                textInfo.meshInfo[matIndex].colors32;

            for (int j = 0; j < 4; j++)
            {
                vertexColors[vertIndex + j].a = 0;
            }
        }

        textMeshPro.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );

        // -------------------------------------------------
        // SOUND
        // -------------------------------------------------

        if (audioSource != null && typeSound != null)
        {
            audioSource.clip = typeSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // =========================================================
    // POP LETTER
    // =========================================================

    private IEnumerator PopLetter(int index)
    {
        if (textInfo == null)
            yield break;

        if (index >= textInfo.characterCount)
            yield break;

        TMP_CharacterInfo charInfo =
            textInfo.characterInfo[index];

        if (!charInfo.isVisible)
            yield break;

        int matIndex = charInfo.materialReferenceIndex;
        int vertIndex = charInfo.vertexIndex;

        Vector3[] verts =
            textInfo.meshInfo[matIndex].vertices;

        Vector3[] original = new Vector3[4];

        for (int j = 0; j < 4; j++)
        {
            original[j] =
                verts[vertIndex + j];
        }

        Vector3 mid =
            (original[0] + original[2]) / 2f;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            float t =
                elapsed / popDuration;

            float scale =
                Mathf.Lerp(
                    popScale,
                    1f,
                    t
                );

            for (int j = 0; j < 4; j++)
            {
                verts[vertIndex + j] =
                    (original[j] - mid) * scale + mid;
            }

            textMeshPro.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        // -------------------------------------------------
        // RESET
        // -------------------------------------------------

        for (int j = 0; j < 4; j++)
        {
            verts[vertIndex + j] =
                original[j];
        }

        textMeshPro.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Vertices
        );
    }

    // =========================================================
    // ANIMATE TEXT
    // =========================================================

    private IEnumerator AnimateText()
    {
        while (true)
        {
            // IMPORTANT :
            // On ne fait PAS ForceMeshUpdate() ici.
            // Le mesh n'a pas besoin d'être reconstruit
            // à chaque frame.

            if (textMeshPro == null ||
                !textMeshPro.enabled ||
                textInfo == null ||
                originalVertices == null)
            {
                yield return null;
                continue;
            }

            shakeTimer += Time.deltaTime;

            bool updateShake =
                shakeTimer >= shakeFrequency;

            if (updateShake)
                shakeTimer = 0f;

            // -------------------------------------------------
            // ANIMATION DES LETTRES REVEALED
            // -------------------------------------------------

            foreach (int i in revealedChars)
            {
                if (i >= textInfo.characterCount)
                    continue;

                TMP_CharacterInfo charInfo =
                    textInfo.characterInfo[i];

                if (!charInfo.isVisible)
                    continue;

                int matIndex =
                    charInfo.materialReferenceIndex;

                int vertIndex =
                    charInfo.vertexIndex;

                Vector3[] verts =
                    textInfo.meshInfo[matIndex].vertices;

                Vector3[] baseVerts =
                    originalVertices[matIndex];

                Color32[] colors =
                    textInfo.meshInfo[matIndex].colors32;

                Vector3 mid =
                    (baseVerts[vertIndex] +
                     baseVerts[vertIndex + 2]) / 2f;

                Vector3 offset =
                    Vector3.zero;

                // -------------------------------------------------
                // SHAKE
                // -------------------------------------------------

                if (shakeChars.Contains(i))
                {
                    // L'offset existe déjà normalement.
                    // On ne fait donc pas de Random au premier frame.

                    if (!shakeOffsets.ContainsKey(i))
                        InitShakeOffset(i);

                    if (updateShake)
                    {
                        shakeOffsets[i] =
                            new Vector3(
                                Random.Range(
                                    -shakeAmplitude,
                                    shakeAmplitude
                                ),
                                Random.Range(
                                    -shakeAmplitude,
                                    shakeAmplitude
                                ),
                                0f
                            );
                    }

                    offset += shakeOffsets[i];
                }

                // -------------------------------------------------
                // WAVE
                // -------------------------------------------------

                if (waveChars.Contains(i))
                {
                    offset.y +=
                        Mathf.Sin(
                            Time.time * waveFrequency + i
                        ) * waveAmplitude;
                }

                // -------------------------------------------------
                // SCALE
                // -------------------------------------------------

                float scale = 1f;

                if (scaleChars.Contains(i))
                {
                    scale +=
                        Mathf.Sin(
                            Time.time * scaleFrequency + i
                        ) * scaleAmplitude;
                }

                // -------------------------------------------------
                // APPLY POSITION + SCALE
                // -------------------------------------------------

                for (int j = 0; j < 4; j++)
                {
                    verts[vertIndex + j] =
                        mid +
                        (baseVerts[vertIndex + j] - mid)
                        * scale
                        + offset;
                }

                // -------------------------------------------------
                // COLOR
                // -------------------------------------------------

                if (colorChars.Contains(i))
                {
                    float t =
                        (Mathf.Sin(
                            Time.time * glowSpeed
                        ) + 1f) / 2f;

                    Color32 c =
                        Color.Lerp(
                            baseColor,
                            glowColor,
                            t
                        );

                    for (int j = 0; j < 4; j++)
                    {
                        colors[vertIndex + j] = c;
                    }
                }
                else
                {
                    Color32 c =
                        new Color32(
                            (byte)(textMeshPro.color.r * 255f),
                            (byte)(textMeshPro.color.g * 255f),
                            (byte)(textMeshPro.color.b * 255f),
                            255
                        );

                    for (int j = 0; j < 4; j++)
                    {
                        colors[vertIndex + j] = c;
                    }
                }
            }

            // -------------------------------------------------
            // UPDATE MESH
            // -------------------------------------------------

            for (int i = 0;
                 i < textInfo.meshInfo.Length;
                 i++)
            {
                textInfo.meshInfo[i].mesh.vertices =
                    textInfo.meshInfo[i].vertices;

                textMeshPro.UpdateGeometry(
                    textInfo.meshInfo[i].mesh,
                    i
                );
            }

            textMeshPro.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Colors32
            );

            yield return null;
        }
    }
}
