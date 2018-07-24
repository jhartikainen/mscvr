using MSCLoader;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace mscvr {
    public class mscvr : Mod {
        private GameObject vrCamera;
        private CVRSystem vrSystem;
        private RenderTexture renderTexture;
        private Texture_t leftEyeTexture;
        private Texture_t rightEyeTexture;
        private RenderTexture rightEyeRT;
        private Camera vrCam;
        private bool hadError;

        public override string ID => "mscvr";
        public override string Name => "MSC VR";
        public override string Author => "zomg";
        public override string Version => "0.1";

        //Set this to true if you will be load custom assets from Assets folder.
        //This will create subfolder in Assets folder for your mod.
        public override bool UseAssetsFolder => false;

        //Called when mod is loading
        public override void OnLoad() {            
            ModConsole.Print("Initializing OpenVR");
            if (!OpenVRInit()) {
                return;
            }

            OpenVR.Compositor.Device
            ModConsole.Print("Set up camera render texture");            
            uint w = 0, h = 0;
            vrSystem.GetRecommendedRenderTargetSize(ref w, ref h);

            renderTexture = new RenderTexture((int)w, (int)h, 0, RenderTextureFormat.ARGB32);
            renderTexture.useMipMap = false;
            renderTexture.generateMips = false;
            renderTexture.enableRandomWrite = true;
            renderTexture.Create();

            var camObj = new GameObject("VR camera");
            vrCam = camObj.AddComponent<Camera>();
            vrCam.targetTexture = renderTexture;
            
            leftEyeTexture = new Texture_t() {
                eColorSpace = EColorSpace.Gamma,
                eType = ETextureType.DirectX,
                handle = renderTexture.GetNativeTexturePtr()
            };

            rightEyeRT = new RenderTexture((int)w, (int)h, 0, RenderTextureFormat.ARGB32);
            rightEyeRT.useMipMap = false;
            rightEyeRT.generateMips = false;
            rightEyeRT.enableRandomWrite = true;
            rightEyeRT.Create();
            rightEyeTexture = new Texture_t() {
                eColorSpace = EColorSpace.Gamma,
                eType = ETextureType.DirectX,
                handle = rightEyeRT.GetNativeTexturePtr()
            };

            Camera.onPostRender += PostRender;
            
            OpenVR.Compositor.FadeToColor(1f, 0, 0, 0, 255, false);            

            /*ModConsole.Print("Initializing SteamVR");
            ModConsole.Print(SteamVR.instance);

            var fpsController = Camera.main.transform.parent.gameObject;

            var vrRoot = new GameObject("VR root");
            var player = vrRoot.AddComponent<Player>();
            player.trackingOriginTransform = player.transform;

            var vrRig = new GameObject("VR rig");
            player.rigSteamVR = vrRig;
            player.rig2DFallback = new GameObject("VR fallback dummy rig");
            
            vrRig.transform.SetParent(vrRoot.transform);

            var vrCam = new GameObject("VR camera");
            vrCam.AddComponent<Camera>();
            vrCam.transform.SetParent(vrRig.transform);
            vrCam.AddComponent<SteamVR_Camera>();

            player.hmdTransforms = new Transform[] { vrCam.transform };

            var handPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var hand1 = new GameObject("Hand1");
            var hand1Component = hand1.AddComponent<Hand>();
            var hand2 = new GameObject("Hand2");
            var hand2Component = hand2.AddComponent<Hand>();
            hand1Component.otherHand = hand2Component;
            hand2Component.otherHand = hand1Component;
            hand1Component.controllerPrefab = hand2Component.controllerPrefab = handPrefab;

            hand1.transform.SetParent(vrRig.transform);
            hand2.transform.SetParent(vrRig.transform);

            player.hands = new Hand[] { hand1Component, hand2Component };

            //var renderer = fpsController.AddComponent<SteamVR_Render>();
            OpenVR.Compositor.CompositorBringToFront();

            ModConsole.Print("Done");*/
        }

        void PostRender(Camera cam) {
            if(cam != vrCam || hadError) {
                return;
            }

            if (!renderTexture.IsCreated()) {
                ModConsole.Print("RenderTexture failure");
                hadError = true;
            }

            Graphics.SetRenderTarget(rightEyeRT);
            Graphics.DrawTexture(new Rect(0, 0, rightEyeRT.width, rightEyeRT.height), renderTexture);
            Graphics.SetRenderTarget(null);

            TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[3];
            TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[3];
            OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);

            OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseStanding);
            VRTextureBounds_t bounds = new VRTextureBounds_t();
            bounds.vMin = bounds.uMin = 0;
            bounds.vMax = bounds.uMax = 1;
            var compError = OpenVR.Compositor.Submit(EVREye.Eye_Left, ref leftEyeTexture, ref bounds, EVRSubmitFlags.Submit_GlRenderBuffer);
            if(compError != EVRCompositorError.None) {
                ModConsole.Print("Compositor left error");
                ModConsole.Print(compError);
                hadError = true;
            }
            compError = OpenVR.Compositor.Submit(EVREye.Eye_Right, ref rightEyeTexture, ref bounds, EVRSubmitFlags.Submit_GlRenderBuffer);
            if (compError != EVRCompositorError.None) {
                ModConsole.Print("Compositor right error");
                ModConsole.Print(compError);
                hadError = true;
            }
        }

        // Update is called once per frame
        public override void Update() {            
            
        }       

        bool OpenVRInit() {
            var error = EVRInitError.None;
            vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None || !OpenVR.IsHmdPresent()) {
                ModConsole.Print("Error in VR init");
                ModConsole.Print(error);
                return false;
            }

            var helper = new GameObject("OpenVRHelper");
            helper.AddComponent<OpenVRHelper>();
            return true;
        }

        void DeviceReading(ETrackedDeviceClass deviceClass) {
            /*

           for (uint i = 0; i < OpenVR.k_unMaxTrackedDeviceCount; i++) {
               var deviceClass = vrSystem.GetTrackedDeviceClass(i);
               if (deviceClass != ETrackedDeviceClass.Invalid) {

                   ModConsole.Print("OpenVR device at " + i + ": " + deviceClass);
               }
           }*/
           /*
            TrackedDevicePose_t[] poses = new TrackedDevicePose_t[OpenVR.k_unMaxTrackedDeviceCount];
            vrSystem.GetDeviceToAbsoluteTrackingPose(ETrackingUniverseOrigin.TrackingUniverseStanding, 0, poses);

            //var p = poses[deviceClass];
            */
        }
    }
}
