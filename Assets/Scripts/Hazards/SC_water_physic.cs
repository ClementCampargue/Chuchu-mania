using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(BoxCollider2D))]
public class SC_water_physic : MonoBehaviour
{
    // =========================================================
    // WATER SIZE
    // =========================================================

    [Header("Water Size")]

    [Min(0.1f)]
    public float width = 10f;

    [Min(0.1f)]
    public float depth = 3f;

    public static SC_water_physic instance;


    // =========================================================
    // SURFACE
    // =========================================================

    [Header("Surface")]

    [Min(3)]
    public int resolution = 50;

    [Tooltip("Hauteur de repos de la surface.")]
    public float surfaceHeight = 0f;


    // =========================================================
    // IDLE WAVES
    // =========================================================

    [Header("Idle Waves")]

    [Tooltip("Active les vagues continues.")]
    public bool idleWaves = true;

    [Tooltip("Amplitude générale.")]
    [Min(0f)]
    public float idleAmplitude = 0.12f;

    [Tooltip("Vitesse générale.")]
    [Min(0f)]
    public float idleSpeed = 1f;

    [Tooltip("Longueur de la première vague.")]
    [Min(0.1f)]
    public float idleWaveLength = 4f;

    [Tooltip("Amplitude de la deuxième vague.")]
    [Min(0f)]
    public float idleSecondaryAmplitude = 0.05f;

    [Tooltip("Longueur de la deuxième vague.")]
    [Min(0.1f)]
    public float idleSecondaryWaveLength = 2.2f;

    [Tooltip("Variation lente de l'amplitude.")]
    [Min(0f)]
    public float idleBreathing = 0.025f;

    [Tooltip("Décalage de phase.")]
    public float idlePhaseOffset = 1.37f;


    // =========================================================
    // PHYSICS
    // =========================================================

    [Header("Wave Physics")]

    [Tooltip("Force qui ramène les vagues vers zéro.")]
    [Min(0f)]
    public float stiffness = 0.035f;

    [Tooltip("Amortissement.")]
    [Range(0f, 1f)]
    public float damping = 0.035f;

    [Tooltip("Propagation entre les points.")]
    [Min(0f)]
    public float spread = 0.22f;

    [Tooltip("Nombre de passes de propagation.")]
    [Range(1, 8)]
    public int spreadPasses = 3;

    [Tooltip("Déplacement vertical maximum.")]
    [Min(0.1f)]
    public float maxWaveHeight = 3f;


    // =========================================================
    // ENTRY / EXIT
    // =========================================================

    [Header("Entry / Exit Waves")]

    [Tooltip("Force lors de l'entrée.")]
    [Min(0f)]
    public float enterWaveForce = 1.0f;

    [Tooltip("Force lors de la sortie.")]
    [Min(0f)]
    public float exitWaveForce = 0.45f;

    [Tooltip("Influence de la vitesse lors de l'entrée.")]
    [Min(0f)]
    public float entryVelocityMultiplier = 0.20f;

    [Tooltip("Influence de la vitesse lors de la sortie.")]
    [Min(0f)]
    public float exitVelocityMultiplier = 0.08f;

    [Tooltip("Force maximale d'un impact.")]
    [Min(0f)]
    public float maxImpactForce = 3f;

    [Tooltip("Nombre de points touchés.")]
    [Range(1, 12)]
    public int impactRadius = 4;

    [Tooltip("Force de l'impulsion physique.")]
    [Min(0f)]
    public float impulseStrength = 0.12f;


    // =========================================================
    // BIG WAVE
    // =========================================================

    [Header("Big Wave")]

    [Tooltip("Active les grosses vagues.")]
    public bool bigWaveEnabled = true;

    [Tooltip("Force de la grosse déformation centrale.")]
    [Min(0f)]
    public float bigWaveForce = 2.5f;

    [Tooltip("Rayon de la déformation centrale.")]
    [Range(1, 30)]
    public int bigWaveRadius = 8;

    [Tooltip("Vitesse de déplacement des deux vagues.")]
    [Min(0.01f)]
    public float bigWaveSpeed = 5f;

