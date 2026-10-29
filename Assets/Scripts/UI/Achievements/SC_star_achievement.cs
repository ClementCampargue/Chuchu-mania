using System.Collections.Generic;
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

    private readonly List<LineRenderer> lines = new List<LineRenderer>();

    public Animator anim;
    public ParticleSystem ps;
    public SC_hue_shift_anim hue;

    private void Awake()
    {
        foreach (Connection connection in connections)
        {
            if (connection != null)
                connection.objectA = gameObject;
        }
    }

    private void Start()
    {
        if (achievement == null || SC_SaveManager.instance == null)
            return;

        SC_SaveManager save = SC_SaveManager.instance;

        debloque = save.IsAchievementFinished(achievement)
                   || save.IsAchievementCollected(achievement);
        récolté = save.IsAchievementCollected(achievement);

        if (debloque && !récolté)
        {
            if (ps != null) ps.Play();
            if (hue != null) hue.enabled = true;
        }

        RefreshVisuals();
        RefreshConnectedStars();
    }

    private void Update()
    {
        if (SC_achievement_menu.instance == null)
            return;

        bool canInteract = SC_achievement_menu.instance.caninteract;

        if (button != null)
        {
            if (canInteract)
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
                SC_achievement_menu.instance.update_display(achievement, this);
        }
    }

    private void UpdateRedState()
    {
        red = !debloque && !HasActiveConnection();
    }

    private bool HasActiveConnection()
    {
        foreach (Connection connection in connections)
        {
            if (connection == null || connection.objectB == null)
                continue;

            SC_star_achievement otherStar =
                connection.objectB.GetComponent<SC_star_achievement>();

            if (otherStar != null && otherStar.debloque)
                return true;
        }

        return false;
    }

    // Appelée lorsque le joueur essaie de débloquer l'étoile.
    public void unlock()
    {
        if (achievement == null || SC_SaveManager.instance == null)
            return;

        SC_SaveManager save = SC_SaveManager.instance;

        if (récolté || save.IsAchievementCollected(achievement))
            return;

        if (save.IsAchievementFinished(achievement))
        {
            TryUnlock(false);
            return;
        }

        if (SC_achievement_menu.instance.current_wishes <= 0)
        {
            Debug.Log("Pas assez de wishes pour débloquer ce succès.");
            return;
        }

        SC_achievement_menu.instance.ShowWishPopup(this);
    }

    // Appelée uniquement après confirmation dans la popup.
    public void ConfirmWishUnlock()
    {
        if (achievement == null || SC_SaveManager.instance == null)
            return;

        if (récolté || SC_SaveManager.instance.IsAchievementCollected(achievement))
            return;

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

    public void hide()
    {
        if (SC_achievement_menu.instance == null)
            return;

        SC_achievement_menu.instance.item_box.gameObject.SetActive(false);
        SC_achievement_menu.instance.hide_sticker();
    }

    private void TryUnlock(bool useWish)
    {
        if (achievement == null ||
            SC_SaveManager.instance == null ||
            SC_achievement_menu.instance == null)
            return;

        SC_SaveManager save = SC_SaveManager.instance;
        SC_achievement_menu menu = SC_achievement_menu.instance;

        if (récolté || save.IsAchievementCollected(achievement))
            return;

        if (useWish && menu.current_wishes <= 0)
            return;

        bool success = save.CollectAchievementReward(achievement, useWish);

        if (!success)
        {
            Debug.LogWarning("Impossible de récupérer le succès : " + achievement.achievementId);
            return;
        }

        if (useWish)
            menu.use_wish();

        debloque = true;
        récolté = true;
        red = false;

        if (anim != null)
            anim.SetTrigger("Press");

        if (ps != null)
            ps.Stop();

        if (hue != null)
            hue.enabled = false;

        if (button != null)
            button.UnSelect();

        // Met immédiatement à jour cette étoile et toutes ses voisines.

        Invoke("delay", 2);
        Debug.Log("Succès récupéré : " + achievement.achievementId);
    }

    void delay()
    {
        RefreshVisuals();
        RefreshConnectedStars();
    }

    // Rafraîchit l'apparence de cette étoile à partir de son état actuel.
    public void update_visual_unlock()
    {
        RefreshVisuals();
        RefreshConnectedStars();
    }

    private void RefreshVisuals()
    {
        if (achievement == null || SC_SaveManager.instance == null)
            return;

        bool finished = SC_SaveManager.instance.IsAchievementFinished(achievement);
        bool collected = SC_SaveManager.instance.IsAchievementCollected(achievement);

        // Synchronise les booléens locaux avec la sauvegarde.
        if (finished || collected)
            debloque = true;

        if (collected)
            récolté = true;

        UpdateRedState();

        if (hue != null && collected)
            hue.enabled = false;

        if (spr != null)
        {
            spr.color = Color.white;

            if (récolté)
                spr.sprite = unlocked_;
            else if (red)
                spr.sprite = red_;
            else
                spr.sprite = default_;
        }

        if (anim != null)
        {
            bool unlockable = (!collected && finished) ||
                              SC_achievement_menu.instance != null &&
                              SC_achievement_menu.instance.current_wishes > 0;

            anim.SetBool("Unlockable", unlockable);
            anim.SetBool("Can_press", !collected);
        }

        UpdateLines();
    }

    // Actualise l'état visuel et les lignes des étoiles directement connectées.
    private void RefreshConnectedStars()
    {
        foreach (Connection connection in connections)
        {
            if (connection == null || connection.objectB == null)
                continue;

            SC_star_achievement otherStar =
                connection.objectB.GetComponent<SC_star_achievement>();

            if (otherStar == null || otherStar == this)
                continue;

            otherStar.RefreshVisuals();
        }
    }

    public void show_reward()
    {
        if (ps != null)
            ps.Stop();

        if (SC_achievement_menu.instance != null)
            SC_achievement_menu.instance.pendingStar = this;

        if (SC_sticker_popup.instance != null && achievement != null)
        {
            SC_sticker_popup.instance.ShowStickers(
                new SO_Sticker[] { achievement.sticker_reward }
            );
        }
    }

    public void disable_movement()
    {
        if (SC_ConstellationNavigation.instance != null)
            SC_ConstellationNavigation.instance.enabled = false;
    }

    private void UpdateLines()
    {
        ClearLines();

        if (!debloque)
            return;

        foreach (Connection connection in connections)
        {
            if (connection == null ||
                connection.objectA == null ||
                connection.objectB == null)
                continue;

            CreateLine(connection.objectA, connection.objectB);
        }
    }

    private void CreateLine(GameObject objectA, GameObject objectB)
    {
        GameObject lineObject = new GameObject("AchievementLine");
        lineObject.transform.SetParent(transform, true);

        LineRenderer line = lineObject.AddComponent<LineRenderer>();
        line.sortingOrder = 10;
        line.positionCount = 2;
        line.useWorldSpace = true;
        line.startColor = color;
        line.endColor = color;
        line.startWidth = lineWidth;
        line.endWidth = lineWidth;

        if (lineMaterial != null)
            line.material = lineMaterial;

        Vector3 posA = objectA.transform.position;
        Vector3 posB = objectB.transform.position;
        Vector3 direction = (posB - posA).normalized;

        line.SetPosition(0, posA + direction * distanceFromCenter);
        line.SetPosition(1, posB - direction * distanceFromCenter);

        lines.Add(line);
    }

    private void ClearLines()
    {
        foreach (LineRenderer line in lines)
        {
            if (line != null)
                Destroy(line.gameObject);
        }

        lines.Clear();
    }

    private void OnDrawGizmos()
    {
        if (connections == null)
            return;

        foreach (Connection connection in connections)
        {
            if (connection == null || connection.objectB == null)
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
