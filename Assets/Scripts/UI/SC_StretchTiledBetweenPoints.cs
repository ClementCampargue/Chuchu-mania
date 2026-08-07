using UnityEngine;
using UnityEngine.UI;

public class SC_StretchTiledBetweenPoints : MonoBehaviour
{
    public RectTransform pointA;
    public RectTransform pointB;

    public RectTransform line;
    public Image image;

    public float thickness = 20f;

    void Update()
    {
        UpdateLine();
    }


    void UpdateLine()
    {
        Vector3 posA = pointA.position;
        Vector3 posB = pointB.position;


        // Position au milieu
        line.position = (posA + posB) / 2f;


        // Distance
        float distance = Vector3.Distance(posA, posB);


        // Taille
        line.sizeDelta = new Vector2(
            distance,
            thickness
        );


        // Rotation
        Vector3 direction = posB - posA;

        float angle = Mathf.Atan2(
            direction.y,
            direction.x
        ) * Mathf.Rad2Deg;


        line.rotation = Quaternion.Euler(
            0,
            0,
            angle
        );


        // Tiling horizontal
        if (image != null)
        {
            image.material.mainTextureScale =
                new Vector2(distance / 100f, 1);
        }
    }
}