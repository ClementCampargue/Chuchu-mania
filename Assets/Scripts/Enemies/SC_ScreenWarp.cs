
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(100)]
public class SC_ScreenWarp : MonoBehaviour
{
    private Vector2 limit;

    [Header("Warning")]
    public float warningDistance = 2f;
    public float warningSpawnOffset = 2f;
    public GameObject warningPrefab;

    private bool hasSpawnedWarning;

    // ============================================================
    // CLONE PHYSIQUE
    // ============================================================

    [Header("Clone physique")]

    [SerializeField]
    private GameObject wrapClonePrefab;

    [SerializeField]
    private bool enablePhysicalClone = true;

    [SerializeField]
    private bool synchronizeAnimator = true;

    private GameObject wrapClone;

    private Transform cloneTransform;

    private Animator[] originalAnimators;
    private Animator[] cloneAnimators;

    private Collider2D[] originalColliders;
    private Collider2D[] cloneColliders;

    private bool cloneInitialized;

    // ============================================================
    // LIMITES
    // ============================================================

    // ============================================================
    // VISIBILITE DU CLONE
    // ============================================================

    /// <summary>
    /// Rend le clone invisible sans désactiver ses Collider2D.
    /// </summary>
    public void HideClone()
    {
        if (wrapClone == null)
            return;

        SpriteRenderer[] renderers =
            wrapClone.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.enabled = false;
        }
    }

    /// <summary>
    /// Rend le clone de nouveau visible.
    /// </summary>
    public void ShowClone()
    {
        if (wrapClone == null)
            return;

        SpriteRenderer[] renderers =
            wrapClone.GetComponentsInChildren<SpriteRenderer>(true);

        foreach (SpriteRenderer renderer in renderers)
        {
            renderer.enabled = true;
        }
    }

    public static float LeftLimit
    {
        get
        {
            return SC_level_master.instance != null
                ? SC_level_master.instance.limits.x
                : -10f;
        }
    }

    public static float RightLimit
    {
        get
        {
            return SC_level_master.instance != null
                ? SC_level_master.instance.limits.y
                : 10f;
        }
    }

    public static float Width =>
        Mathf.Max(0.001f, RightLimit - LeftLimit);

    // ============================================================
    // SCREEN WARP MATHEMATIQUE
    // ============================================================

    public static Vector3 NearestImage(
        Vector3 from,
        Vector3 target)
    {
        float width = Width;

        float dx = target.x - from.x;

        if (dx > width * 0.5f)
            target.x -= width;
        else if (dx < -width * 0.5f)
            target.x += width;

        return target;
    }

    public static Vector3 WrappedDelta(
        Vector3 from,
        Vector3 to)
    {
        return NearestImage(from, to) - from;
    }

    public static float WrappedDistance(
        Vector3 a,
        Vector3 b)
    {
        return WrappedDelta(a, b).magnitude;
    }

    public static Vector3 MoveTowards(
        Vector3 from,
        Vector3 to,
        float maxDistance)
    {
        return Vector3.MoveTowards(
            from,
            NearestImage(from, to),
            maxDistance
        );
    }

    // ============================================================
    // START
    // ============================================================

    private void Start()
    {
        if (SC_level_master.instance == null)
        {
            Debug.LogError(
                "SC_ScreenWarp : SC_level_master.instance introuvable.",
                this
            );

            enabled = false;
            return;
        }

        limit = SC_level_master.instance.limits;

        originalAnimators =
            GetComponentsInChildren<Animator>(true);

        originalColliders =
            GetComponentsInChildren<Collider2D>(true);

        InitializeClone();
    }

    // ============================================================
    // INITIALISATION DU CLONE
    // ============================================================

    private void InitializeClone()
    {
        if (!enablePhysicalClone || wrapClonePrefab == null)
            return;

        wrapClone = Instantiate(wrapClonePrefab);

        wrapClone.name = gameObject.name + "_WrapClone";

        cloneTransform = wrapClone.transform;

        // Le clone ne doit pas être contrôlé par une IA.
        // Le prefab doit contenir uniquement les éléments
        // graphiques et physiques nécessaires.

        cloneAnimators =
            wrapClone.GetComponentsInChildren<Animator>(true);

        cloneColliders =
            wrapClone.GetComponentsInChildren<Collider2D>(true);

        // Rigidbody2D cinématique.
        Rigidbody2D[] bodies =
            wrapClone.GetComponentsInChildren<Rigidbody2D>(true);

        foreach (Rigidbody2D body in bodies)
        {
            body.bodyType = RigidbodyType2D.Kinematic;
            body.gravityScale = 0f;
            body.constraints = RigidbodyConstraints2D.FreezeAll;
            body.useFullKinematicContacts = true;
        }

        // Empêcher le garde de se collisionner lui-même.
        foreach (Collider2D original in originalColliders)
        {
            if (original == null)
                continue;

            foreach (Collider2D clone in cloneColliders)
            {
                if (clone == null)
                    continue;

                Physics2D.IgnoreCollision(
                    original,
                    clone,
                    true
                );
            }
        }

        wrapClone.SetActive(false);

        cloneInitialized = true;
    }

    // ============================================================
    // UPDATE
    // ============================================================

    private void Update()
    {
        if (SC_level_master.instance != null)
            limit = SC_level_master.instance.limits;

        HandleWarning();

        HandleScreenWrap();
    }

    private void LateUpdate()
    {
        if (!cloneInitialized)
            return;

        UpdatePhysicalClone();
    }

    // ============================================================
    // DETECTION DU BORD DE L'ECRAN
    // ============================================================

    private void UpdatePhysicalClone()
    {
        if (wrapClone == null)
            return;

        float width = Width;

        Bounds bounds = GetOriginalBounds();

        bool crossingRight = bounds.max.x > limit.y;
        bool crossingLeft = bounds.min.x < limit.x;

        if (!crossingRight && !crossingLeft)
        {
            if (wrapClone.activeSelf)
                wrapClone.SetActive(false);

            return;
        }

        float offset = crossingRight ? -width : width;

        Vector3 clonePosition =
            transform.position + Vector3.right * offset;

        // Synchronisation de la transformation.
        cloneTransform.position = clonePosition;
        cloneTransform.rotation = transform.rotation;
        cloneTransform.localScale = transform.localScale;

        // Synchronisation des animations.
        if (synchronizeAnimator)
            SynchronizeAnimations();

        if (!wrapClone.activeSelf)
            wrapClone.SetActive(true);

        // Actualiser les transformations physiques.
        Physics2D.SyncTransforms();
    }

    // ============================================================
    // BOUNDS DU GARDE
    // ============================================================

    private Bounds GetOriginalBounds()
    {
        bool initialized = false;

        Bounds bounds = new Bounds(
            transform.position,
            Vector3.zero
        );

        foreach (Collider2D collider in originalColliders)
        {
            if (collider == null || !collider.enabled)
                continue;

            if (!collider.gameObject.activeInHierarchy)
                continue;

            if (!initialized)
            {
                bounds = collider.bounds;
                initialized = true;
            }
            else
            {
                bounds.Encapsulate(collider.bounds);
            }
        }

        if (!initialized)
        {
            Renderer[] renderers =
                GetComponentsInChildren<Renderer>(true);

            foreach (Renderer renderer in renderers)
            {
                if (renderer == null || !renderer.enabled)
                    continue;

                if (!initialized)
                {
                    bounds = renderer.bounds;
                    initialized = true;
                }
                else
                {
                    bounds.Encapsulate(renderer.bounds);
                }
            }
        }

        return bounds;
    }

    // ============================================================
    // SYNCHRONISATION DES ANIMATIONS
    // ============================================================

    private void SynchronizeAnimations()
    {
        if (originalAnimators == null ||
            cloneAnimators == null)
            return;

        int count = Mathf.Min(
            originalAnimators.Length,
            cloneAnimators.Length
        );

        for (int i = 0; i < count; i++)
        {
            Animator original = originalAnimators[i];
            Animator clone = cloneAnimators[i];

            if (original == null || clone == null)
                continue;

            if (!original.isActiveAndEnabled)
                continue;

            if (!clone.gameObject.activeInHierarchy)
                continue;

            foreach (AnimatorControllerParameter parameter
                     in original.parameters)
            {
                switch (parameter.type)
                {
                    case AnimatorControllerParameterType.Bool:

                        clone.SetBool(
                            parameter.nameHash,
                            original.GetBool(parameter.nameHash)
                        );

                        break;

                    case AnimatorControllerParameterType.Float:

                        clone.SetFloat(
                            parameter.nameHash,
                            original.GetFloat(parameter.nameHash)
                        );

                        break;

                    case AnimatorControllerParameterType.Int:

                        clone.SetInteger(
                            parameter.nameHash,
                            original.GetInteger(parameter.nameHash)
                        );

                        break;
                }
            }

            // Reproduire également l'état d'animation courant.
            int layers = Mathf.Min(
                original.layerCount,
                clone.layerCount
            );

            for (int layer = 0; layer < layers; layer++)
            {
                AnimatorStateInfo originalState =
                    original.GetCurrentAnimatorStateInfo(layer);

                AnimatorStateInfo cloneState =
                    clone.GetCurrentAnimatorStateInfo(layer);

                if (originalState.fullPathHash !=
                    cloneState.fullPathHash)
                {
                    clone.Play(
                        originalState.fullPathHash,
                        layer,
                        originalState.normalizedTime
                    );
                }
            }
        }
    }

    // ============================================================
    // WARNING
    // ============================================================

    private void HandleWarning()
    {
        Vector3 pos = transform.position;

        bool inRightWarningZone =
            pos.x > limit.y - warningDistance;

        bool inLeftWarningZone =
            pos.x < limit.x + warningDistance;

        if (!hasSpawnedWarning)
        {
            if (inRightWarningZone)
            {
                SpawnWarning(
                    new Vector3(
                        limit.x + warningSpawnOffset,
                        pos.y,
                        pos.z
                    )
                );

                hasSpawnedWarning = true;
            }
            else if (inLeftWarningZone)
            {
                SpawnWarning(
                    new Vector3(
                        limit.y - warningSpawnOffset,
                        pos.y,
                        pos.z
                    )
                );

                hasSpawnedWarning = true;
            }
        }

        if (!inRightWarningZone && !inLeftWarningZone)
            hasSpawnedWarning = false;
    }

    private void SpawnWarning(Vector3 position)
    {
        if (warningPrefab == null)
            return;

        GameObject warning = Instantiate(
            warningPrefab,
            position,
            Quaternion.identity
        );

        warning.transform.localScale = transform.localScale;
    }

    // ============================================================
    // TELEPORTATION DU GARDE PRINCIPAL
    // ============================================================

    private void HandleScreenWrap()
    {
        Vector3 pos = transform.position;

        if (pos.x > limit.y)
            pos.x = limit.x;
        else if (pos.x < limit.x)
            pos.x = limit.y;

        transform.position = pos;
    }

    // ============================================================
    // DESTRUCTION
    // ============================================================

    private void OnDestroy()
    {
        if (wrapClone != null)
            Destroy(wrapClone);
    }

    // ============================================================
    // GIZMOS
    // ============================================================

    private void OnDrawGizmosSelected()
    {
        Vector2 bounds =
            SC_level_master.instance != null
                ? SC_level_master.instance.limits
                : limit;

        Gizmos.color = Color.green;

        Gizmos.DrawLine(
            new Vector3(bounds.x, -50, 0),
            new Vector3(bounds.x, 50, 0)
        );

        Gizmos.DrawLine(
            new Vector3(bounds.y, -50, 0),
            new Vector3(bounds.y, 50, 0)
        );

        Gizmos.color = Color.yellow;

        Gizmos.DrawLine(
            new Vector3(
                bounds.x + warningDistance,
                -50,
                0
            ),
            new Vector3(
                bounds.x + warningDistance,
                50,
                0
            )
        );

        Gizmos.DrawLine(
            new Vector3(
                bounds.y - warningDistance,
                -50,
                0
            ),
            new Vector3(
                bounds.y - warningDistance,
                50,
                0
            )
        );
    }
}
