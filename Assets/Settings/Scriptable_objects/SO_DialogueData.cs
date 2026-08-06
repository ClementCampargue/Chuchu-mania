using UnityEngine;
using UnityEngine.Events;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/System")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}


[System.Serializable]
public class DialogueLine
{
    public SO_dialogue_character character;

    [TextArea(2, 4)]
    public string text;

    public bool hasChoices;

    public Choice[] choices;
}


[System.Serializable]
public class Choice
{
    public string choiceText;


    [Header("Réponse après le choix")]
    public DialogueLine[] answer;
    public string actionID;


}