using UnityEngine;
using System.Collections;
using System;

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

            var right = new GameObject("Right Eye");
            var rightCam = right.AddComponent<Camera>();
            right.transform.localPosition.Set(0.05f, 0, 0);

            leftRt = new RenderTexture(vpWidth, vpHeight, 0, RenderTextureFormat.ARGB32);
            leftRt.useMipMap = false;
            leftRt.Create();
            leftCam.targetTexture = leftRt;

            rightRt = new RenderTexture(vpWidth, vpHeight, 0, RenderTextureFormat.ARGB32);
            rightRt.useMipMap = false;
            rightRt.Create();
            rightCam.targetTexture = rightRt;
            var mat = new Material("");

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