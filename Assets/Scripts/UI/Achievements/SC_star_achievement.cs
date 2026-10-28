
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class SC_star_achievement : MonoBehaviour
{
    [System.Serializable]
    public class Connection
    {
        [HideInInspector] public GameObject objectA;
        public GameObject objectB;
    }
    public SC_constellation constellation;

    public SO_achievement achievement;
    public SC_Button button;

    [Header("Condition")]
    public bool debloque = false;
    public bool récolté = false;
    public bool red = false;

    [Header("Connexions")]
    [SerializeField] private List<Connection> connections = new List<Connection>();

    [Header("Lignes")]
    [SerializeField] private float lineWidth = 0.1f;
    [SerializeField] private float distanceFromCenter = 0.5f;
    [SerializeField] private Material lineMaterial;

    public Color color;
    public SpriteRenderer spr;
    public Sprite default_;
    public Sprite unlocked_;
    public Sprite red_;

    private List<LineRenderer> lines = new List<LineRenderer>();

    public Animator anim;
    public ParticleSystem ps;
    public SC_hue_shift_anim hue;

    private void Awake()
    {
        foreach (Connection connection in connections)
        {
            connection.objectA = gameObject;
        }
    }

    private void Start()
    {
        SC_SaveManager save = SC_SaveManager.instance;

        if (save.IsAchievementFinished(achievement))
        {
            debloque = true;

            if (!save.IsAchievementCollected(achievement))
            {
                ps.Play();
                hue.enabled = true;
            }
        }

        if (save.IsAchievementCollected(achievement))
        {
            récolté = true;
        }        bool finished = SC_SaveManager.instance.IsAchievementFinished(achievement);
        bool collected = SC_SaveManager.instance.IsAchievementCollected(achievement);

        if (!collected && finished ||
            SC_achievement_menu.instance.current_wishes > 0)
        {
            anim.SetBool("Unlockable", true);
        }
        else
        {
            anim.SetBool("Unlockable", false);
        }

        UpdateRedState();

        if (collected)
        {
            anim.SetBool("Can_press", false);
            spr.sprite = unlocked_;
        }
        else if (red)
        {
            anim.SetBool("Can_press", true);
            spr.sprite = red_;
        }
        else
        {
            anim.SetBool("Can_press", true);
            spr.sprite = default_;
        }

        UpdateLines();
    }

    private void Update()
    {
        if (SC_achievement_menu.instance.caninteract)
        {
            button.enabled = true;
            button.accept_input = true;
            button.clickable = true;
        }
        else
        {
            button.isSelected = false;
            button.isHovered = false;
            button.enabled = false;
            button.accept_input = false;
            button.clickable = false;
        }
        if (button.isHovered || button.isSelected)
        {
            SC_achievement_menu.instance.update_display(achievement, this);
        }


    }

    private void UpdateRedState()
    {
        if (!debloque)
        {
            red = !HasActiveConnection();
        }
        else
        {
            red = false;
        }
    }

    private bool HasActiveConnection()
    {
        foreach (Connection connection in connections)
        {
            if (connection.objectB == null)
            {
                Debug.LogWarning(name + " : objectB n'est pas assigné.");
                continue;
            }

            SC_star_achievement otherStar =
                connection.objectB.GetComponent<SC_star_achievement>();

            if (otherStar == null)
            {
                Debug.LogWarning(
                    connection.objectB.name +
                    " n'a pas de SC_star_achievement."
                );
                continue;
            }

            Debug.Log(
                name + " connecté à " + otherStar.name +
                " | debloque = " + otherStar.debloque
            );

            if (otherStar.debloque)
                return true;
        }

        return false;
    }
    // Appelée lorsque le joueur essaie de débloquer l'étoile.
    public void unlock()
    {
        SC_SaveManager save = SC_SaveManager.instance;

        // Ne pas proposer de récupérer une récompense déjà collectée.
        if (récolté || save.IsAchievementCollected(achievement))
            return;

        // Si le succès est terminé, aucun wish n'est nécessaire.
        if (save.IsAchievementFinished(achievement))
        {
            TryUnlock(false);
            return;
        }

        // Un succès non terminé nécessite un wish.
        if (SC_achievement_menu.instance.current_wishes <= 0)
        {
            Debug.Log("Pas assez de wishes pour débloquer ce succès.");
            return;
        }

        // On demande confirmation sans dépenser le wish.
        SC_achievement_menu.instance.ShowWishPopup(this);
    }

    // Appelée uniquement après confirmation dans la popup.
    public void ConfirmWishUnlock()
    {
        if (récolté ||
            SC_SaveManager.instance.IsAchievementCollected(achievement))
        {
            return;
        }

        if (SC_SaveManager.instance.IsAchievementFinished(achievement))
        {
            TryUnlock(false);
            return;
        }

        if (SC_achievement_menu.instance.current_wishes <= 0)
        {
            Debug.Log("Tu n'as plus de wishes.");
            return;
        }

        TryUnlock(true);
    }

    private void TryUnlock(bool useWish)
    {
        SC_SaveManager save = SC_SaveManager.instance;
        SC_achievement_menu menu = SC_achievement_menu.instance;

        // Vérification juste avant de dépenser le wish.
        if (récolté || save.IsAchievementCollected(achievement))
            return;

        if (useWish && menu.current_wishes <= 0)
            return;

        bool success = save.CollectAchievementReward(
            achievement,
            useWish
        );

        if (!success)
        {
            Debug.LogWarning(
                "Impossible de récupérer le succès : " +
                achievement.achievementId
            );

            return;
        }

        // Le wish est dépensé uniquement si la récupération réussit.
        if (useWish)
        {
            menu.use_wish();
        }

        debloque = true;
        récolté = true;
        red = false;

        // Animation jouée après confirmation et récupération réussie.
        anim.SetTrigger("Press");



        if (ps != null)
        {
            ps.Stop();
        }

        UpdateLines();

        if (button != null)
        {
            button.UnSelect();
        }

        Debug.Log(
            "Succès récupéré : " + achievement.achievementId
        );
    }
    public void update_visual_unlock()
    {
        hue.enabled = false;
        spr.color = Color.white;
        spr.sprite = unlocked_;
        bool finished = SC_SaveManager.instance.IsAchievementFinished(achievement);
        bool collected = SC_SaveManager.instance.IsAchievementCollected(achievement);

        if (!collected && finished ||
            SC_achievement_menu.instance.current_wishes > 0)
        {
            anim.SetBool("Unlockable", true);
        }
        else
        {
            anim.SetBool("Unlockable", false);
        }

        UpdateRedState();

        if (collected)
        {
            anim.SetBool("Can_press", false);
            spr.sprite = unlocked_;
        }
        else if (red)
        {
            anim.SetBool("Can_press", true);
            spr.sprite = red_;
        }
        else
        {
            anim.SetBool("Can_press", true);
            spr.sprite = default_;
        }
    }
    public void show_reward()
    {
        if (ps != null)
        {
            ps.Stop();
        }
        SC_achievement_menu.instance.pendingStar = this;

        SC_sticker_popup.instance.ShowStickers(
            new SO_Sticker[] { achievement.sticker_reward }
        );
    }

    public void disable_movement()
    {
        SC_ConstellationNavigation.instance.enabled = false;
    }

    private void UpdateLines()
    {
        ClearLines();

        if (!debloque)
            return;

        foreach (Connection connection in connections)
        {
            if (connection.objectA == null || connection.objectB == null)
                continue;

            CreateLine(connection.objectA, connection.objectB);
        }
    }

    private void CreateLine(GameObject objectA, GameObject objectB)
    {
        GameObject lineObject = new GameObject("AchievementLine");
        lineObject.transform.SetParent(transform);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();

        line.sortingOrder = 10;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (lineMaterial != null)
        {
            line.material = lineMaterial;
        }

        Vector3 posA = objectA.transform.position;
        Vector3 posB = objectB.transform.position;
        Vector3 direction = (posB - posA).normalized;

        Vector3 startPosition = posA + direction * distanceFromCenter;
        Vector3 endPosition = posB - direction * distanceFromCenter;

        line.SetPosition(0, startPosition);
        line.SetPosition(1, endPosition);

        lines.Add(line);
    }

    private void ClearLines()
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
            {
                Destroy(line.gameObject);
            }
        }

        lines.Clear();
    }

    private void OnDrawGizmos()
    {
        if (connections == null)
            return;

        foreach (Connection connection in connections)
        {
            if (connection.objectB == null)
                continue;

            Vector3 posA = transform.position;
            Vector3 posB = connection.objectB.transform.position;

            Gizmos.color = Color.yellow;
            Gizmos.DrawLine(posA, posB);
            Gizmos.DrawSphere(posA, 0.08f);
            Gizmos.DrawSphere(posB, 0.08f);
        }
    }
}
