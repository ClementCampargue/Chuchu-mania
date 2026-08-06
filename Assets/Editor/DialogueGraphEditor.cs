using UnityEditor;
using UnityEngine;
using UnityEditor.Experimental.GraphView;
using UnityEngine.UIElements;


public class DialogueGraphEditor : EditorWindow
{


    DialogueGraphView graphView;



    [MenuItem("Window/Dialogue Graph")]
    public static void Open()
    {
        GetWindow<DialogueGraphEditor>();
    }



    void OnEnable()
    {
        graphView = new DialogueGraphView();

        graphView.StretchToParentSize();

        rootVisualElement.Add(graphView);
    }



    void OnDisable()
    {
        rootVisualElement.Remove(graphView);
    }

}