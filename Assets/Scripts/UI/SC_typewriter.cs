using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class SC_typewriter : MonoBehaviour
{
    public bool isTyping = false;
    public bool finished = false;

    public DialogueManager dialogueManager;

    [Header("Text Settings")]
    [TextArea]
    public string fullText;

    public float typeSpeed = 0.05f;
    public TMP_Text textMeshPro;

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
    private Vector3[][] originalVertices;

    private readonly Dictionary<int, Vector3> shakeOffsets = new();
    private readonly HashSet<int> revealedChars = new();

    private readonly HashSet<int> shakeChars = new();
    private readonly HashSet<int> waveChars = new();
    private readonly HashSet<int> scaleChars = new();
    private readonly HashSet<int> colorChars = new();

    private readonly List<(int index, float duration)> pauseChars = new();
    private readonly List<Coroutine> popCoroutines = new();

    private float shakeTimer;

    private Coroutine typeCoroutine;
    private Coroutine animateCoroutine;

    private int textVersion = 0;

    // Empêche les événements de fin de ligne
    // d'être appelés plusieurs fois pour le même texte.
    private bool endEventInvoked = false;

    // Empêche l'événement de fin du dialogue entier
    // d'être appelé plusieurs fois.
    private bool dialogueFinishedEventInvoked = false;

    private void Awake()
    {
        if (animateCoroutine == null)
            animateCoroutine = StartCoroutine(AnimateText());
    }

    private void Start()
    {
        if (dialogueManager == null)
            dialogueManager = DialogueManager.instance;
    }

    private void OnDisable()
    {
        textVersion++;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        StopAllPopCoroutines();

        if (audioSource != null &&
            audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isTyping = false;
        finished = true;
    }

    // =========================================================
    // TRIGGER TEXT
    // =========================================================

    public void TriggerText(string text)
    {
        if (dialogueManager == null)
            dialogueManager = DialogueManager.instance;

        if (wait_input_logo != null)
            wait_input_logo.SetActive(false);

        textMeshPro.enabled = true;

        textVersion++;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        StopAllPopCoroutines();

        if (audioSource != null &&
            audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        isTyping = false;
        finished = false;

        // Reset de l'événement de fin de cette ligne.
        endEventInvoked = false;

        // IMPORTANT :
        // On ne reset PAS dialogueFinishedEventInvoked ici.
        // Il doit rester actif pendant toute la conversation.
        //
        // Il sera reset uniquement quand on commencera
        // une nouvelle conversation complète.

        fullText = text;

        ParseTags();

        int currentVersion = textVersion;

        // Event au début de la ligne.
        if (dialogueManager != null &&
            dialogueManager.npc != null)
        {
            dialogueManager.npc.TriggerDialogueEvents(
                dialogueManager.currentLineIndex,
                true
            );
        }

        typeCoroutine =
            StartCoroutine(
                TypeText(currentVersion)
            );
    }

    // =========================================================
    // TYPEWRITER
    // =========================================================

    private IEnumerator TypeText(int version)
    {
        if (wait_input_logo != null)
            wait_input_logo.SetActive(false);

        isTyping = true;
        finished = false;

        int charCount =
            textInfo.characterCount;

        for (int i = 0;
             i < charCount;
             i++)
        {
            if (version != textVersion)
                yield break;

            foreach (var pause in pauseChars)
            {
                if (pause.index != i)
                    continue;

                if (audioSource != null)
                    audioSource.Pause();

                yield return new WaitForSeconds(
                    pause.duration / 10f
                );

                if (version != textVersion)
                    yield break;

                if (audioSource != null)
                    audioSource.UnPause();
            }

            if (version != textVersion)
                yield break;

            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex =
                charInfo.materialReferenceIndex;

            int vertIndex =
                charInfo.vertexIndex;

            Color32[] vertexColors =
                textInfo.meshInfo[matIndex].colors32;

            for (int j = 0; j < 4; j++)
            {
                vertexColors[
                    vertIndex + j
                ].a = 255;
            }

            revealedChars.Add(i);

            if (shakeChars.Contains(i))
                InitShakeOffset(i);

            textMeshPro.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Colors32
            );

            Coroutine popCoroutine =
                StartCoroutine(
                    PopLetter(i, version)
                );

            popCoroutines.Add(popCoroutine);

            yield return new WaitForSeconds(
                typeSpeed
            );
        }

        if (version != textVersion)
            yield break;

        isTyping = false;
        finished = true;

        typeCoroutine = null;

        // Fin de la ligne.
        InvokeEndEventOnce();

        // Vérifie si c'est la fin du dialogue entier.

        if (audioSource != null &&
            audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // =========================================================
    // DIALOGUE EVENTS
    // =========================================================

    private void InvokeEndEventOnce()
    {
        if (endEventInvoked)
            return;

        endEventInvoked = true;

        if (dialogueManager == null)
            dialogueManager = DialogueManager.instance;

        if (dialogueManager == null ||
            dialogueManager.npc == null)
        {
            return;
        }

        // Event de fin de ligne.
        dialogueManager.npc.TriggerDialogueEvents(
            dialogueManager.currentLineIndex,
            false
        );
    }

    // =========================================================
    // FIN DU DIALOGUE ENTIER
    // =========================================================

    public void InvokeDialogueFinishedIfNeeded()
    {

        if (dialogueManager == null)
            dialogueManager = DialogueManager.instance;

        if (dialogueManager == null ||
            dialogueManager.npc == null)
        {
            return;
        }


        dialogueManager.npc.CheckDialogueFinished();
    }

    // =========================================================
    // SHAKE INITIALIZATION
    // =========================================================

    private void InitShakeOffset(int index)
    {
        if (shakeOffsets.ContainsKey(index))
            return;

        shakeOffsets[index] = new Vector3(
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

        textVersion++;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        StopAllPopCoroutines();

        isTyping = false;
        finished = true;

        RevealAllCharacters();

        // Fin de la ligne.
        InvokeEndEventOnce();

        // Vérifie également la fin du dialogue entier.

        if (audioSource != null &&
            audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // =========================================================
    // FINISH TEXT
    // =========================================================

    public void FinishText()
    {
        textVersion++;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        StopAllPopCoroutines();

        isTyping = false;
        finished = true;

        RevealAllCharacters();

        // Fin de la ligne.
        InvokeEndEventOnce();

        // Vérifie également la fin du dialogue entier.
 
        if (audioSource != null &&
            audioSource.isPlaying)
        {
            audioSource.Stop();
        }
    }

    // =========================================================
    // STOP POP COROUTINES
    // =========================================================

    private void StopAllPopCoroutines()
    {
        for (int i = 0;
             i < popCoroutines.Count;
             i++)
        {
            if (popCoroutines[i] != null)
                StopCoroutine(
                    popCoroutines[i]
                );
        }

        popCoroutines.Clear();
    }

    // =========================================================
    // REVEAL ALL CHARACTERS
    // =========================================================

    private void RevealAllCharacters()
    {
        if (textInfo == null)
            return;

        for (int i = 0;
             i < textInfo.characterCount;
             i++)
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex =
                charInfo.materialReferenceIndex;

            int vertIndex =
                charInfo.vertexIndex;

            Color32[] colors =
                textInfo.meshInfo[matIndex].colors32;

            for (int j = 0; j < 4; j++)
            {
                colors[
                    vertIndex + j
                ].a = 255;
            }

            revealedChars.Add(i);

            if (shakeChars.Contains(i))
                InitShakeOffset(i);
        }

        textMeshPro.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );
    }

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

        Stack<string> activeTags =
            new Stack<string>();

        int visibleIndex = 0;

        for (int i = 0;
             i < raw.Length;
             i++)
        {
            if (raw[i] == '<')
            {
                int end =
                    raw.IndexOf('>', i);

                if (end == -1)
                {
                    clean += raw[i];
                    visibleIndex++;
                    continue;
                }

                string tag =
                    raw.Substring(
                        i + 1,
                        end - i - 1
                    );

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

                if (!tag.StartsWith("/"))
                {
                    activeTags.Push(tag);
                }
                else if (activeTags.Count > 0)
                {
                    activeTags.Pop();
                }

                i = end;
                continue;
            }

            foreach (string tag in activeTags)
            {
                switch (tag)
                {
                    case "sh":
                        shakeChars.Add(
                            visibleIndex
                        );
                        break;

                    case "w":
                        waveChars.Add(
                            visibleIndex
                        );
                        break;

                    case "sc":
                        scaleChars.Add(
                            visibleIndex
                        );
                        break;

                    case "c":
                        colorChars.Add(
                            visibleIndex
                        );
                        break;
                }
            }

            clean += raw[i];
            visibleIndex++;
        }

        textMeshPro.text = clean;

        textMeshPro.ForceMeshUpdate();

        textInfo = textMeshPro.textInfo;

        originalVertices =
            new Vector3[
                textInfo.meshInfo.Length
            ][];

        for (int i = 0;
             i < textInfo.meshInfo.Length;
             i++)
        {
            originalVertices[i] =
                (Vector3[])textInfo
                    .meshInfo[i]
                    .vertices
                    .Clone();
        }

        for (int i = 0;
             i < textInfo.characterCount;
             i++)
        {
            TMP_CharacterInfo charInfo =
                textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex =
                charInfo.materialReferenceIndex;

            int vertIndex =
                charInfo.vertexIndex;

            Color32[] vertexColors =
                textInfo.meshInfo[
                    matIndex
                ].colors32;

            for (int j = 0; j < 4; j++)
            {
                vertexColors[
                    vertIndex + j
                ].a = 0;
            }
        }

        textMeshPro.UpdateVertexData(
            TMP_VertexDataUpdateFlags.Colors32
        );

        if (audioSource != null &&
            typeSound != null)
        {
            audioSource.clip = typeSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }

    // =========================================================
    // POP LETTER
    // =========================================================

    private IEnumerator PopLetter(
        int index,
        int version)
    {
        if (version != textVersion ||
            textInfo == null)
        {
            yield break;
        }

        if (index >= textInfo.characterCount)
            yield break;

        TMP_CharacterInfo charInfo =
            textInfo.characterInfo[index];

        if (!charInfo.isVisible)
            yield break;

        int matIndex =
            charInfo.materialReferenceIndex;

        int vertIndex =
            charInfo.vertexIndex;

        Vector3[] verts =
            textInfo.meshInfo[
                matIndex
            ].vertices;

        Vector3[] original =
            new Vector3[4];

        for (int j = 0; j < 4; j++)
        {
            original[j] =
                verts[vertIndex + j];
        }

        Vector3 mid =
            (original[0] +
             original[2]) / 2f;

        float elapsed = 0f;

        while (elapsed < popDuration)
        {
            if (version != textVersion)
                yield break;

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
                    (original[j] - mid) *
                    scale + mid;
            }

            textMeshPro.UpdateVertexData(
                TMP_VertexDataUpdateFlags.Vertices
            );

            elapsed += Time.deltaTime;

            yield return null;
        }

        if (version != textVersion)
            yield break;

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
                    textInfo.meshInfo[
                        matIndex
                    ].vertices;

                Vector3[] baseVerts =
                    originalVertices[
                        matIndex
                    ];

                Color32[] colors =
                    textInfo.meshInfo[
                        matIndex
                    ].colors32;

                Vector3 mid =
                    (
                        baseVerts[vertIndex] +
                        baseVerts[vertIndex + 2]
                    ) / 2f;

                Vector3 offset =
                    Vector3.zero;

                if (shakeChars.Contains(i))
                {
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

                if (waveChars.Contains(i))
                {
                    offset.y +=
                        Mathf.Sin(
                            Time.time *
                            waveFrequency + i
                        ) *
                        waveAmplitude;
                }

                float scale = 1f;

                if (scaleChars.Contains(i))
                {
                    scale +=
                        Mathf.Sin(
                            Time.time *
                            scaleFrequency + i
                        ) *
                        scaleAmplitude;
                }

                for (int j = 0; j < 4; j++)
                {
                    verts[vertIndex + j] =
                        mid +
                        (
                            baseVerts[
                                vertIndex + j
                            ] - mid
                        ) *
                        scale +
                        offset;
                }

                Color32 c;

                if (colorChars.Contains(i))
                {
                    float t =
                        (
                            Mathf.Sin(
                                Time.time *
                                glowSpeed
                            ) + 1f
                        ) / 2f;

                    c = Color.Lerp(
                        baseColor,
                        glowColor,
                        t
                    );
                }
                else
                {
                    c = new Color32(
                        (byte)(
                            textMeshPro.color.r *
                            255f
                        ),
                        (byte)(
                            textMeshPro.color.g *
                            255f
                        ),
                        (byte)(
                            textMeshPro.color.b *
                            255f
                        ),
                        255
                    );
                }

                for (int j = 0; j < 4; j++)
                {
                    colors[
                        vertIndex + j
                    ] = c;
                }
            }

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

    // =========================================================
    // CLEAR TEXT
    // =========================================================

    public void clear_text()
    {
        textVersion++;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        StopAllPopCoroutines();

        isTyping = false;
        finished = true;

        endEventInvoked = true;
        dialogueFinishedEventInvoked = true;

        if (audioSource != null &&
            audioSource.isPlaying)
        {
            audioSource.Stop();
        }

        fullText = string.Empty;

        if (textMeshPro != null)
            textMeshPro.text = string.Empty;
    }
    public void TriggerTextInstant(string text)
    {
        if (dialogueManager == null)
            dialogueManager = DialogueManager.instance;

        if (wait_input_logo != null)
            wait_input_logo.SetActive(false);

        if (textMeshPro == null)
            return;
        fullText = text;
        textMeshPro.enabled = true;

        textVersion++;

        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        StopAllPopCoroutines();
        audioSource.Stop();

        isTyping = false;
        finished = true;

        endEventInvoked = false;

        fullText = text;

        ParseTags();

        // Révèle immédiatement tous les caractères.
        RevealAllCharacters();

        // Événement de début de ligne.
        if (dialogueManager != null &&
            dialogueManager.npc != null)
        {
            dialogueManager.npc.TriggerDialogueEvents(
                dialogueManager.currentLineIndex,
                true
            );
        }

        // Événement de fin de ligne.
        InvokeEndEventOnce();
        audioSource.Stop();
    }

}