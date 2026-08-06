using UnityEngine;


public class DialogueManager : MonoBehaviour
{

    public DialogueGraph graph;

    DialogueNode current;



    void Start()
    {
        StartDialogue(graph.startNode);
    }



    public void StartDialogue(DialogueNode node)
    {
        current = node;
        DisplayNode();
    }



    void DisplayNode()
    {
        Debug.Log(
            current.character.name
            + " : "
            + current.dialogueText
        );


        foreach (var choice in current.choices)
        {
            Debug.Log("CHOIX : " + choice.text);
        }
    }



    public void SelectChoice(int index)
    {
        current = current.choices[index].nextNode;

        DisplayNode();
    }
}