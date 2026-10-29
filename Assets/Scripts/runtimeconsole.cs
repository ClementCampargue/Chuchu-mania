using UnityEngine;

public class RuntimeConsole : MonoBehaviour
{
    private string logs = "";
    private Vector2 scroll;

    private void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    private void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    private void HandleLog(
        string message,
        string stackTrace,
        LogType type)
    {
        logs += $"[{type}] {message}\n";

        if (type == LogType.Error ||
            type == LogType.Exception ||
            type == LogType.Assert)
        {
            logs += stackTrace + "\n";
        }
    }

    private void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 600, 400));
        scroll = GUILayout.BeginScrollView(scroll);

        GUILayout.TextArea(logs);

        GUILayout.EndScrollView();
        GUILayout.EndArea();
    }
}