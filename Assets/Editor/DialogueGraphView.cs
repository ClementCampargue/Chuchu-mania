using System.Collections.Generic;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.EventSystems;


public class DialogueGraphView : GraphView
{
    private void OnKeyDown(KeyDownEvent evt)
    {
        if (evt.keyCode == KeyCode.Space)
        {
            Vector2 mousePosition =
                contentViewContainer.WorldToLocal(
                    evt.originalMousePosition
                );


            DialogueNodeView node = CreateNode();


            node.SetPosition(
                new Rect(
                    mousePosition,
                    new Vector2(250, 200)
                )
            );


            AddElement(node);


            evt.StopPropagation();
        }
    }


    public DialogueGraphView()
    {
        RegisterCallback<KeyDownEvent>(OnKeyDown);

    SetupZoom(ContentZoomer.DefaultMinScale,
                  ContentZoomer.DefaultMaxScale);


        this.AddManipulator(
            new ContentDragger()
        );


        this.AddManipulator(
            new SelectionDragger()
        );


        this.AddManipulator(
            new RectangleSelector()
        );



        AddElement(CreateNode());

    }


    DialogueNodeView CreateNode()
    {
        DialogueNodeView node = new DialogueNodeView();

        node.SetPosition(
            new Rect(100, 100, 200, 150)
        );

        return node;
    }
    public override List<Port> GetCompatiblePorts(
    Port startPort,
    NodeAdapter nodeAdapter)
    {
        List<Port> compatiblePorts = new List<Port>();


        ports.ForEach(port =>
        {
            if (startPort != port
               && startPort.node != port.node
               && startPort.direction != port.direction)
            {
                compatiblePorts.Add(port);
            }
        });


        return compatiblePorts;
    }
    public override void BuildContextualMenu(
    ContextualMenuPopulateEvent evt)
    {
        evt.menu.AppendAction(
            "Créer Dialogue",
            action =>
            {
                DialogueNodeView node = CreateNode();

                node.SetPosition(
                    new Rect(
                        action.eventInfo.localMousePosition,
                        new Vector2(250, 150)
                    )
                );

                AddElement(node);
            }
        );
    }
}