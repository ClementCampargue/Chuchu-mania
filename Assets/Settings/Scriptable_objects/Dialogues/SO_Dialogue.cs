using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_Dialogue", menuName = "Scriptable Objects/Dialogue")]
public class SO_Dialogue : ScriptableObject
{
    [System.Serializable]
    public class DialogueChoice
    {
        public string choiceText; // Texte du choix affiché au joueur

        public SO_dialogue_line responseLine; // Réponse après le choix
    }

    [System.Serializable]
    public class DialogueGroup
    {
        public List<SO_dialogue_line> lines = new List<SO_dialogue_line>();

        public bool hasChoice; // Active un choix après ce groupe

        public DialogueChoice choice1;
        public DialogueChoice choice2;
    }

    public List<DialogueGroup> groups = new List<DialogueGroup>();
}