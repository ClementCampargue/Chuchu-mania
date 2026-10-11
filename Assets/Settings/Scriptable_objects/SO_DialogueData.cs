using UnityEngine;
using UnityEngine.Localization;

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/System")]
public class DialogueData : ScriptableObject
{
    public DialogueLine[] lines;
}


[System.Serializable]
public class DialogueLine
{
    public SO_dialogue_character character;

    public LocalizedString text;

    public bool hasChoices;

    public Choice[] choices;
}


[System.Serializable]
public class Choice
{
    public LocalizedString choiceText;


    [Header("Réponse après le choix")]
    public DialogueLine[] answer;
    public string actionID;


}