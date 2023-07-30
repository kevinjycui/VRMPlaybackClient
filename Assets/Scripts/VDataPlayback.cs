using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Profiling;
using VRM;
using UniGLTF;

[RequireComponent(typeof(uOSC.uOscClient))]
public class VDataPlayback : MonoBehaviour 
{
    uOSC.uOscClient client = null;

    private float loadTime = -1.0f;
    private int blendShapeIndex = 0;
    private int boneIndex = 0;
    
    [SerializeField]
    private VDataCapture captureObject;

    void Start()
    {
        client = GetComponent<uOSC.uOscClient>();
    }

    public void Init()
    {
        blendShapeIndex = 0;
        boneIndex = 0;
        loadTime = Time.realtimeSinceStartup;

        StartCoroutine(WaitForStopRecording((int) captureObject.recordingDuration));
    }

    IEnumerator WaitForStopRecording(int seconds)
    {
        yield return new WaitForSeconds(seconds);
        Init();
    }

    bool UpdateBlendShapes(float timeSync)
    {
        List<float> blendShapeRecord = captureObject.GetBlendShapeRecordAt(blendShapeIndex, timeSync);

        if (blendShapeRecord == null) {
            return false;
        }

        for (int i=0; i<VDataCapture.BlendShapeKeys.Length; i++) {
            client.Send("/VMC/Ext/Blend/Val", VDataCapture.BlendShapeKeys[i], blendShapeRecord.ElementAt(i+1));
        }
        client.Send("/VMC/Ext/Blend/Apply");

        return true;
    }

    bool UpdateBones(float timeSync)
    {
        List<float> boneRecord = captureObject.GetBoneRecordAt(boneIndex, timeSync);

        if (boneRecord == null) {
            return false;
        }

        for (int i=0; i<VDataCapture.BoneKeys.Length; i++) {
            int pointer = (i * 7) + 1;
            client.Send("/VMC/Ext/Bone/Pos", VDataCapture.BoneKeys[i], 
                boneRecord.ElementAt(pointer), boneRecord.ElementAt(pointer+1), boneRecord.ElementAt(pointer+2), 
                boneRecord.ElementAt(pointer+3), boneRecord.ElementAt(pointer+4), boneRecord.ElementAt(pointer+5), boneRecord.ElementAt(pointer+6));
        }

        return true;
    }

    void Update()
    {
        if (client == null) {
            return;
        }
        
        if (loadTime == -1.0f) {
            return;
        }

        float timeSync = Time.realtimeSinceStartup - loadTime;

        if (UpdateBlendShapes(timeSync)) {
            blendShapeIndex++;
        }

        if (UpdateBones(timeSync)) {
            boneIndex++;
        }

    }
}