    [Tooltip("Amplitude initiale des deux vagues.")]
    [Min(0f)]
    public float bigWaveAmplitude = 1.2f;

    [Tooltip("Largeur du front de chaque vague.")]
    [Range(1, 30)]
    public int bigWaveWidth = 7;

    [Tooltip("Vitesse à laquelle les deux vagues s'affaiblissent.")]
    [Min(0f)]
    public float bigWaveFadeSpeed = 0.5f;

    [Tooltip("Impulsion physique de la grosse vague.")]
    [Min(0f)]
    public float bigWaveImpulse = 0.35f;


    // =========================================================
    // PLAYER MOVEMENT
    // =========================================================

    [Header("Player Movement")]

    [Tooltip("Active les petites vagues du joueur.")]
    public bool playerMovementWaves = true;

    [Tooltip("Vitesse horizontale minimale.")]
    [Min(0f)]
    public float playerMinSpeed = 0.5f;

    [Tooltip("Influence du mouvement.")]
    [Range(0f, 1f)]
    public float playerWaveInfluence = 0.2f;

    [Tooltip("Force générée par la vitesse.")]
    [Min(0f)]
    public float playerWaveMultiplier = 0.08f;

    [Tooltip("Force maximale.")]
    [Min(0f)]
    public float playerMaxWaveForce = 0.8f;

    [Tooltip("Temps entre deux impulsions.")]
    [Min(0f)]
    public float playerWaveCooldown = 0.12f;

    [Tooltip("Nombre de points touchés.")]
    [Range(1, 8)]
    public int playerImpactRadius = 2;

    // =========================================================
    // DEBUG
    // =========================================================

    [Header("Debug")]

    [Tooltip("Active les contrôles de debug.")]
    public bool debugInput = true;

    [Tooltip("Force du clic gauche.")]
    [Min(0f)]
    public float debugSplashForce = 1f;

    [Tooltip("Force du clic molette.")]
    [Min(0f)]
    public float debugBigWaveForce = 2.5f;

    [Tooltip("Clic gauche = Splash.")]
    public bool debugLeftClick = true;

    [Tooltip("Clic molette = Big Wave.")]
    public bool debugMiddleClick = true;


    // =========================================================
    // VISUAL
    // =========================================================

    [Header("Visual")]

    public Color waterColor =
        new Color(0.1f, 0.5f, 1f, 0.6f);

    [Tooltip("Position du fond par rapport à la surface.")]
    public float bottomOffset = -3f;


    // =========================================================
    // INTERNAL
    // =========================================================

    private Mesh mesh;

    private float[] heights;
    private float[] velocities;

    private float[] idleHeights;

    private float[] leftDeltas;
    private float[] rightDeltas;

    private Vector3[] vertices;

    private float pointSpacing;

    private Transform player;

    private Rigidbody2D playerRigidbody;

    private Vector3 previousPlayerPosition;

    private float playerWaveTimer;

    private float idleTime;


    // =========================================================
    // TRAVELING WAVES
    // =========================================================

    private class TravelingWave
    {
        public float currentIndex;

        public int direction;

        public float speed;

        public float currentAmplitude;

        public float width;

        public bool playerHit;
    }


    private readonly List<TravelingWave>
        travelingWaves =
        new List<TravelingWave>();


    // =========================================================
    // UNITY
    // =========================================================

    private void Awake()
    {
        instance = this;
        resolution =
            Mathf.Max(
                3,
                resolution
            );


        heights =
            new float[resolution];

        velocities =
            new float[resolution];

        idleHeights =
            new float[resolution];

        leftDeltas =
            new float[resolution];

        rightDeltas =
            new float[resolution];

        vertices =
            new Vector3[
                resolution * 2
            ];


        pointSpacing =
            width /
            (resolution - 1);


        CreateMesh();

        SetupCollider();

        FindPlayer();
    }


    private void Update()
    {
        FindPlayer();

        UpdateDebugInput();

        UpdateIdleWaves();

        UpdatePlayerMovement();

        UpdateTravelingBigWaves();

        SimulateWater();

        UpdateMesh();
    }


    // =========================================================
    // DEBUG
    // =========================================================

