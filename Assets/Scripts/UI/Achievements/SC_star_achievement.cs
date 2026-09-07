using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

public class SC_star_achievement : MonoBehaviour
{
    [System.Serializable]
    public class Connection
    {
        public GameObject objectA;
        public GameObject objectB;
    }

    public SO_achievement achievement;
    public SC_Button button;
    [Header("Condition")]
    public bool debloque = false;
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
    void Start()
    {
        if (achievement.réussi)
        {
            debloque = true;
        }

        UpdateRedState();
        UpdateLines();
    }

    void Update()
    {
        if (button.isHovered || button.isSelected)
        {
    
            SC_achievement_menu.instance.update_display(achievement, this);
        }
        if (achievement.réussi || SC_achievement_menu.instance.current_wishes>0)
        {
            anim.SetBool("Unlockable", true);

        }
        else
        {
            anim.SetBool("Unlockable", false);
        }

        // Vérifie si l'étoile possède une connexion
        UpdateRedState();

        if (debloque)
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

    private void UpdateRedState()
    {
        // Si l'étoile n'est pas débloquée et n'a aucune connexion valide,
        // elle passe en rouge.
        if (!debloque)
        {
            red = !HasActiveConnection();
        }
        else
        {
            red = false;
        }
    }
    public void show_reward()
    {
        SC_sticker_popup.instance.ShowStickers(
            new SO_Sticker[] { achievement.sticker_reward }
        );
    }

    private bool HasActiveConnection()
    {
        foreach (Connection connection in connections)
        {
            if (connection.objectA == null || connection.objectB == null)
                continue;

            GameObject otherObject = null;

            // Cette étoile est A
            if (connection.objectA == gameObject)
            {
                otherObject = connection.objectB;
            }
            // Cette étoile est B
            else if (connection.objectB == gameObject)
            {
                otherObject = connection.objectA;
            }

            // Cette connexion ne concerne pas cette étoile
            if (otherObject == null)
                continue;

            // Vérifie que l'autre étoile est réellement active/débloquée
            SC_star_achievement otherStar =
                otherObject.GetComponent<SC_star_achievement>();

            if (otherStar != null && otherStar.debloque)
            {
                return true;
            }
        }

        return false;
    }

    public void unlock()
    {
        if (debloque) return;

        if (SC_achievement_menu.instance.current_wishes > 0)
        {
            SC_achievement_menu.instance.use_wish();
            debloque = true;
            UpdateLines();
            button.UnSelect();

        }
        else if (achievement.réussi)
        {
            debloque = true;
            UpdateLines();
            button.UnSelect();
        }

     
    }

    public void disable_movement()
    {
        SC_ConstellationNavigation.instance.enabled = false;
        SC_achievement_menu.instance.hide_sticker();
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
            line.material = lineMaterial;

        Vector3 posA = objectA.transform.position;
        Vector3 posB = objectB.transform.position;

        Vector3 direction = (posB - posA).normalized;

        // Départ de la ligne à une certaine distance du centre de A
        Vector3 startPosition = posA + direction * distanceFromCenter;

        // Arrivée de la ligne à une certaine distance du centre de B
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
                Destroy(line.gameObject);
        }

        lines.Clear();
    }
}
