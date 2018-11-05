using UnityEngine;
using System.Collections;
using System;
using MSCLoader;

namespace mscvr {

    public class VRRig : IDisposable {
        private GameObject root;
        public RenderTexture leftRt;
        public RenderTexture rightRt;

        public SharpDX.Direct3D11.Texture2D leftTexture;
        public SharpDX.Direct3D11.Texture2D rightTexture;

        public VRRig(int vpWidth, int vpHeight) {
            root = new GameObject("MSCVR Camera Rig");

            var left = new GameObject("Left Eye");
            var leftCam = left.AddComponent<Camera>();
            left.transform.localPosition.Set(-0.05f, 0, 0);
            left.transform.SetParent(root.transform);
            /*var leftHelper = left.AddComponent<OpenVRHelper>();
            leftHelper.eye = Valve.VR.EVREye.Eye_Left;*/

            var right = new GameObject("Right Eye");
            var rightCam = right.AddComponent<Camera>();
            right.transform.SetParent(root.transform);
            right.transform.localPosition.Set(0.05f, 0, 0);
            /*var rightHelper = right.AddComponent<OpenVRHelper>();
            rightHelper.eye = Valve.VR.EVREye.Eye_Right;*/
            
            leftRt = new RenderTexture(vpWidth, vpHeight, 0, RenderTextureFormat.ARGB32);
            leftRt.useMipMap = false;
            leftRt.Create();
            leftCam.targetTexture = leftRt;

            rightRt = new RenderTexture(vpWidth, vpHeight, 0, RenderTextureFormat.ARGB32);
            rightRt.useMipMap = false;
            rightRt.Create();
            rightCam.targetTexture = rightRt;

            leftTexture = new SharpDX.Direct3D11.Texture2D(leftRt.GetNativeTexturePtr());
            rightTexture = new SharpDX.Direct3D11.Texture2D(rightRt.GetNativeTexturePtr());
        }

        public void Move(Vector3 position, Quaternion rotation) {
            root.transform.position = position;
            root.transform.rotation = rotation;
        }

        public void Dispose() {
            leftRt.Release();
            rightRt.Release();
        }
    }
}