    private void UpdateDebugInput()
    {
        if (!debugInput)
            return;


        // -----------------------------------------------------
        // CLIC GAUCHE
        // -----------------------------------------------------

        if (
            debugLeftClick &&
            Input.GetMouseButtonDown(0)
        )
        {
            Vector2 mouseWorld =
                GetMouseWorldPosition();


            Splash(
                mouseWorld,
                debugSplashForce
            );
        }


        // -----------------------------------------------------
        // CLIC MOLETTE
        // -----------------------------------------------------

        if (
            debugMiddleClick &&
            Input.GetMouseButtonDown(2)
        )
        {
            Vector2 mouseWorld =
                GetMouseWorldPosition();


            BigWave(
                mouseWorld,
                debugBigWaveForce
            );
        }
    }


    private Vector2 GetMouseWorldPosition()
    {
        Camera cam =
            Camera.main;


        if (cam == null)
            return transform.position;


        Vector3 mouse =
            Input.mousePosition;


        mouse.z =
            Mathf.Abs(
                cam.transform.position.z -
                transform.position.z
            );


        return cam.ScreenToWorldPoint(
            mouse
        );
    }


    // =========================================================
    // PLAYER
    // =========================================================

    private void FindPlayer()
    {
        if (player != null)
            return;


        if (SC_player.instance != null)
        {
            player =
                SC_player.instance.transform;


            playerRigidbody =
                player.GetComponent<Rigidbody2D>();


            previousPlayerPosition =
                player.position;
        }
    }


    // =========================================================
    // IDLE WAVES
    // =========================================================

    private void UpdateIdleWaves()
    {
        if (!idleWaves)
        {
            for (int i = 0; i < resolution; i++)
                idleHeights[i] = 0f;


            return;
        }


        idleTime +=
            Time.deltaTime *
            idleSpeed;


        for (int i = 0; i < resolution; i++)
        {
            float x =
                -width * 0.5f +
                i * pointSpacing;


            // -------------------------------------------------
            // GRANDE VAGUE
            // -------------------------------------------------

            float phase1 =
                (x / idleWaveLength) *
                Mathf.PI * 2f;


            float wave1 =
                Mathf.Sin(
                    phase1 +
                    idleTime
                );


            // -------------------------------------------------
            // PETITE VAGUE
            // -------------------------------------------------

            float phase2 =
                (x / idleSecondaryWaveLength) *
                Mathf.PI * 2f;


            float wave2 =
                Mathf.Sin(
                    phase2 -
                    idleTime * 1.37f +
                    idlePhaseOffset
                );


            // -------------------------------------------------
            // RESPIRATION
            // -------------------------------------------------

            float breathing =
                1f +
                Mathf.Sin(
                    idleTime * 0.35f
                ) *
                idleBreathing;


            idleHeights[i] =
                (
                    wave1 *
                    idleAmplitude +
                    wave2 *
                    idleSecondaryAmplitude
                ) *
                breathing;
        }
    }


    // =========================================================
    // PLAYER MOVEMENT
    // =========================================================

    private void UpdatePlayerMovement()
    {
        if (!playerMovementWaves)
            return;


        if (player == null)
            return;


        float dt =
            Mathf.Max(
                Time.deltaTime,
                0.0001f
            );


        Vector3 currentPosition =
            player.position;


        Vector3 movement =
            currentPosition -
            previousPlayerPosition;


        previousPlayerPosition =
            currentPosition;


        float horizontalSpeed =
            Mathf.Abs(
                movement.x
            ) / dt;


        // -----------------------------------------------------
        // COOLDOWN
        // -----------------------------------------------------

        playerWaveTimer -= dt;


        if (playerWaveTimer > 0f)
            return;


        if (horizontalSpeed < playerMinSpeed)
            return;


        // -----------------------------------------------------
        // FORCE
        // -----------------------------------------------------

        float speed =
            horizontalSpeed -
            playerMinSpeed;


        float force =
            speed *
            playerWaveMultiplier;


        force =
            Mathf.Clamp(
                force,
                0f,
                playerMaxWaveForce
            );


        if (force <= 0f)
            return;


        force *=
            playerWaveInfluence;


        AddWave(
            player.position.x,
            force,
            -1f,
            playerImpactRadius
        );


        playerWaveTimer =
            playerWaveCooldown;
    }


