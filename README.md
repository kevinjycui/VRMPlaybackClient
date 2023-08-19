# VRMPlaybackClient
Capture and playback of VRM motion data in VMC protocol

[Read about VMC Protocol](https://protocol.vmc.info/english.html)

Built with [UniGLTF](https://github.com/ousttrue/UniGLTF/releases/tag/v1.27), [UniVRM](https://github.com/vrm-c/UniVRM/releases/tag/v0.113.0), [uOSC](https://github.com/hecomi/uOSC/releases/tag/v2.2.0), [EVMC4U](https://github.com/gpsnmeajp/EasyVirtualMotionCaptureForUnity/releases/tag/v4.0a) (MIT License)

### To use
Open the scene VRMPlaybackClient. It should look like so:

![](github/Screenshot.png)

Open VMC software (such as [VSeeFace](https://www.vseeface.icu/)) and toggle on sending/forwarding OSC/VMC protocol data to the port `39540`.

Play the scene. It should begin copying the playback in the VMC software. Ensure you have a microphone that is accessible by Unity. Record yourself by clicking "Start Recording". Save the recording by clicking "Save Recording" once it finishes. Saved recordings will go in `~/AppData/LocalLow/Junferno/VRMPlaybackClient`. To load a recording, paste the file stem in the textbox and click "Load" (e.g. if there is a file called `~/AppData/LocalLow/Junferno/VRMPlaybackClient/Audio/8-8-2023 3-15-42 PM.wav`, paste `8-8-2023 3-15-42 PM` into the textbox). 

### Adjustable Parameters
DataCapture
 * Recording Duration: Duration of the recording you would like to make in seconds. 
 * Live Port: Port for your third-party VMC software.
 * Record Rate: Rate at which the client will save the current VRM state.
 * Audio Delay: Delay between audio and animation.
