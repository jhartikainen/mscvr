using MSCLoader;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using System;
using EasyHook;

namespace mscvr {
    public class mscvr : Mod {
        private CVRSystem vrSystem;
        private RenderTexture renderTexture;
        private Texture_t leftEyeTexture;
        private Texture_t rightEyeTexture;
        private Camera vrCam;
        private bool hadError;
        private SharpDX.Direct3D11.Texture2D leftRawTexture;
        private SharpDX.Direct3D11.Texture2D rightRawTexture;
        private SharpDX.Direct3D11.Device d3d11Device;
        private SharpDX.Direct3D11.Device unityRenderer;
        private SharpDX.Direct3D11.Texture2D renderTextureDx;

        private VRRig vrRig;
        private VRRenderer vrRenderer;
        private Camera mainCamera;
        private D3D11Hook hook;

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
            
            
            mainCamera = Camera.main;

            /*ModConsole.Print(mainCamera.cullingMask);
            mainCamera.cullingMask = ~mainCamera.cullingMask;

            mainCamera.enabled = false;*/
            
            ModConsole.Print(SystemInfo.graphicsDeviceVersion);
                            
            
            System.Diagnostics.Trace.WriteLine("Initializing our own D3D11 device");
            d3d11Device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware, SharpDX.Direct3D11.DeviceCreationFlags.Debug);            


            hook = new D3D11Hook();
            hook.Create();
            
            System.Diagnostics.Trace.WriteLine("Getting RenderTarget Size");

            uint w = 0, h = 0;
            vrSystem.GetRecommendedRenderTargetSize(ref w, ref h);
            vrRig = new VRRig((int)w, (int)h);
            
            System.Diagnostics.Trace.WriteLine("Fetching Unity D3D11 Device");
            var devChild = vrRig.leftTexture.QueryInterface<SharpDX.Direct3D11.DeviceChild>();
            unityRenderer = devChild.Device;            
            devChild.Dispose();

            ModConsole.Print(unityRenderer.CreationFlags & SharpDX.Direct3D11.DeviceCreationFlags.SingleThreaded);
            vrRenderer = new VRRenderer(d3d11Device, unityRenderer, vrRig);

            //Camera.onPostRender += PostRender;

            hook.OnRender += HookRender;

            System.Diagnostics.Trace.WriteLine("All done");
            ModConsole.Print("MSCVR initialized");         
        }

        void HookRender() {
            vrRenderer.Render();
        }

        void PostRender(Camera cam) {        
            if(cam != mainCamera || hadError) {
                return;
            }

            vrRenderer.Render();

            //Compositor_FrameTiming timing = new Compositor_FrameTiming();
            //OpenVR.Compositor.GetFrameTiming(ref timing, 0);
            //ModConsole.Print(timing.m_nNumFramePresents);
        }
                
        public override void FixedUpdate() {
            //ModConsole.Print(PrintANumber());
            //GL.IssuePluginEvent(1337);



            //vrRig.Move(mainCamera.transform.position, mainCamera.transform.rotation);
        }

        public override void Update() {
            //NEXT: Try again with dx debugging output visible to see hwy the ptr didn't work maybe


            //ModConsole.Print(1f / Time.deltaTime);
        }

        bool OpenVRInit() {
            var error = EVRInitError.None;
            vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None || !OpenVR.IsHmdPresent()) {
                ModConsole.Error("Error in VR init");
                ModConsole.Print(error);
                return false;
            }

            return true;
        }

        ~mscvr() {
            //OpenVR.Shutdown();
            //vrRig.Dispose();
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