    // =========================================================
    // BIG WAVES UPDATE
    // =========================================================

    private void UpdateTravelingBigWaves()
    {
        if (travelingWaves.Count == 0)
            return;


        float dt =
            Time.deltaTime;


        for (
            int w = travelingWaves.Count - 1;
            w >= 0;
            w--
        )
        {
            TravelingWave wave =
                travelingWaves[w];


            // =================================================
            // AFFAIBLISSEMENT
            // =================================================

            wave.currentAmplitude =
                Mathf.MoveTowards(
                    wave.currentAmplitude,
                    0f,
                    bigWaveFadeSpeed *
                    dt
                );


            // -------------------------------------------------
            // VAGUE MORTE
            // -------------------------------------------------

            if (
                wave.currentAmplitude <=
                0.001f
            )
            {
                travelingWaves.RemoveAt(w);

                continue;
            }


            // =================================================
            // DÉPLACEMENT
            // =================================================

            float movement =
                wave.speed *
                dt /
                pointSpacing;


            wave.currentIndex +=
                movement *
                wave.direction;


            // =================================================
            // BORD DU MESH
            // =================================================

            if (
                wave.currentIndex < 0f ||
                wave.currentIndex >
                resolution - 1
            )
            {
                travelingWaves.RemoveAt(w);

                continue;
            }


            // =================================================
            // DÉFORMATION
            // =================================================

            AddTravelingWaveShape(
                wave
            );

        }
    }


    // =========================================================
    // TRAVELING WAVE SHAPE
    // =========================================================

    private void AddTravelingWaveShape(
        TravelingWave wave
    )
    {
        int center =
            Mathf.RoundToInt(
                wave.currentIndex
            );


        int radius =
            Mathf.Max(
                1,
                Mathf.RoundToInt(
                    wave.width
                )
            );


        for (
            int offset = -radius;
            offset <= radius;
            offset++
        )
        {
            int index =
                center +
                offset;


            if (
                index < 0 ||
                index >= resolution
            )
            {
                continue;
            }


            // -------------------------------------------------
            // DISTANCE
            // -------------------------------------------------

            float distance =
                Mathf.Abs(
                    offset
                ) /
                (float)radius;


            if (distance > 1f)
                continue;


            // -------------------------------------------------
            // FALLOFF
            // -------------------------------------------------

            float falloff =
                1f -
                distance;


            // Smoothstep
            falloff =
                falloff *
                falloff *
                (
                    3f -
                    2f *
                    falloff
                );


            // -------------------------------------------------
            // DÉFORMATION
            // -------------------------------------------------

            float displacement =
                wave.currentAmplitude *
                falloff;


            // IMPORTANT :
            // On AJOUTE à la déformation existante.
            heights[index] +=
                displacement;


            // -------------------------------------------------
            // IMPULSION
            // -------------------------------------------------

            velocities[index] +=
                displacement *
                bigWaveImpulse *
                0.1f;


            heights[index] =
                Mathf.Clamp(
                    heights[index],
                    -maxWaveHeight,
                    maxWaveHeight
                );
        }
    }

    // =========================================================
    // SIMULATION
    // =========================================================

