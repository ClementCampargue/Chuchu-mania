using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SC_bee_stinger : MonoBehaviour
{
    public GameObject dard;
    public List<Transform> positions;

    public float fire_rate = 3f;
    public float move_speed = 5f;
    public Animator anim;
    public SpriteRenderer spriteRenderer;

    public int current_position;

    private bool isMoving;
    private bool isTeleporting;
    public Transform spawn_point;
    private Coroutine attackCoroutine;

    // Distance hors écran avant de téléporter l'abeille
    public float offScreenDistance = 2f;

    void Start()
    {
        if (positions == null || positions.Count < 4)
        {
            Debug.LogError("Il faut renseigner 4 Transform dans positions.");
            return;
        }

        // Choisit un point aléatoire parmi les 4 bords
        current_position = Random.Range(0, 4);

        // Place l'abeille hors écran du côté correspondant
        // afin qu'on puisse la voir entrer jusqu'à son premier point.
        bool spawnFromLeft = IsLeft(current_position);

        transform.position = GetOffScreenPosition(spawnFromLeft);

        // Oriente l'abeille vers son premier point
        FlipTowards(positions[current_position].position);

        // Commence immédiatement le déplacement vers le premier point
        isMoving = true;

        // Commence le cycle d'attaque
        attackCoroutine = StartCoroutine(AttackRoutine());
    }

    void Update()
    {
        if (isMoving && !isTeleporting)
        {
            MoveBee();
        }
    }

    private IEnumerator AttackRoutine()
    {
        while (true)
        {
            // Attend que l'abeille soit arrivée à sa position.
            yield return new WaitUntil(() => !isMoving && !isTeleporting);

            // Le délai avant l'attaque commence seulement
            // une fois que l'abeille est arrivée.
            yield return new WaitForSeconds(fire_rate);

            // Vérification de sécurité
            if (isMoving || isTeleporting)
                continue;

            // Attaque
            attack();

        }
    }

    public void start_moving()
    {

        // Choisir un autre bord
        ChooseNewPosition();

        // Si ce n'est pas un changement de côté,
        // on lance le déplacement normal.
        if (!isTeleporting)
        {
            move();
        }
    }

    public void spawn_dard()
    {
        Instantiate(dard, spawn_point.position, spawn_point.rotation);
    }

    public void attack()
    {
        anim.SetTrigger("fire");
    }

    public void move()
    {
        isMoving = true;

        // Oriente l'abeille vers sa nouvelle destination
        FlipTowards(positions[current_position].position);
    }

    private void MoveBee()
    {
        if (isTeleporting)
            return;

        Vector3 target = positions[current_position].position;

        // Oriente l'abeille dans la direction du déplacement
        FlipTowards(target);

        transform.position = Vector3.MoveTowards(
            transform.position,
            target,
            move_speed * Time.deltaTime
        );

        // Si l'abeille est arrivée au point
        if (Vector3.Distance(transform.position, target) < 0.05f)
        {
            transform.position = target;
            isMoving = false;
        }
    }

    private void ChooseNewPosition()
    {
        int newPosition;

        // Évite de reprendre exactement le même point
        do
        {
            newPosition = Random.Range(0, 4);
        }
        while (newPosition == current_position);

        // Vérifie si on passe d'un côté horizontal à l'autre
        bool currentIsLeft = IsLeft(current_position);
        bool currentIsRight = IsRight(current_position);

        bool newIsLeft = IsLeft(newPosition);
        bool newIsRight = IsRight(newPosition);

        // GAUCHE -> DROITE
        if (currentIsLeft && newIsRight)
        {
            StartCoroutine(TeleportFromLeftToRight(newPosition));
            return;
        }

        // DROITE -> GAUCHE
        if (currentIsRight && newIsLeft)
        {
            StartCoroutine(TeleportFromRightToLeft(newPosition));
            return;
        }

        // Pour les autres déplacements,
        // on utilise le déplacement normal.
        current_position = newPosition;

        // Oriente immédiatement l'abeille vers sa nouvelle destination
        FlipTowards(positions[current_position].position);
    }

    private IEnumerator TeleportFromLeftToRight(int newPosition)
    {
        isTeleporting = true;
        isMoving = false;

        // Position complètement hors écran à gauche
        Vector3 leftOffScreen = GetOffScreenPosition(true);

        // Oriente l'abeille vers la gauche
        FlipTowards(leftOffScreen);

        // L'abeille sort réellement par la gauche
        while (Vector3.Distance(transform.position, leftOffScreen) > 0.05f)
        {
            FlipTowards(leftOffScreen);

            transform.position = Vector3.MoveTowards(
                transform.position,
                leftOffScreen,
                move_speed * Time.deltaTime
            );

            yield return null;
        }

        // Téléportation hors écran à droite
        Vector3 rightOffScreen = GetOffScreenPosition(false);
        transform.position = rightOffScreen;

        // Nouveau point
        current_position = newPosition;

        // Elle entre maintenant par la droite
        Vector3 target = positions[current_position].position;

        FlipTowards(target);

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            FlipTowards(target);

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                move_speed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = target;

        isTeleporting = false;
        isMoving = false;
    }

    private IEnumerator TeleportFromRightToLeft(int newPosition)
    {
        isTeleporting = true;
        isMoving = false;

        // Position complètement hors écran à droite
        Vector3 rightOffScreen = GetOffScreenPosition(false);

        // Oriente l'abeille vers la droite
        FlipTowards(rightOffScreen);

        // L'abeille sort réellement par la droite
        while (Vector3.Distance(transform.position, rightOffScreen) > 0.05f)
        {
            FlipTowards(rightOffScreen);

            transform.position = Vector3.MoveTowards(
                transform.position,
                rightOffScreen,
                move_speed * Time.deltaTime
            );

            yield return null;
        }

        // Téléportation hors écran à gauche
        Vector3 leftOffScreen = GetOffScreenPosition(true);
        transform.position = leftOffScreen;

        // Nouveau point
        current_position = newPosition;

        // Elle entre maintenant par la gauche
        Vector3 target = positions[current_position].position;

        FlipTowards(target);

        while (Vector3.Distance(transform.position, target) > 0.05f)
        {
            FlipTowards(target);

            transform.position = Vector3.MoveTowards(
                transform.position,
                target,
                move_speed * Time.deltaTime
            );

            yield return null;
        }

        transform.position = target;

        isTeleporting = false;
        isMoving = false;
    }

    private bool IsLeft(int position)
    {
        Vector3 pos = positions[position].position;

        Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);

        return viewportPos.x < 0.5f;
    }

    private bool IsRight(int position)
    {
        Vector3 pos = positions[position].position;

        Vector3 viewportPos = Camera.main.WorldToViewportPoint(pos);

        return viewportPos.x > 0.5f;
    }

    private Vector3 GetOffScreenPosition(bool left)
    {
        Camera cam = Camera.main;

        Vector3 currentPos = transform.position;

        Vector3 viewportPosition;

        if (left)
        {
            // Hors écran à gauche
            viewportPosition = new Vector3(
                -0.15f,
                cam.WorldToViewportPoint(currentPos).y,
                cam.WorldToViewportPoint(currentPos).z
            );
        }
        else
        {
            // Hors écran à droite
            viewportPosition = new Vector3(
                1.15f,
                cam.WorldToViewportPoint(currentPos).y,
                cam.WorldToViewportPoint(currentPos).z
            );
        }

        Vector3 worldPosition = cam.ViewportToWorldPoint(viewportPosition);

        // Conserve le Z de l'abeille
        worldPosition.z = transform.position.z;

        return worldPosition;
    }

    private void FlipTowards(Vector3 target)
    {
        if (spriteRenderer == null)
            return;

        float direction = target.x - transform.position.x;

        // On ne flip que si la destination est réellement à gauche/droite
        if (Mathf.Abs(direction) > 0.01f)
        {
            // Sprite original supposé regarder vers la droite.
            spriteRenderer.flipX = direction < 0f;
        }
    }

    private void OnTriggerEnter2D(Collider2D collision)
    {
        if (collision.CompareTag("Damage"))
        {
            Cheeks();
        }
    }

    public void Cheeks()
    {
        anim.SetTrigger("Cheeks");
    }
}