using System;
using System.IO;
using System.Linq;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UISettings : MonoBehaviour
{

    public static readonly int defaultLivePort = 39540;
    public static readonly int defaultPlaybackPort = 39541;

    [SerializeField]
    private VDataCapture captureObject;

    [SerializeField]
    private InputField livePortField;
    
    [SerializeField]
    private InputField playbackPortField;

    [SerializeField]
    private Button resetButton;

    [SerializeField]
    private Button applyButton;

    [SerializeField]
    private Text maxDurationText;

    [SerializeField]
    private Dropdown devices;

    [SerializeField]
    private Dropdown files;

    [SerializeField]
    private Text version;

    public void UpdateFiles()
    {
        string [] paths = Directory.GetFiles(Path.Combine(Application.persistentDataPath, "Bones"));
        files.ClearOptions();
        List<string> fileNames = new List<string>(new string[1]{""});
        for (int i=0; i<paths.Length; i++) {
            if (Path.GetExtension(paths[i]).Equals(".csv")) {
                fileNames.Add(Path.GetFileNameWithoutExtension(paths[i]));
            }
        }
        files.AddOptions(fileNames);
    }

    void Start()
    {
        livePortField.placeholder.GetComponent<Text>().text = defaultLivePort.ToString();
        playbackPortField.placeholder.GetComponent<Text>().text = defaultPlaybackPort.ToString();
        ResetPorts();
        maxDurationText.text = "/" + captureObject.maxDuration.ToString() + "s";
        devices.ClearOptions();
        devices.AddOptions(new List<string>(Microphone.devices));
        UpdateFiles();
        version.text = Application.version;
    }

    public void InputPortChanged()
    {
        resetButton.interactable = livePortField.text.Length == 0 || playbackPortField.text.Length == 0 || (Int32.Parse(livePortField.text) != defaultLivePort || Int32.Parse(playbackPortField.text) != defaultPlaybackPort);
        applyButton.interactable = livePortField.text.Length > 0 && playbackPortField.text.Length > 0 && (Int32.Parse(livePortField.text) != captureObject.livePort || Int32.Parse(playbackPortField.text) != captureObject.playbackPort);
    }

    public void ResetPorts()
    {
        livePortField.text = defaultLivePort.ToString();
        playbackPortField.text = defaultPlaybackPort.ToString();
    }

    public void ApplyPorts()
    {
        int livePort = Int32.Parse(livePortField.text);
        if (livePort != captureObject.livePort) {
            captureObject.livePort = livePort;
            if (!captureObject.CurrentlyPlayingBack()) {
                captureObject.RestartLiveServer();
            }
        }
        int playbackPort = Int32.Parse(playbackPortField.text);
        if (playbackPort != captureObject.playbackPort) {
            captureObject.playbackPort = playbackPort;
            if (captureObject.CurrentlyPlayingBack()) {
                captureObject.RestartPlaybackServer();
            }
        }
    }

}
