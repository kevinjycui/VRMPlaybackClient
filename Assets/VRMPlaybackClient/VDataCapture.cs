using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;
using VRM;
using UniGLTF;

[RequireComponent (typeof (AudioSource))]  
public class VDataCapture : MonoBehaviour
{
    private bool micConnected = false;

    private int minFreq;    
    private int maxFreq;
    private AudioSource goAudioSource;

    [SerializeField]
    public float recordingDuration = 0f;

    [SerializeField]
    private Button recordButton;

    [SerializeField]
    private Button stopButton;

    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private Button loadButton;

    [SerializeField]
    private Dropdown loadSource;

    [SerializeField]
    private Text timeText;

    [SerializeField]
    private Text directoryText;

    [SerializeField]
    private VDataPlayback playback;

    [SerializeField]
    public uOSC.uOscServer server;

    [SerializeField]
    public int livePort = 39540;

    [SerializeField]
    public int playbackPort = 39541;

    [SerializeField]
    public float recordRate = 0.01f;

    [SerializeField]
    public float audioDelay = 0.05f;

    [SerializeField]
    public bool staticInterval = false;

    [SerializeField]
    public bool recordAudio = true;

    [HideInInspector]
    public int playbackDuration;

    [SerializeField]
    private Toggle recordAudioToggle;

    [SerializeField]
    private Dropdown deviceDropdown;

    private float startTime = -1.0f;
    // Time | Keys
    [HideInInspector]
    public List<List<float>> BlendShapeData = new List<List<float>>();
    private List<float> BlendShapeDataWindow = new List<float>();
    // Time | Positional keys x y z | Rotational keys x y z w
    [HideInInspector]
    public List<List<float>> BoneData = new List<List<float>>();
    private List<float> BoneDataWindow = new List<float>();

    [HideInInspector]
    public static readonly string[] BlendShapeKeys = {"A", "Angry", "Blink", "Blink_L", "Blink_R", "E", "Fun", "I", "Joy", "LookDown", "LookLeft", "LookRight", "LookUp", "Neutral", "O", "Sorrow", "Surprised", "U"};
    
    [HideInInspector]
    public static readonly string[] BoneKeys = {"Chest", "Head", "Hips", "LeftEye", "LeftFoot", "LeftHand", "LeftIndexDistal", "LeftIndexIntermediate", "LeftIndexProximal", "LeftLittleDistal", "LeftLittleIntermediate", "LeftLittleProximal", "LeftLowerArm", "LeftLowerLeg", "LeftMiddleDistal", "LeftMiddleIntermediate", "LeftMiddleProximal", "LeftRingDistal", "LeftRingIntermediate", "LeftRingProximal", "LeftShoulder", "LeftThumbDistal", "LeftThumbIntermediate", "LeftThumbProximal", "LeftToes", "LeftUpperArm", "LeftUpperLeg", "Neck", "RightEye", "RightFoot", "RightHand", "RightIndexDistal", "RightIndexIntermediate", "RightIndexProximal", "RightLittleDistal", "RightLittleIntermediate", "RightLittleProximal", "RightLowerArm", "RightLowerLeg", "RightMiddleDistal", "RightMiddleIntermediate", "RightMiddleProximal", "RightRingDistal", "RightRingIntermediate", "RightRingProximal", "RightShoulder", "RightThumbDistal", "RightThumbIntermediate", "RightThumbProximal", "RightToes", "RightUpperArm", "RightUpperLeg", "Spine", "UpperChest"};

    private List<string> BlendShapeHeader;
    private List<string> BoneHeader;

    private bool playbackStatus = false;

    [HideInInspector]
    public int maxDuration = 60 * 60 - 1; // Maximum recording length of 59m59s

    private IEnumerator stopRecordingTimer;
    private bool timerFinished = true;

    private string deviceName;

    private bool serverEnabled = true;

