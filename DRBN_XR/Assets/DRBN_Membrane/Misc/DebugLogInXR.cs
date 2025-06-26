using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class DebugLogInXR : MonoBehaviour
{
    public LayoutGroup console;
    public TextMeshProUGUI linePrefab;
    public int maxLines = 20;
    public bool logs = true;
    public bool warning = true;
    public bool errors = true;
    readonly Queue<TextMeshProUGUI> logQueue = new();

    public void Clear()
    {
        while (logQueue.Count > 0)
        {
            var oldLine = logQueue.Dequeue();
            Destroy(oldLine.gameObject);
        }
    }

    void Start()
    {
        linePrefab.gameObject.SetActive(false);
    }

    void OnEnable()
    {
        Application.logMessageReceived += HandleLog;
    }

    void OnDisable()
    {
        Application.logMessageReceived -= HandleLog;
    }

    void HandleLog(string logString, string stackTrace, LogType type)
    {
        var logLine = Instantiate(linePrefab, console.transform);
        if (!logs && type == LogType.Log) return;
        if (!warning && type == LogType.Warning) return;
        if (!errors && (type == LogType.Error || type == LogType.Exception || type == LogType.Assert)) return;
        logLine.color = type switch
        {
            LogType.Error => Color.red,
            LogType.Exception => Color.red,
            LogType.Assert => Color.red,
            LogType.Warning => Color.yellow,
            LogType.Log => Color.white,
            _ => Color.gray,
        };
        logLine.text = logString.Replace("\n", "  ");
        logQueue.Enqueue(logLine);
        logLine.gameObject.SetActive(true);
        while (logQueue.Count > maxLines)
        {
            var oldLine = logQueue.Dequeue();
            Destroy(oldLine.gameObject);
        }
    }

}
