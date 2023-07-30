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
    public float recordingDuration = 10f;

    [SerializeField]
    private Button recordButton;

    [SerializeField]
    private Button saveButton;

    [SerializeField]
    private Button loadButton;

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

    private float startTime = -1.0f;
    // Time | Keys
    public List<List<float>> BlendShapeData = new List<List<float>>();
    private List<float> BlendShapeDataWindow = new List<float>();
    // Time | Positional keys x y z | Rotational keys x y z w
    public List<List<float>> BoneData = new List<List<float>>();
    private List<float> BoneDataWindow = new List<float>();

    public static readonly string[] BlendShapeKeys = {"A", "Angry", "Blink", "Blink_L", "Blink_R", "E", "Fun", "I", "Joy", "LookDown", "LookLeft", "LookRight", "LookUp", "Neutral", "O", "Sorrow", "Surprised", "U"};
    public static readonly string[] BoneKeys = {"Chest", "Head", "Hips", "LeftEye", "LeftFoot", "LeftHand", "LeftIndexDistal", "LeftIndexIntermediate", "LeftIndexProximal", "LeftLittleDistal", "LeftLittleIntermediate", "LeftLittleProximal", "LeftLowerArm", "LeftLowerLeg", "LeftMiddleDistal", "LeftMiddleIntermediate", "LeftMiddleProximal", "LeftRingDistal", "LeftRingIntermediate", "LeftRingProximal", "LeftShoulder", "LeftThumbDistal", "LeftThumbIntermediate", "LeftThumbProximal", "LeftToes", "LeftUpperArm", "LeftUpperLeg", "Neck", "RightEye", "RightFoot", "RightHand", "RightIndexDistal", "RightIndexIntermediate", "RightIndexProximal", "RightLittleDistal", "RightLittleIntermediate", "RightLittleProximal", "RightLowerArm", "RightLowerLeg", "RightMiddleDistal", "RightMiddleIntermediate", "RightMiddleProximal", "RightRingDistal", "RightRingIntermediate", "RightRingProximal", "RightShoulder", "RightThumbDistal", "RightThumbIntermediate", "RightThumbProximal", "RightToes", "RightUpperArm", "RightUpperLeg", "Spine", "UpperChest"};

    private List<string> BlendShapeHeader;
    private List<string> BoneHeader;

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

        if(Microphone.devices.Length <= 0) {    
            Debug.LogWarning("Microphone not connected!");    
            recordButton.interactable = false;  
        }    
        else {    
            micConnected = true;  
            recordButton.interactable = true;  
    
            Microphone.GetDeviceCaps(null, out minFreq, out maxFreq);    
       
            if(minFreq == 0 && maxFreq == 0) { // Microphone supports any frequency 
                maxFreq = 44100;    
            }    
      
            goAudioSource = this.GetComponent<AudioSource>();    
        }
        saveButton.interactable = false;
        loadButton.interactable = File.Exists(Path.Combine(Application.persistentDataPath, "Audio/prediction.wav"));
    }

    public void StartRecording()
    {
        if(micConnected)    
        {    
            if(!Microphone.IsRecording(null))    
            {    
                goAudioSource.clip = Microphone.Start(null, true, (int) recordingDuration, maxFreq);
                recordButton.GetComponentInChildren<Text>().text = "Recording...";
                recordButton.interactable = false;
                loadButton.interactable = false;

                RestartServer(livePort);
                BlendShapeData.Clear();
                BoneData.Clear();
                startTime = Time.realtimeSinceStartup;

                StartCoroutine(WaitForSubmitRecordings());
                StartCoroutine(WaitForStopRecording((int) recordingDuration));
            } 
        }
    }

    IEnumerator WaitForStopRecording(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        startTime = -1;
        StopRecording();
    }

    void RestartServer(int port)
    {
        server.StopServer();
        server.port = port;
        server.StartServer();
    }

    IEnumerator PlayAudioDelay()
    {
        yield return new WaitForSeconds(audioDelay);
        goAudioSource.Play();
    }

    public void StopRecording()
    {
        Microphone.End(null);
        goAudioSource.loop = true;
        StartCoroutine(PlayAudioDelay());
        goAudioSource.Play();
        RestartServer(playbackPort);
        playback.Init();
        recordButton.GetComponentInChildren<Text>().text = "Redo Recording";
        recordButton.interactable = true;
        saveButton.interactable = true;
        loadButton.interactable = File.Exists(Path.Combine(Application.persistentDataPath, "Audio/prediction.wav"));
    }

    public void SaveRecording()
    {
        string timestamp = System.DateTime.Now.ToString().Replace(":", "-").Replace("/", "-").Replace("\\", "-");

        SavWav.Save("Audio/" + timestamp, goAudioSource.clip);
        SavCsv.Save("Blendshapes/" + timestamp, BlendShapeHeader, BlendShapeData);
        SavCsv.Save("Bones/" + timestamp, BoneHeader, BoneData);

        saveButton.interactable = false;
    }

    public async void LoadPrediction()
    {
        goAudioSource.loop = true;

        goAudioSource.clip = await SavWav.Load("Audio/prediction.wav");
        recordingDuration = goAudioSource.clip.length;
        
        BlendShapeData = SavCsv.Load("Blendshapes/prediction.csv");
        BoneData = SavCsv.Load("Bones/prediction.csv");

        StartCoroutine(PlayAudioDelay());
        
        RestartServer(playbackPort);
        playback.Init();
        recordButton.GetComponentInChildren<Text>().text = "Start Recording";
        recordButton.interactable = true;
        saveButton.interactable = false;
        loadButton.interactable = false;
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
        BlendShapeData.Add(BlendShapeDataWindow.GetRange(0, BlendShapeDataWindow.Count));

        BoneDataWindow[0] = timeSync;
        BoneData.Add(BoneDataWindow.GetRange(0, BoneDataWindow.Count));
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
        foreach(var key in _BlendShapeToValueDictionary.Values) {
            BlendShapeDataWindow.Add(key);
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
    }

}
