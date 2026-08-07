using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

public class SC_typewriter : MonoBehaviour
{
    private bool isTyping = false; // indique si le texte est en train d'être tapé
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

    TMP_TextInfo textInfo;
    Vector3[][] originalVertices;
    Dictionary<int, Vector3> shakeOffsets = new();
    HashSet<int> revealedChars = new();

    List<int> shakeChars = new();
    List<int> waveChars = new();
    List<int> scaleChars = new();
    List<int> colorChars = new();
    List<(int index, float duration)> pauseChars = new();
    float shakeTimer;

    Coroutine typeCoroutine;
    Coroutine animateCoroutine;
    public GameObject wait_input_logo;


    private void OnDisable()
    {
        textMeshPro.text = "";
    }
    public void TriggerText(string text)
    {
        textMeshPro.enabled = true;

        animateCoroutine = StartCoroutine(AnimateText());

        if (isTyping)
        {
            CompleteTyping();
            return;
        }

        // sinon, on lance la frappe normale
        if (typeCoroutine != null)
            StopCoroutine(typeCoroutine);

        fullText = text;
        ParseTags();
        typeCoroutine = StartCoroutine(TypeText());
    }
    IEnumerator TypeText()
    {
        wait_input_logo.SetActive(false);

        isTyping = true;
        int charCount = textInfo.characterCount;

        for (int i = 0; i < charCount; i++)
        {
            foreach (var pause in pauseChars)
            {
                if (pause.index == i)
                {
                    if (audioSource != null)
                        audioSource.Pause();

                    yield return new WaitForSeconds(pause.duration/10);

                    if (audioSource != null)
                        audioSource.UnPause();
                }
            }

            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible) continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[matIndex].colors32;

            // rendre la lettre visible
            for (int j = 0; j < 4; j++) vertexColors[vertIndex + j].a = 255;
            textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            revealedChars.Add(i);
            StartCoroutine(PopLetter(i));

    

            yield return new WaitForSeconds(typeSpeed);
        }

        isTyping = false;
        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
    public void SetWaitInputVisible(bool visible)
    {
        if (wait_input_logo != null)
            wait_input_logo.SetActive(visible);
    }
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

        // Révéler toutes les lettres
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Color32[] colors = textInfo.meshInfo[matIndex].colors32;

            for (int j = 0; j < 4; j++)
                colors[vertIndex + j].a = 255;

