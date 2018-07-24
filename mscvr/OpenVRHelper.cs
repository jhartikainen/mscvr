using UnityEngine;
using System.Collections;
using Valve.VR;

public class OpenVRHelper : MonoBehaviour {
    private void OnDestroy() {
        OpenVR.Shutdown();
    }    
}
