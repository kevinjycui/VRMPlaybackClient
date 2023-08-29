using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Net.Sockets;

public class ExceptionHandler : MonoBehaviour
{
    [SerializeField]
    private Text errorText;

    void Awake()
    {
        Application.logMessageReceived += HandleException;
        DontDestroyOnLoad(gameObject);
    }

    void Start()
    {
        errorText.gameObject.SetActive(false);
    }

    public void Dismiss()
    {
        errorText.gameObject.SetActive(false);
    }

    void HandleException(string logString, string stackTrace, LogType type)
    {
        // Debug.Log(type);
        if (type == LogType.Error) {
            Debug.Log(logString);
            if (logString.Contains("SocketException")) {
                errorText.gameObject.SetActive(true);
            }
        }
    }
}