    private void SimulateWater()
    {
        float dt =
            Mathf.Min(
                Time.deltaTime,
                0.033f
            );


        // =====================================================
        // RESTAURATION + DAMPING
        // =====================================================

        for (int i = 0; i < resolution; i++)
        {
            float restoringForce =
                -heights[i] *
                stiffness;


            velocities[i] +=
                restoringForce *
                dt *
                60f;


            float dampingFactor =
                Mathf.Pow(
                    1f - damping,
                    dt * 60f
                );


            velocities[i] *=
                dampingFactor;


            heights[i] +=
                velocities[i] *
                dt *
                60f;


            heights[i] =
                Mathf.Clamp(
                    heights[i],
                    -maxWaveHeight,
                    maxWaveHeight
                );
        }


        // =====================================================
        // PROPAGATION
        // =====================================================

        for (
            int pass = 0;
            pass < spreadPasses;
            pass++
        )
        {
            for (int i = 0; i < resolution; i++)
            {
                leftDeltas[i] = 0f;

                rightDeltas[i] = 0f;
            }


            // -------------------------------------------------
            // DIFFÉRENCES
            // -------------------------------------------------

            for (int i = 0; i < resolution; i++)
            {
                if (i > 0)
                {
                    leftDeltas[i] =
                        spread *
                        (
                            heights[i] -
                            heights[i - 1]
                        );
                }


                if (i < resolution - 1)
                {
                    rightDeltas[i] =
                        spread *
                        (
                            heights[i] -
                            heights[i + 1]
                        );
                }
            }


            // -------------------------------------------------
            // PROPAGATION
            // -------------------------------------------------

            for (int i = 0; i < resolution; i++)
            {
                if (i > 0)
                {
                    velocities[i - 1] +=
                        leftDeltas[i];
                }


                if (i < resolution - 1)
                {
                    velocities[i + 1] +=
                        rightDeltas[i];
                }
            }
        }
    }


    // =========================================================
    // NORMAL WAVE
    // =========================================================

    private void AddWave(
        float worldX,
        float force,
        float direction,
        int radius
    )
    {
        int center =
            WorldXToIndex(
                worldX
            );


        radius =
            Mathf.Max(
                1,
                radius
            );


        force =
            Mathf.Clamp(
                force,
                0f,
                maxImpactForce
            );


        for (
            int offset = -radius;
            offset <= radius;
            offset++
        )
        {
            int index =
                center +
                offset;


            if (
                index < 0 ||
                index >= resolution
            )
            {
                continue;
            }


            float normalizedDistance =
                Mathf.Abs(offset) /
                (float)radius;


            float falloff =
                1f -
                normalizedDistance;


            falloff *=
                falloff;


            float displacement =
                force *
                falloff *
                direction;


            // -------------------------------------------------
            // AJOUTE à l'existant
            // -------------------------------------------------

            heights[index] +=
                displacement;


            velocities[index] +=
                displacement *
                impulseStrength;


            heights[index] =
                Mathf.Clamp(
                    heights[index],
                    -maxWaveHeight,
                    maxWaveHeight
                );
        }
    }


    // =========================================================
    // BIG WAVE
    // =========================================================

    /// <summary>
    /// Crée une grosse vague.
    ///
    /// Une déformation centrale est créée puis deux fronts
    /// partent simultanément vers la gauche et la droite.
    /// </summary>
    public void BigWave(
        Vector2 worldPosition,
        float force = -1f
    )
    {
        if (!bigWaveEnabled)
            return;


        if (force < 0f)
            force =
                bigWaveForce;


        force =
            Mathf.Clamp(
                force,
                0f,
                maxWaveHeight
            );


        int center =
            WorldXToIndex(
                worldPosition.x
            );


        // =====================================================
        // IMPACT CENTRAL
        // =====================================================

        int radius =
            Mathf.Max(
                1,
                bigWaveRadius
            );


        for (
            int offset = -radius;
            offset <= radius;
            offset++
        )
        {
            int index =
                center +
                offset;


            if (
                index < 0 ||
                index >= resolution
            )
            {
                continue;
            }


            float distance =
                Mathf.Abs(offset) /
                (float)radius;


            float falloff =
                1f -
                distance;


            falloff =
                falloff *
                falloff *
                (
                    3f -
                    2f *
                    falloff
                );


            float displacement =
                -force *
                falloff;


            heights[index] +=
                displacement;


            velocities[index] +=
                displacement *
                bigWaveImpulse;


            heights[index] =
                Mathf.Clamp(
                    heights[index],
                    -maxWaveHeight,
                    maxWaveHeight
                );
        }


        // =====================================================
        // VAGUE GAUCHE
        // =====================================================

        TravelingWave leftWave =
            new TravelingWave();


        leftWave.currentIndex =
            center;


        leftWave.direction =
            -1;


        leftWave.speed =
            bigWaveSpeed;


        leftWave.currentAmplitude =
            bigWaveAmplitude *
            force;


        leftWave.width =
            bigWaveWidth;


        leftWave.playerHit =
            false;


        travelingWaves.Add(
            leftWave
        );


        // =====================================================
        // VAGUE DROITE
        // =====================================================

        TravelingWave rightWave =
            new TravelingWave();


        rightWave.currentIndex =
            center;


        rightWave.direction =
            1;


        rightWave.speed =
            bigWaveSpeed;


        rightWave.currentAmplitude =
            bigWaveAmplitude *
            force;


        rightWave.width =
            bigWaveWidth;


        rightWave.playerHit =
            false;


        travelingWaves.Add(
            rightWave
        );
    }