            revealedChars.Add(i);
        }

        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
    public void FinishText()
    {
        if (typeCoroutine != null)
        {
            StopCoroutine(typeCoroutine);
            typeCoroutine = null;
        }

        isTyping = false;

        for (int i = 0; i < textInfo.characterCount; i++)
        {
            TMP_CharacterInfo charInfo = textInfo.characterInfo[i];

            if (!charInfo.isVisible)
                continue;

            int matIndex = charInfo.materialReferenceIndex;
            int vertIndex = charInfo.vertexIndex;

            Color32[] colors = textInfo.meshInfo[matIndex].colors32;

            for (int j = 0; j < 4; j++)
                colors[vertIndex + j].a = 255;

            revealedChars.Add(i);
        }

        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        if (audioSource != null && audioSource.isPlaying)
            audioSource.Stop();
    }
    public bool IsTyping()
    {
        return isTyping;
    }
    void ParseTags()
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
            if (raw[i] == '<')
            {
                int end = raw.IndexOf('>', i);
                if (end == -1) continue;

                string tag = raw.Substring(i + 1, end - i - 1);
                if (tag.StartsWith("p="))
                {
                    float time = float.Parse(tag.Replace("p=", ""));
                    pauseChars.Add((visibleIndex, time));

                    i = end;
                    continue;
                }
                if (!tag.StartsWith("/"))
                    activeTags.Push(tag);
                else if (activeTags.Count > 0)
                    activeTags.Pop();

                i = end;
                continue;
            }

            foreach (string tag in activeTags)
            {
                switch (tag)
                {
                    case string s when s.StartsWith("p="):
                        float time = float.Parse(s.Replace("p=", ""));
                        pauseChars.Add((visibleIndex, time));
                        break;
                    case "sh": shakeChars.Add(visibleIndex); break;
                    case "w": waveChars.Add(visibleIndex); break;
                    case "sc": scaleChars.Add(visibleIndex); break;
                    case "c": colorChars.Add(visibleIndex); break;
                }
            }

            clean += raw[i];
            visibleIndex++;
        }

        textMeshPro.text = clean;
        textMeshPro.ForceMeshUpdate();
        textInfo = textMeshPro.textInfo;

        originalVertices = new Vector3[textInfo.meshInfo.Length][];
        for (int i = 0; i < textInfo.meshInfo.Length; i++)
            originalVertices[i] = (Vector3[])textInfo.meshInfo[i].vertices.Clone();

        // rendre toutes les lettres invisibles
        for (int i = 0; i < textInfo.characterCount; i++)
        {
            if (!textInfo.characterInfo[i].isVisible) continue;
            int matIndex = textInfo.characterInfo[i].materialReferenceIndex;
            int vertIndex = textInfo.characterInfo[i].vertexIndex;
            Color32[] vertexColors = textInfo.meshInfo[matIndex].colors32;
            for (int j = 0; j < 4; j++) vertexColors[vertIndex + j].a = 0;
        }
        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

        if (audioSource != null && typeSound != null)
        {
            audioSource.clip = typeSound;
            audioSource.loop = true;
            audioSource.Play();
        }
    }


    IEnumerator PopLetter(int index)
    {
        TMP_TextInfo ti = textMeshPro.textInfo;
        int matIndex = ti.characterInfo[index].materialReferenceIndex;
        int vertIndex = ti.characterInfo[index].vertexIndex;

        Vector3[] verts = ti.meshInfo[matIndex].vertices;
        Vector3[] orig = new Vector3[4];
        for (int j = 0; j < 4; j++) orig[j] = verts[vertIndex + j];

        Vector3 mid = (orig[0] + orig[2]) / 2;

        float elapsed = 0f;
        while (elapsed < popDuration)
        {
            float scale = Mathf.Lerp(popScale, 1f, elapsed / popDuration);
            for (int j = 0; j < 4; j++) verts[vertIndex + j] = (orig[j] - mid) * scale + mid;
            textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int j = 0; j < 4; j++) verts[vertIndex + j] = orig[j];
        textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Vertices);
    }

    IEnumerator AnimateText()
    {
        while (true)
        {
            textMeshPro.ForceMeshUpdate();
            textInfo = textMeshPro.textInfo;

            shakeTimer += Time.deltaTime;
            bool updateShake = shakeTimer >= shakeFrequency;
            if (updateShake) shakeTimer = 0f;

            foreach (int i in revealedChars)
            {
                TMP_CharacterInfo charInfo = textInfo.characterInfo[i];
                int mat = charInfo.materialReferenceIndex;
                int vIndex = charInfo.vertexIndex;
                Vector3[] verts = textInfo.meshInfo[mat].vertices;
                Vector3[] baseVerts = originalVertices[mat];
                Color32[] colors = textInfo.meshInfo[mat].colors32;

                Vector3 mid = (baseVerts[vIndex] + baseVerts[vIndex + 2]) / 2f;
                Vector3 offset = Vector3.zero;

                if (shakeChars.Contains(i))
                {
                    if (updateShake || !shakeOffsets.ContainsKey(i))
                        shakeOffsets[i] = new Vector3(Random.Range(-shakeAmplitude, shakeAmplitude),
                                                      Random.Range(-shakeAmplitude, shakeAmplitude), 0f);
                    offset += shakeOffsets[i];
                }

                if (waveChars.Contains(i))
                    offset.y += Mathf.Sin(Time.time * waveFrequency + i) * waveAmplitude;

                float scale = 1f;
                if (scaleChars.Contains(i))
                    scale += Mathf.Sin(Time.time * scaleFrequency + i) * scaleAmplitude;

                for (int j = 0; j < 4; j++)
                    verts[vIndex + j] = mid + (baseVerts[vIndex + j] - mid) * scale + offset;

                if (colorChars.Contains(i))
                {
                    float t = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;
                    Color32 c = Color.Lerp(baseColor, glowColor, t);
                    for (int j = 0; j < 4; j++) colors[vIndex + j] = c;
                }
                else
                {
                    float t = (Mathf.Sin(Time.time * glowSpeed) + 1f) / 2f;
                    Color32 c = new Color(textMeshPro.color.r, textMeshPro.color.g, textMeshPro.color.b, 255);
                    for (int j = 0; j < 4; j++) colors[vIndex + j] = c;
                }
            }

            for (int i = 0; i < textInfo.meshInfo.Length; i++)
            {
                textInfo.meshInfo[i].mesh.vertices = textInfo.meshInfo[i].vertices;
                textMeshPro.UpdateGeometry(textInfo.meshInfo[i].mesh, i);
            }
            textMeshPro.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);

            yield return null;
        }
    }
}