
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_star_spawner_level_selection : MonoBehaviour
{
    [Header("Objets à faire apparaître")]
    public List<GameObject> spritePrefabs = new List<GameObject>();

    [Header("Quantité à spawn")]
    [Min(0)]
    public int spawnAmount = 5;

    [Header("Points de destination")]
    public Transform[] points;

    [Header("Aim final")]
    public Transform aim;

    [Header("Vitesses")]
    public float moveSpeed = 5f;
    public float retreatDistance = 1f;
    public float retreatSpeed = 4f;
    public float dashSpeed = 12f;

    [Header("Délais")]
    public float delayBeforeRetreat = 0.2f;
    public float delayBeforeDash = 0.2f;

    [Tooltip("0 = tous les objets dashent en même temps. Sinon, délai entre chaque dash.")]
    public float delayBetweenDashes = 0f;

    [Header("Disparition")]
    public float destroyDistance = 0.15f;

    private readonly List<GameObject> spawnedSprites = new();
    private readonly List<Coroutine> runningCoroutines = new();

    public void SpawnSprites()
    {
        if (spritePrefabs == null || spritePrefabs.Count == 0 ||
            points == null || points.Length == 0 || aim == null)
        {
            Debug.LogWarning("Prefabs, points ou Aim manquant !");
            return;
        }
        spawnAmount = SC_level_selection_screen.instance.world.price;
        // Arrête les anciennes animations.
        foreach (Coroutine coroutine in runningCoroutines)
        {
            if (coroutine != null)
                StopCoroutine(coroutine);
        }

        runningCoroutines.Clear();

        // Détruit les anciens sprites.
        foreach (GameObject sprite in spawnedSprites)
        {
            if (sprite != null)
                Destroy(sprite);
        }

        spawnedSprites.Clear();

        if (spawnAmount <= 0)
            return;

        // Récupère les points valides en conservant leur ordre.
        List<Transform> validPoints = new List<Transform>();

        foreach (Transform point in points)
        {
            if (point != null)
                validPoints.Add(point);
        }

        if (validPoints.Count == 0)
        {
            Debug.LogWarning("Aucun point valide !");
            return;
        }

        // Récupère les prefabs valides.
        List<GameObject> validPrefabs = new List<GameObject>();

        foreach (GameObject prefab in spritePrefabs)
        {
            if (prefab != null)
                validPrefabs.Add(prefab);
        }

        if (validPrefabs.Count == 0)
        {
            Debug.LogWarning("Aucun prefab valide !");
            return;
        }

        int pointCount = validPoints.Count;

        List<GameObject> newSprites = new List<GameObject>();
        List<Transform> targets = new List<Transform>();

        // Crée exactement spawnAmount sprites.
        // Crée exactement spawnAmount sprites,
        // répartis régulièrement sur les points disponibles.
        for (int i = 0; i < spawnAmount; i++)
        {
            int pointIndex;

            if (spawnAmount == 1)
            {
                // Un seul objet : point central.
                pointIndex = (pointCount - 1) / 2;
            }
            else
            {
                // Répartition régulière avec un pas basé sur le nombre
                // de sprites, et non sur le dernier point.
                pointIndex = Mathf.FloorToInt(
                    i * (float)pointCount / spawnAmount
                );
            }

            Transform target = validPoints[pointIndex];

            // Réutilise les prefabs en alternance si nécessaire.
            GameObject prefab = validPrefabs[i % validPrefabs.Count];

            GameObject sprite = Instantiate(
                prefab,
                target.position,
                Quaternion.identity
            );

            newSprites.Add(sprite);
            targets.Add(target);
            spawnedSprites.Add(sprite);
        }
        // Lance les animations une fois tous les objets créés.
        for (int i = 0; i < newSprites.Count; i++)
        {
            Coroutine coroutine = StartCoroutine(
                MoveSprite(newSprites[i], targets[i], i)
            );

            runningCoroutines.Add(coroutine);
        }
    }

    private IEnumerator MoveSprite(
        GameObject sprite,
        Transform target,
        int spriteIndex)
    {
        // 1. Va vers son point assigné.
        yield return MoveTo(sprite, target.position, moveSpeed);

        if (sprite == null)
            yield break;

        yield return new WaitForSeconds(delayBeforeRetreat);

        if (sprite == null || target == null || aim == null)
            yield break;

        // 2. Recule dans la direction opposée au point.
        Vector3 retreatDirection =
            (sprite.transform.position - target.position).normalized;

        if (retreatDirection.sqrMagnitude < 0.001f)
        {
            retreatDirection =
                (sprite.transform.position - aim.position).normalized;
        }

        if (retreatDirection.sqrMagnitude < 0.001f)
            retreatDirection = Vector3.left;

        Vector3 retreatPosition =
            sprite.transform.position +
            retreatDirection * retreatDistance;

        yield return MoveTo(sprite, retreatPosition, retreatSpeed);

        if (sprite == null)
            yield break;

        yield return new WaitForSeconds(delayBeforeDash);

        if (sprite == null || aim == null)
            yield break;

        // 3. Décale éventuellement le départ du dash.
        if (delayBetweenDashes > 0f)
        {
            yield return new WaitForSeconds(
                spriteIndex * delayBetweenDashes
            );
        }

        if (sprite == null || aim == null)
            yield break;

        // 4. Fonce vers l'Aim.
        yield return MoveTo(sprite, aim.position, dashSpeed);

        // 5. Disparaît.
        if (sprite != null)
        {
            spawnedSprites.Remove(sprite);
            Destroy(sprite);
        }
    }

    private IEnumerator MoveTo(
        GameObject sprite,
        Vector3 destination,
        float speed)
    {
        while (sprite != null &&
               Vector3.Distance(
                   sprite.transform.position,
                   destination
               ) > destroyDistance)
        {
            sprite.transform.position = Vector3.MoveTowards(
                sprite.transform.position,
                destination,
                speed * Time.deltaTime
            );

            yield return null;
        }

        if (sprite != null)
            sprite.transform.position = destination;
    }
}
