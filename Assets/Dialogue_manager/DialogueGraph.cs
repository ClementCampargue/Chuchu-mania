using UnityEngine;
using System.Collections.Generic;


[CreateAssetMenu(fileName = "DialogueGraph", menuName = "Dialogue/Graph")]
public class DialogueGraph : ScriptableObject
{
    public DialogueNode startNode;

    public List<DialogueNode> nodes = new();
}