    // =========================================================
    // ENTRY
    // =========================================================

    private void OnTriggerEnter2D(
        Collider2D other
    )
    {
        Rigidbody2D rb =
            other.attachedRigidbody;


        float speed =
            0f;


        if (rb != null)
        {
            speed =
                rb.linearVelocity.magnitude;
        }


        float force =
            enterWaveForce +
            speed *
            entryVelocityMultiplier;


        force =
            Mathf.Clamp(
                force,
                0f,
                maxImpactForce
            );


        AddWave(
            other.transform.position.x,
            force,
            -1f,
            impactRadius
        );
    }


    // =========================================================
    // EXIT
    // =========================================================

    private void OnTriggerExit2D(
        Collider2D other
    )
    {
        Rigidbody2D rb =
            other.attachedRigidbody;


        float speed =
            0f;


        if (rb != null)
        {
            speed =
                rb.linearVelocity.magnitude;
        }


        float force =
            exitWaveForce +
            speed *
            exitVelocityMultiplier;


        force =
            Mathf.Clamp(
                force,
                0f,
                maxImpactForce
            );


        AddWave(
            other.transform.position.x,
            force,
            1f,
            impactRadius
        );
    }


    // =========================================================
    // MESH
    // =========================================================

    private void CreateMesh()
    {
        mesh =
            new Mesh();


        mesh.name =
            "Dynamic Water Mesh";


        mesh.MarkDynamic();


        GetComponent<MeshFilter>().mesh =
            mesh;


        MeshRenderer renderer =
            GetComponent<MeshRenderer>();


        Material material =
            renderer.sharedMaterial;


        if (material == null)
        {
            material =
                new Material(
                    Shader.Find(
                        "Sprites/Default"
                    )
                );


            material.color =
                waterColor;


            renderer.sharedMaterial =
                material;
        }


        UpdateMesh();
    }


    private void UpdateMesh()
    {
        if (mesh == null)
            return;


        Vector2[] uv =
            new Vector2[
                vertices.Length
            ];


        int[] triangles =
            new int[
                (resolution - 1) *
                6
            ];


        // =====================================================
        // VERTICES
        // =====================================================

        for (int i = 0; i < resolution; i++)
        {
            float x =
                -width * 0.5f +
                i * pointSpacing;


            float finalHeight =
                idleHeights[i] +
                heights[i];


            // -------------------------------------------------
            // SURFACE
            // -------------------------------------------------

            vertices[i] =
                new Vector3(
                    x,
                    surfaceHeight +
                    finalHeight,
                    0f
                );


            // -------------------------------------------------
            // FOND
            // -------------------------------------------------

            vertices[
                i + resolution
            ] =
                new Vector3(
                    x,
                    surfaceHeight +
                    bottomOffset,
                    0f
                );


            float u =
                (float)i /
                (resolution - 1);


            uv[i] =
                new Vector2(
                    u,
                    1f
                );


            uv[
                i + resolution
            ] =
                new Vector2(
                    u,
                    0f
                );
        }


        // =====================================================
        // TRIANGLES
        // =====================================================

        int triangleIndex = 0;


        for (
            int i = 0;
            i < resolution - 1;
            i++
        )
        {
            int topLeft =
                i;


            int topRight =
                i + 1;


            int bottomLeft =
                i + resolution;


            int bottomRight =
                i + resolution + 1;


            triangles[
                triangleIndex++
            ] =
                topLeft;


            triangles[
                triangleIndex++
            ] =
                topRight;


            triangles[
                triangleIndex++
            ] =
                bottomRight;


            triangles[
                triangleIndex++
            ] =
                topLeft;


            triangles[
                triangleIndex++
            ] =
                bottomRight;


            triangles[
                triangleIndex++
            ] =
                bottomLeft;
        }


        mesh.Clear();


        mesh.vertices =
            vertices;


        mesh.triangles =
            triangles;


        mesh.uv =
            uv;


        mesh.RecalculateBounds();
    }


