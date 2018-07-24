using MSCLoader;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;

namespace mscvr {
    public class mscvr : Mod {
        private GameObject vrCamera;
        private CVRSystem vrSystem;

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
            OpenVRInit();
            ModConsole.Print("Initializing SteamVR");
            ModConsole.Print(SteamVR.instance);

            var fpsController = Camera.main.transform.parent.gameObject;

            var vrObject = new GameObject("VR Junk");

            var player = fpsController.AddComponent<Player>();
            
            vrObject.AddComponent<Camera>();
            var vrCamera = vrObject.AddComponent<SteamVR_Camera>();

            player.hmdTransforms = new Transform[] { vrCamera.transform };

            //vrCamera.transform.SetParent(fpsController.transform);

            var handPrefab = GameObject.CreatePrimitive(PrimitiveType.Cube);
            handPrefab.transform.localScale = new Vector3(0.3f, 0.3f, 0.3f);
            var hand1 = new GameObject("Hand1");
            var hand1Component = hand1.AddComponent<Hand>();
            var hand2 = new GameObject("Hand2");
            var hand2Component = hand2.AddComponent<Hand>();
            hand1Component.otherHand = hand2Component;
            hand2Component.otherHand = hand1Component;
            hand1Component.controllerPrefab = hand2Component.controllerPrefab = handPrefab;

            player.hands = new Hand[] { hand1Component, hand2Component };

            //var renderer = fpsController.AddComponent<SteamVR_Render>();
            ModConsole.Print("Done");
        }

        // Update is called once per frame
        public override void Update() {            
            //OpenVR.Compositor.Submit(EVREye.Eye_Left,)
        }

        void OpenVRInit() {
            var error = EVRInitError.None;
            vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None) {
                ModConsole.Print("Error in VR init");
                ModConsole.Print(error);
                return;
            }
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
