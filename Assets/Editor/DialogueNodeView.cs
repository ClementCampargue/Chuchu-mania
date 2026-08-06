using UnityEditor;
using UnityEditor.Experimental.GraphView;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.UIElements;


public class DialogueNodeView : Node
{

    public string nodeID;


    public SO_dialogue_character character;



    public DialogueNodeView()
    {

        title = "Dialogue";


        nodeID = GUID.Generate().ToString();



        // Choix du personnage
        ObjectField characterField = new ObjectField("Personnage");

        characterField.objectType = typeof(SO_dialogue_character);


        characterField.RegisterValueChangedCallback(evt =>
        {
            character = evt.newValue as SO_dialogue_character;

            if (character != null)
            {
                title = character.name;
            }
        });


        extensionContainer.Add(characterField);



        // Texte dialogue
        TextField dialogueText = new TextField("Dialogue");

        dialogueText.multiline = true;


        extensionContainer.Add(dialogueText);



        // Entrée
        Port input = InstantiatePort(
            Orientation.Horizontal,
            Direction.Input,
            Port.Capacity.Multi,
            typeof(bool)
        );

        input.portName = "Entrée";

        inputContainer.Add(input);



        // Sortie
        Port output = InstantiatePort(
            Orientation.Horizontal,
            Direction.Output,
            Port.Capacity.Multi,
            typeof(bool)
        );

        output.portName = "Choix";

        outputContainer.Add(output);



        RefreshExpandedState();
        RefreshPorts();

    }
}