using System.Collections.Generic;
using UnityEngine;


public class PixelParticleSystem : MonoBehaviour
{
    public enum EmissionShape
    {
        Point,
        Circle,
        Sphere,
        Box,
        Cone
    }



    [System.Serializable]
    public class Particle
    {
        public Transform transform;
        public SpriteRenderer renderer;

        public Vector2 position;
        public Vector2 velocity;

        public float life;
        public float maxLife;

        public int spriteIndex;

        public bool active;
    }





    [Header("Sprite Sheet")]
    public Sprite[] particleSprites;

    public bool animateSprites = true;





    [Header("Rendering")]
    public string sortingLayerName = "Default";

    public int orderInLayer = 0;

    public Color particleColor = Color.white;





    [Header("Emission")]
    public EmissionShape shape = EmissionShape.Point;


    public float emissionRate = 50f;

    public int maxParticles = 200;



    [Header("Loop System")]

    public bool playOnStart = true;

    public bool loop = true;

    [Tooltip("Durée d'une émission")]
    public float duration = 2f;



    private bool playing;

    private float loopTimer;






    [Header("Burst")]

    public int burstAmount = 20;






    [Header("Shape")]

    public float emissionRadius = 1f;

    public Vector2 emissionBoxSize = Vector2.one;



    public Vector2 coneDirection = Vector2.up;

    [Range(0, 180)]
    public float coneAngle = 30f;






    [Header("Particle")]

    public float particleLife = 1f;

    public float speed = 5f;





    [Header("Physics")]

    public Vector2 gravity = new Vector2(0, -10);





    [Header("Pixel")]

    public float pixelSize = 1f / 16f;






    private readonly List<Particle> particles = new();


    private float emissionTimer;










    void Awake()
    {
        CreatePool();
    }





    void Start()
    {
        if (playOnStart)
            Play();
    }





    void Update()
    {
        float dt = Time.deltaTime;


        if (playing)
        {
            UpdateEmission(dt);
        }


        UpdateParticles(dt);
    }








    void CreatePool()
    {
        for (int i = 0; i < maxParticles; i++)
        {
            GameObject obj =
                new GameObject("Pixel Particle");


            obj.transform.parent = transform;



            SpriteRenderer sr =
                obj.AddComponent<SpriteRenderer>();



            sr.sortingLayerName =
                sortingLayerName;


            sr.sortingOrder =
                orderInLayer;



            obj.SetActive(false);



            particles.Add(new Particle
            {
                transform = obj.transform,
                renderer = sr
            });
        }
    }










    // =============================
    // CONTROLES PUBLICS
    // =============================


    public void Play()
    {
        playing = true;

        loopTimer = 0;
    }





    public void Stop()
    {
        playing = false;
    }






    public void Burst(int amount)
    {
        for (int i = 0; i < amount; i++)
        {
            Spawn();
        }
    }






    public void Clear()
    {
        foreach (Particle p in particles)
        {
            if (p.active)
            {
                p.active = false;

                p.transform.gameObject.SetActive(false);
            }
        }
    }










    void UpdateEmission(float dt)
    {
        loopTimer += dt;



        if (loopTimer >= duration)
        {
            if (loop)
            {
                loopTimer = 0;
            }
            else
            {
                Stop();
                return;
            }
        }



        emissionTimer += emissionRate * dt;



        while (emissionTimer >= 1f)
        {
            emissionTimer--;

            Spawn();
        }
    }










    void Spawn()
    {
        foreach (Particle p in particles)
        {
            if (p.active)
                continue;



            Vector2 pos =
                GetEmissionPosition();



            Vector2 dir =
                GetEmissionDirection();




            p.position =
                Snap(
                    (Vector2)transform.position +
                    pos
                );



            p.velocity =
                dir * speed;



            p.life =
                particleLife;


            p.maxLife =
                particleLife;




            p.spriteIndex = 0;




            if (particleSprites != null &&
               particleSprites.Length > 0)
            {
                p.renderer.sprite =
                    particleSprites[0];
            }



            p.renderer.color =
                particleColor;



            p.renderer.sortingLayerName =
                sortingLayerName;


            p.renderer.sortingOrder =
                orderInLayer;



            p.transform.position =
                p.position;



            p.transform.gameObject.SetActive(true);



            p.active = true;



            break;
        }
    }










    void UpdateParticles(float dt)
    {
        foreach (Particle p in particles)
        {
            if (!p.active)
                continue;



            p.velocity += gravity * dt;


            p.position += p.velocity * dt;



            p.position =
                Snap(p.position);



            p.transform.position =
                p.position;



            Animate(p);



            p.life -= dt;




            if (p.life <= 0)
            {
                p.active = false;

                p.transform.gameObject.SetActive(false);
            }
        }
    }









    void Animate(Particle p)
    {
        if (!animateSprites)
            return;


        if (particleSprites.Length <= 1)
            return;



        float progress =
            1 -
            (p.life / p.maxLife);



        int frame =
            Mathf.FloorToInt(
                progress *
                particleSprites.Length
            );


        frame =
            Mathf.Clamp(
                frame,
                0,
                particleSprites.Length - 1
            );



        if (frame != p.spriteIndex)
        {
            p.spriteIndex = frame;


            p.renderer.sprite =
                particleSprites[frame];
        }
    }









    Vector2 GetEmissionPosition()
    {
        switch (shape)
        {
            case EmissionShape.Circle:
            case EmissionShape.Sphere:

                return Random.insideUnitCircle *
                       emissionRadius;



            case EmissionShape.Box:

                return new Vector2(
                    Random.Range(
                    -emissionBoxSize.x / 2,
                     emissionBoxSize.x / 2),

                    Random.Range(
                    -emissionBoxSize.y / 2,
                     emissionBoxSize.y / 2)
                );
        }


        return Vector2.zero;
    }









    Vector2 GetEmissionDirection()
    {
        if (shape == EmissionShape.Cone)
        {
            float angle =
                Random.Range(
                -coneAngle,
                 coneAngle);


            return Quaternion.Euler(
                0,
                0,
                angle)
                *
                coneDirection.normalized;
        }


        return Random.insideUnitCircle.normalized;
    }









    Vector2 Snap(Vector2 pos)
    {
        return new Vector2(
            Mathf.Round(pos.x / pixelSize) * pixelSize,
            Mathf.Round(pos.y / pixelSize) * pixelSize
        );
    }








#if UNITY_EDITOR

    void OnDrawGizmosSelected()
    {
        Gizmos.matrix =
            transform.localToWorldMatrix;


        switch (shape)
        {
            case EmissionShape.Box:

                Gizmos.DrawWireCube(
                    Vector3.zero,
                    emissionBoxSize);

                break;


            case EmissionShape.Circle:
            case EmissionShape.Sphere:

                Gizmos.DrawWireSphere(
                    Vector3.zero,
                    emissionRadius);

                break;


            case EmissionShape.Point:

                Gizmos.DrawSphere(
                    Vector3.zero,
                    0.05f);

                break;


            case EmissionShape.Cone:

                Gizmos.DrawRay(
                    Vector3.zero,
                    coneDirection *
                    emissionRadius);

                break;
        }
    }

#endif
}