using UnityEngine;

public class SC_urya : MonoBehaviour
{
    public Transform player;
    public Transform positionA;
    public Transform positionB;

    private void OnEnable()
    {
        player = SC_player.instance.transform;
        positionA = GameObject.Find("Urya_down").transform;
        positionB = GameObject.Find("Urya_up").transform;
        Transform oppositePosition = GetOppositePosition();
        transform.position = oppositePosition.position;
    }

    public Transform GetOppositePosition()
    {
        float distanceA = Vector3.Distance(player.position, positionA.position);
        float distanceB = Vector3.Distance(player.position, positionB.position);

        return distanceA < distanceB ? positionB : positionA;
    }
}