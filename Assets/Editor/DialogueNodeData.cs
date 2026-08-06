using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class DialogueNodeData
{
    public string id;

    public SO_dialogue_character character;

    [TextArea(3, 8)]
    public string dialogueText;

    public List<DialogueChoice> choices = new();
}


[System.Serializable]
public class DialogueChoice
{
    public string text;

    public string nextNodeID;
}