    private void InitMicrophone()
    {
        if (Microphone.devices.Length <= 0) {    
            Debug.LogWarning("Microphone not connected!");
            if (recordAudio) {
                micConnected = false;
                recordButton.interactable = false;
            }  
        }    
        else {
            micConnected = true;
            recordButton.interactable = true;  
    
            Microphone.GetDeviceCaps(deviceName, out minFreq, out maxFreq);    
    
            if (minFreq == 0 && maxFreq == 0) { // Microphone supports any frequency 
                maxFreq = 44100;    
            }     
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        BlendShapeHeader = BlendShapeKeys.ToList();
        BlendShapeHeader.Insert(0, "Time");
        BoneHeader = new List<string>();
        BoneHeader.Add("Time");
        foreach (string key in BoneKeys) {
            BoneHeader.Add(key + "PosX");
            BoneHeader.Add(key + "PosY");
            BoneHeader.Add(key + "PosZ");
            BoneHeader.Add(key + "RotX");
            BoneHeader.Add(key + "RotY");
            BoneHeader.Add(key + "RotZ");
            BoneHeader.Add(key + "RotW");
        }

        recordButton.interactable = true;
        if (recordAudio) {
            try {
                deviceName = Microphone.devices[0];
            }
            catch (IndexOutOfRangeException e) {
                deviceName = null;
            }
            InitMicrophone();
        }
        goAudioSource = this.GetComponent<AudioSource>();

        stopButton.interactable = false;
        saveButton.interactable = false;
        loadButton.interactable = true;
        directoryText.text = Application.persistentDataPath + "/";
    }

    void Update()
    {
        if (startTime == -1) {
            return;
        }
        // float timeLeft = recordingDuration - (Time.realtimeSinceStartup - startTime);
        // if (timeLeft >= 0) {
        //     timeText.text = timeLeft.ToString("n1") + "s";
        // }
        float timedPassed = Time.realtimeSinceStartup - startTime;
        timeText.text = timedPassed.ToString("n1") + "s";
        if (timedPassed >= maxDuration - 1) {
            StopRecording();
        }
    }

    public void ToggleRecordAudio(bool flag)
    {
        recordAudio = flag;
        if (!recordAudio) {
            recordButton.interactable = true;
        }
        else {
            InitMicrophone();
        }
    }

    public void UpdateDevice()
    {
        deviceName = deviceDropdown.options[deviceDropdown.value].text;
        InitMicrophone();
    }

    public bool CurrentlyPlayingBack()
    {
        return playbackStatus;
    }

    public void StartRecording()
    {
        if (goAudioSource.isPlaying) {
            goAudioSource.Stop();
        }
        playback.Stop();
        if (recordAudio) {
            if (!micConnected || Microphone.IsRecording(deviceName)) {
                return;
            }
            goAudioSource.clip = Microphone.Start(deviceName, true, maxDuration, maxFreq);
        }
        recordButton.GetComponentInChildren<Text>().text = "Recording...";
        recordButton.interactable = false;
        loadButton.interactable = false;
        recordAudioToggle.interactable = false;
        deviceDropdown.interactable = false;

        playbackStatus = false;
        RestartServer(livePort);
        BlendShapeData.Clear();
        BoneData.Clear();
        startTime = Time.realtimeSinceStartup;
        stopButton.interactable = true;

        if (staticInterval) {
            StartCoroutine(WaitForSubmitRecordings());
        }
        if (recordingDuration > 0f) {
            stopRecordingTimer = WaitForStopRecording((int) recordingDuration);
            StartCoroutine(stopRecordingTimer);
        }
        else if (recordAudio) {
            stopRecordingTimer = WaitForStopRecording(maxDuration);
            StartCoroutine(stopRecordingTimer);
        }
    }

    IEnumerator WaitForStopRecording(int seconds)
    {
        timerFinished = false;
        yield return new WaitForSeconds(seconds);
        timerFinished = true;
        StopRecording();
    }

    void RestartServer(int port)
    {
        server.StopServer();
        server.port = port;
        server.StartServer();
    }

    public void RestartLiveServer() {
        RestartServer(livePort);
    }

    public void RestartPlaybackServer() {
        RestartServer(playbackPort);
        playback.RestartClient(playbackPort);
    }

    public void SetServerActive() {
        if (serverEnabled) {
            server.StopServer();
            serverEnabled = false;
        }
        else {
            server.StartServer();
            serverEnabled = true;
        }
    }

    IEnumerator PlayAudioDelay()
    {
        yield return new WaitForSeconds(audioDelay);
        goAudioSource.Play();
    }

    public void StopRecording()
    {
        playbackDuration = (int) (Time.realtimeSinceStartup - startTime);
        startTime = -1.0f;
        if (!timerFinished) {
            StopCoroutine(stopRecordingTimer);
            timerFinished = true;
        }
        if (recordAudio) {
            int position = Microphone.GetPosition(deviceName);
            Microphone.End(deviceName);
            float[] soundData = new float[goAudioSource.clip.samples * goAudioSource.clip.channels];
            goAudioSource.clip.GetData(soundData, 0);
            float[] trimmedData = new float[position * goAudioSource.clip.channels];
            for (int i=0; i < trimmedData.Length; i++) {
                trimmedData[i] = soundData[i];
            }
            AudioClip trimmedClip = AudioClip.Create(goAudioSource.clip.name, position, goAudioSource.clip.channels, goAudioSource.clip.frequency, false, false);
            trimmedClip.SetData(trimmedData, 0);
            AudioClip.Destroy(goAudioSource.clip);
            goAudioSource.clip = trimmedClip;
            goAudioSource.loop = true;
            StartCoroutine(PlayAudioDelay());
        }
        playbackStatus = true;
        RestartServer(playbackPort);
        playback.Init();
        recordButton.GetComponentInChildren<Text>().text = "Rerecord";
        recordButton.interactable = true;
        stopButton.interactable = false;
        saveButton.interactable = true;
        loadButton.interactable = true;
        recordAudioToggle.interactable = true;
        deviceDropdown.interactable = true;
        timeText.text = "0.0s";
    }

    public void SaveRecording()
    {
        string timestamp = System.DateTime.Now.ToString().Replace(":", "-").Replace("/", "-").Replace("\\", "-");

        if (recordAudio) {
            SavWav.Save(Path.Combine("Audio", timestamp), goAudioSource.clip);
        }
        SavCsv.Save(Path.Combine("Blendshapes", timestamp), BlendShapeHeader, BlendShapeData);
        SavCsv.Save(Path.Combine("Bones", timestamp), BoneHeader, BoneData);

        saveButton.interactable = false;
    }

    public async void LoadPrediction()
    {
        if (goAudioSource.isPlaying) {
            goAudioSource.Stop();
        }
        playback.Stop();
        goAudioSource.loop = true;

        if (loadSource.value == 0) {
            return;
        }

        string source = loadSource.options[loadSource.value].text;
        Debug.Log(Path.Combine(Application.persistentDataPath, "Blendshapes", source + ".csv"));

        if (!File.Exists(Path.Combine(Application.persistentDataPath, "Blendshapes", source + ".csv")) ||
            !File.Exists(Path.Combine(Application.persistentDataPath, "Bones", source + ".csv"))) {
                loadSource.value = 0;
                return;
            }

        BlendShapeData = SavCsv.Load(Path.Combine("Blendshapes", source + ".csv"));
        BoneData = SavCsv.Load(Path.Combine("Bones", source + ".csv"));

        playbackDuration = (int) (BoneData[BoneData.Count-1][0]);

        if (File.Exists(Path.Combine(Application.persistentDataPath, "Audio", source + ".wav"))) {
            goAudioSource.clip = await SavWav.Load(Path.Combine("Audio", source + ".wav"));
            playbackDuration = (int) goAudioSource.clip.length;
            StartCoroutine(PlayAudioDelay());
        }
        
        playbackStatus = true;
        RestartServer(playbackPort);
        playback.Init();
        recordButton.GetComponentInChildren<Text>().text = "Record";
        recordButton.interactable = true;
        stopButton.interactable = false;
        saveButton.interactable = false;
        loadButton.interactable = true;
    }

    public int GetBlendShapeRecordLength()
    {
        return BlendShapeData.Count;
    }

    public List<float> GetBlendShapeRecordAt(int index, float timePassed)
    {
        if (index < 0 || index >= GetBlendShapeRecordLength()) {
            return null;
        }
        List<float> record = BlendShapeData.ElementAt(index);
        if (record.ElementAt(0) > timePassed) {
            return null;
        }
        return record;
    }

    public int GetBoneRecordLength()
    {
        return BoneData.Count;
    }

    public List<float> GetBoneRecordAt(int index, float timePassed)
    {
        if (index < 0 || index >= GetBoneRecordLength()) {
            return null;
        }
        List<float> record = BoneData.ElementAt(index);
        if (record.ElementAt(0) > timePassed) {
            return null;
        }
        return record;
    }

    IEnumerator WaitForSubmitRecordings()
    {
        yield return new WaitForSeconds(recordRate);
        SubmitRecordings();
        if (!recordButton.interactable) {
            StartCoroutine(WaitForSubmitRecordings());
        }
    }

    void SubmitRecordings()
    {
        float timeSync = Time.realtimeSinceStartup - startTime;

        if (BlendShapeDataWindow.Count != BlendShapeHeader.Count || BoneDataWindow.Count != BoneHeader.Count) {
            Debug.Log("Skipped frame at " + timeSync);
            return;
        }

        BlendShapeDataWindow[0] = timeSync;
        SubmitBlendShapeRecordings();

        BoneDataWindow[0] = timeSync;
        SubmitBoneRecordings();
    }

    void SubmitBlendShapeRecordings()
    {
        if (BlendShapeDataWindow.Count == BlendShapeHeader.Count) {
            BlendShapeData.Add(BlendShapeDataWindow.GetRange(0, BlendShapeDataWindow.Count));
        }
    }
    
    void SubmitBoneRecordings()
    {
        if (BoneDataWindow.Count == BoneHeader.Count) {
            BoneData.Add(BoneDataWindow.GetRange(0, BoneDataWindow.Count));
        }
    }

    public void RecordBlendShapes(Dictionary<BlendShapeKey, float> BlendShapeToValueDictionary)
    {
        if (startTime == -1.0f) {
            return;
        }
        // foreach(var item in BlendShapeToValueDictionary) {
        //     Debug.Log(item.Key + ": " + item.Value);
        // }

        if (BoneDataWindow.Count == 0) {
            return;
        }

        SortedDictionary<string, float> _BlendShapeToValueDictionary;
        _BlendShapeToValueDictionary = new SortedDictionary<string, float>(BlendShapeToValueDictionary.ToDictionary(item => item.Key.ToString(), item => item.Value));

        // string keystrings = "";
        // foreach(var key in _BlendShapeToValueDictionary.Keys) {
        //     keystrings += "\"" + key + "\", ";
        // }
        // Debug.Log(keystrings);

        BlendShapeDataWindow.Clear();
        BlendShapeDataWindow.Add(Time.realtimeSinceStartup - startTime);
        try {
            foreach(var key in _BlendShapeToValueDictionary.Values) {
                BlendShapeDataWindow.Add(key);
            }

            if (!staticInterval) {
                SubmitBlendShapeRecordings();
            }
        }
        catch (KeyNotFoundException e) {
            return;
        }
    }

    public void RecordBones(Dictionary<HumanBodyBones, Vector3> HumanBodyBonesPositionTable, Dictionary<HumanBodyBones, Quaternion> HumanBodyBonesRotationTable) 
    {
        if (startTime == -1.0f) {
            return;
        }

        // foreach(var item in HumanBodyBonesPositionTable) {
        //     Debug.Log(item.Key + "_Pos: " + item.Value);
        // }
        // foreach(var item in HumanBodyBonesRotationTable) {
        //     Debug.Log(item.Key + "_Rot: " + item.Value);
        // }

        SortedDictionary<string, Vector3> _HumanBodyBonesPositionTable;
        _HumanBodyBonesPositionTable = new SortedDictionary<string, Vector3>(
            HumanBodyBonesPositionTable.ToDictionary(item => item.Key.ToString(), item => item.Value));

        // string keystrings = "";
        // foreach(var key in _HumanBodyBonesPositionTable.Keys) {
        //     keystrings += "\"" + key + "\", ";
        // }
        // Debug.Log("Positional: " + keystrings);

        SortedDictionary<string, Quaternion> _HumanBodyBonesRotationTable;
        _HumanBodyBonesRotationTable = new SortedDictionary<string, Quaternion>(
            HumanBodyBonesRotationTable.ToDictionary(item => item.Key.ToString(), item => item.Value));

        // keystrings = "";
        // foreach(var key in _HumanBodyBonesRotationTable.Keys) {
        //     keystrings += "\"" + key + "\", ";
        // }
        // Debug.Log("Rotational: " + keystrings);

        BoneDataWindow.Clear();
        BoneDataWindow.Add(Time.realtimeSinceStartup - startTime);

        try {
            foreach(string key in BoneKeys) {
                Vector3 position = _HumanBodyBonesPositionTable[key];
                Quaternion rotation = _HumanBodyBonesRotationTable[key];

                BoneDataWindow.Add(position.x);
                BoneDataWindow.Add(position.y);
                BoneDataWindow.Add(position.z);
                BoneDataWindow.Add(rotation.x);
                BoneDataWindow.Add(rotation.y);
                BoneDataWindow.Add(rotation.z);
                BoneDataWindow.Add(rotation.w);
            }

            // foreach(Vector3 position in _HumanBodyBonesPositionTable.Values) {
            //     BoneDataWindow.Add(position.x);
            //     BoneDataWindow.Add(position.y);
            //     BoneDataWindow.Add(position.z);
            // }
            // foreach(Quaternion rotation in _HumanBodyBonesRotationTable.Values) {
            //     BoneDataWindow.Add(rotation.w);
            //     BoneDataWindow.Add(rotation.x);
            //     BoneDataWindow.Add(rotation.y);
            //     BoneDataWindow.Add(rotation.z);
            // }

            if (!staticInterval) {
                SubmitBoneRecordings();
            }
        }
        catch (KeyNotFoundException e) {
            return;
        }
    }

}
