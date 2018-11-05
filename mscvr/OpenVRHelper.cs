using UnityEngine;
using System.Collections;
using Valve.VR;
using MSCLoader;

namespace mscvr {
    public class OpenVRHelper : MonoBehaviour {
        public EVREye eye;

        IEnumerator Start() {
            ModConsole.Print("Camera helper on");
            while (true) {
                yield return new WaitForEndOfFrame();
                VRRenderer.instance.RenderEye(eye);
            }            
        }
    }
}