    // =========================================================
    // COLLIDER
    // =========================================================

    private void SetupCollider()
    {
        BoxCollider2D collider =
            GetComponent<BoxCollider2D>();


        collider.isTrigger =
            true;


        collider.size =
            new Vector2(
                width,
                depth
            );


        collider.offset =
            new Vector2(
                0f,
                bottomOffset * 0.5f
            );
    }


    // =========================================================
    // WORLD X -> INDEX
    // =========================================================

    private int WorldXToIndex(
        float worldX
    )
    {
        Vector3 worldPosition =
            new Vector3(
                worldX,
                transform.position.y,
                0f
            );


        float localX =
            transform
                .InverseTransformPoint(
                    worldPosition
                )
                .x;


        int index =
            Mathf.RoundToInt(
                (
                    localX +
                    width * 0.5f
                ) /
                pointSpacing
            );


        return Mathf.Clamp(
            index,
            0,
            resolution - 1
        );
    }


    // =========================================================
    // WORLD X -> FLOAT INDEX
    // =========================================================

    private float WorldXToFloatIndex(
        float worldX
    )
    {
        Vector3 worldPosition =
            new Vector3(
                worldX,
                transform.position.y,
                0f
            );


        float localX =
            transform
                .InverseTransformPoint(
                    worldPosition
                )
                .x;


        return
            (
                localX +
                width * 0.5f
            ) /
            pointSpacing;
    }


    // =========================================================
    // PLAYER IN WATER
    // =========================================================

    private bool IsPlayerInWater()
    {
        if (player == null)
            return false;


        float surfaceWorldY =
            transform.TransformPoint(
                new Vector3(
                    0f,
                    surfaceHeight,
                    0f
                )
            ).y;


        float bottomWorldY =
            transform.TransformPoint(
                new Vector3(
                    0f,
                    surfaceHeight +
                    bottomOffset,
                    0f
                )
            ).y;


        float top =
            Mathf.Max(
                surfaceWorldY,
                bottomWorldY
            );


        float bottom =
            Mathf.Min(
                surfaceWorldY,
                bottomWorldY
            );


        float playerY =
            player.position.y;


        return
            playerY <= top &&
            playerY >= bottom;
    }


    // =========================================================
    // PUBLIC API
    // =========================================================

    /// <summary>
    /// Crée une petite vague vers le bas.
    /// </summary>
    public void Splash(
        Vector2 worldPosition,
        float force
    )
    {
        AddWave(
            worldPosition.x,
            force,
            -1f,
            impactRadius
        );
    }


    /// <summary>
    /// Crée une grosse vague au centre de l'eau.
    /// </summary>
    public void BigSplash(
        float force = 2f
    )
    {
        BigWave(
            transform.position,
            force
        );
    }


    /// <summary>
    /// Crée une grosse vague à une position précise.
    /// </summary>
    public void BigSplashAt(
        Vector2 worldPosition,
        float force = 2f
    )
    {
        BigWave(
            worldPosition,
            force
        );
    }


    /// <summary>
    /// Crée une vague vers le haut.
    /// </summary>
    public void SplashUp(
        Vector2 worldPosition,
        float force
    )
    {
        AddWave(
            worldPosition.x,
            force,
            1f,
            impactRadius
        );
    }


    /// <summary>
    /// Remet toutes les vagues physiques à zéro.
    /// Les vagues idle continuent.
    /// </summary>
    public void ResetWater()
    {
        for (int i = 0; i < resolution; i++)
        {
            heights[i] = 0f;

            velocities[i] = 0f;

            leftDeltas[i] = 0f;

            rightDeltas[i] = 0f;
        }


        travelingWaves.Clear();
    }


    /// <summary>
    /// Active ou désactive les vagues idle.
    /// </summary>
    public void SetIdleWaves(
        bool enabled
    )
    {
        idleWaves =
            enabled;
    }
}
