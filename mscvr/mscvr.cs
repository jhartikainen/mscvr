using MSCLoader;
using UnityEngine;
using Valve.VR;
using Valve.VR.InteractionSystem;
using SharpDX.DXGI;
using System.Runtime.InteropServices;
using System;

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
        private Texture2D leftDxTexture;
        private SharpDX.Direct3D11.Texture2D leftRawTexture;
        private SharpDX.Direct3D11.Texture2D rightRawTexture;
        private SharpDX.Direct3D11.ShaderResourceView leftSRV;
        private SharpDX.Direct3D11.Device d3d11Device;
        private bool tookScreen;
        private SharpDX.Direct3D11.Device unityRenderer;
        private int counter;
        private Texture2D temp;
        private SharpDX.Direct3D11.Texture2D renderTextureDx;

        public override string ID => "mscvr";
        public override string Name => "MSC VR";
        public override string Author => "zomg";
        public override string Version => "0.1";

        //Set this to true if you will be load custom assets from Assets folder.
        //This will create subfolder in Assets folder for your mod.
        public override bool UseAssetsFolder => false;
        /*
        //Lets make our calls from the Plugin
        [DllImport("mscvrhook")]
        private static extern int PrintANumber();

        [DllImport("mscvrhook")]
        private static extern IntPtr PrintHello();

        [DllImport("mscvrhook")]
        private static extern int AddTwoIntegers(int i1, int i2);

        [DllImport("mscvrhook")]
        private static extern float AddTwoFloats(float f1, float f2);
        */

        //Called when mod is loading
        public override void OnLoad() {
            ModConsole.Print("Initializing OpenVR");
            if (!OpenVRInit()) {
                return;
            }
                        
            ModConsole.Print(SystemInfo.graphicsDeviceVersion);

            /*ModConsole.Print(PrintHello());
            ModConsole.Print(PrintANumber());
            */

            System.Diagnostics.Trace.WriteLine("Getting RenderTarget Size");

            uint w = 0, h = 0;
            vrSystem.GetRecommendedRenderTargetSize(ref w, ref h);
            ModConsole.Print("Setting up camera render texture");
            renderTexture = new RenderTexture((int)w, (int)h, 0, RenderTextureFormat.ARGB32);
            renderTexture.useMipMap = false;
            renderTexture.Create();

            System.Diagnostics.Trace.WriteLine("Fetching Unity D3D11 Device");
            renderTextureDx = new SharpDX.Direct3D11.Texture2D(renderTexture.GetNativeTexturePtr());
            var devChild = renderTextureDx.QueryInterface<SharpDX.Direct3D11.DeviceChild>();
            unityRenderer = devChild.Device;
            devChild.Dispose();                     
        

            System.Diagnostics.Trace.WriteLine("Initializing our own D3D11 device");
            d3d11Device = new SharpDX.Direct3D11.Device(SharpDX.Direct3D.DriverType.Hardware);//, SharpDX.Direct3D11.DeviceCreationFlags.Debug);

            /*var debug = d3d11Device.QueryInterface<SharpDX.Direct3D11.DeviceDebug>();
            var infoQueue = debug.QueryInterface<SharpDX.Direct3D11.InfoQueue>();
            infoQueue.AddApplicationMessage(SharpDX.Direct3D11.MessageSeverity.Warning, "Initializing OpenVR shitz");
            */

            ModConsole.Print("Creating description");
            var description = new SharpDX.Direct3D11.Texture2DDescription() {
    
                MipLevels = 1,
                ArraySize = 1,
                SampleDescription = new SampleDescription() { Count = 1 },
            
                Width = (int)w,
                Height = (int)h,

                BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource | SharpDX.Direct3D11.BindFlags.RenderTarget,
                CpuAccessFlags = 0, //SharpDX.Direct3D11.CpuAccessFlags.Read | SharpDX.Direct3D11.CpuAccessFlags.Write,
                Format = Format.R8G8B8A8_UNorm,
                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.Shared,
                Usage = SharpDX.Direct3D11.ResourceUsage.Default
            };
            ModConsole.Print("Creating leftRawTexture");
            System.Diagnostics.Trace.WriteLine("Creating D3D11 Textures");
            leftRawTexture = new SharpDX.Direct3D11.Texture2D(d3d11Device, description);
            ModConsole.Print("Creating rightRawTexture");
            rightRawTexture = new SharpDX.Direct3D11.Texture2D(d3d11Device, description);          

            ModConsole.Print("Creating SRV");
            System.Diagnostics.Trace.WriteLine("Creating D3D11 ShaderResourceView");
            var viewDesc = new SharpDX.Direct3D11.ShaderResourceViewDescription() {
                Format = Format.R8G8B8A8_UNorm,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource() {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };

            leftSRV = new SharpDX.Direct3D11.ShaderResourceView(d3d11Device, leftRawTexture, viewDesc);                     
           
            ModConsole.Print("Loading D3D11 texture");
            System.Diagnostics.Trace.WriteLine("Creating external texture from SRV");
            //leftDxTexture = Texture2D.CreateExternalTexture((int)w, (int)h, TextureFormat.ARGB32, false, false, leftSRV.NativePointer);          

            ModConsole.Print("Setting up VR camera");
            System.Diagnostics.Trace.WriteLine("Creating Camera");
            var camObj = new GameObject("VR camera");
            camObj.transform.position = Camera.main.transform.position;
            vrCam = camObj.AddComponent<Camera>();
            vrCam.targetTexture = renderTexture;

            leftEyeTexture = new Texture_t() {
                eColorSpace = EColorSpace.Gamma,
                eType = ETextureType.DirectX,
                handle = leftRawTexture.NativePointer
            };            
            rightEyeTexture = new Texture_t() {
                eColorSpace = EColorSpace.Gamma,
                eType = ETextureType.DirectX,
                handle = rightRawTexture.NativePointer
            };

            Camera.onPostRender += PostRender;
            System.Diagnostics.Trace.WriteLine("All done");
            ModConsole.Print("MSCVR initialized");

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


            vrCam.targetTexture = null;           

            System.Diagnostics.Trace.WriteLine("Attempting to copy resource");
            System.Diagnostics.Trace.WriteLine(leftRawTexture.Description.Format);
            System.Diagnostics.Trace.WriteLine(renderTextureDx.Description.Format);

            var sharedHandle = leftRawTexture.QueryInterface<Resource>().SharedHandle;
            var unityLeft = unityRenderer.OpenSharedResource<SharpDX.Direct3D11.Texture2D>(sharedHandle);
            unityRenderer.ImmediateContext.CopyResource(renderTextureDx, unityLeft);
            //d3d11Device.ImmediateContext.CopyResource(d3d, rightRawTexture);                        

            unityRenderer.ImmediateContext.Flush();
            //mutex.Release(0);
            //d3d11Device.ImmediateContext.Flush();

            //unityLeft.Dispose();

            System.Diagnostics.Trace.WriteLine("Finished copy resource?");

            vrCam.targetTexture = renderTexture;

            /*Graphics.SetRenderTarget(rightEyeRT);
            Graphics.DrawTexture(new Rect(0, 0, rightEyeRT.width, rightEyeRT.height), renderTexture);
            Graphics.SetRenderTarget(null);*/

            //var mutex = leftRawTexture.QueryInterface<KeyedMutex>();
            //mutex.Acquire(0, 10000);
            /*RenderTexture.active = renderTexture;
            leftDxTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
            leftDxTexture.Apply();
            RenderTexture.active = null;*/

            //mutex.Release(0);

            counter++;
            
               
            //if (!tookScreen) {
            if(counter == 20) {
                tookScreen = true;

                /*var viewDesc = new SharpDX.Direct3D11.ShaderResourceViewDescription() {
                    Format = Format.R8G8B8A8_UNorm,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource() {
                        MipLevels = 1,
                        MostDetailedMip = 0
                    }
                };

                var srv = new SharpDX.Direct3D11.ShaderResourceView(d3d11Device, leftRawTexture, viewDesc);

                
                var tex = Texture2D.CreateExternalTexture(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false, srv.NativePointer);*/
                ModConsole.Print("Writing to file");
                //System.IO.File.WriteAllBytes(@"D:\butts.png", leftDxTexture.EncodeToPNG());

                RenderTexture.active = renderTexture;
                var tempNormalTexture = new Texture2D(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false);
                tempNormalTexture.ReadPixels(new Rect(0, 0, renderTexture.width, renderTexture.height), 0, 0);
                tempNormalTexture.Apply();
                RenderTexture.active = null;
                System.IO.File.WriteAllBytes(@"D:\butts.png", tempNormalTexture.EncodeToPNG());


                
                var description = new SharpDX.Direct3D11.Texture2DDescription() {

                    MipLevels = 1,
                    ArraySize = 1,
                    SampleDescription = new SampleDescription() { Count = 1 },

                    Width = renderTexture.width,
                    Height = renderTexture.height,

                    BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource | SharpDX.Direct3D11.BindFlags.RenderTarget,
                    CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                    Format = Format.R8G8B8A8_UNorm,
                    OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.None,
                    Usage = SharpDX.Direct3D11.ResourceUsage.Default
                };
                var tex = new SharpDX.Direct3D11.Texture2D(unityRenderer, description);
                                                
                var viewDesc = new SharpDX.Direct3D11.ShaderResourceViewDescription() {
                    Format = Format.R8G8B8A8_UNorm,
                    Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                    Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource() {
                        MipLevels = 1,
                        MostDetailedMip = 0
                    }
                };

                var goodTexture = new SharpDX.Direct3D11.Texture2D(tempNormalTexture.GetNativeTexturePtr());
              
                unityRenderer.ImmediateContext.CopyResource(goodTexture, tex);
                unityRenderer.ImmediateContext.Flush();

                var srv = new SharpDX.Direct3D11.ShaderResourceView(unityRenderer, tex, viewDesc);               
                temp = Texture2D.CreateExternalTexture(renderTexture.width, renderTexture.height, TextureFormat.ARGB32, false, false, srv.NativePointer);
                
                System.IO.File.WriteAllBytes(@"D:\butts2.png", temp.EncodeToPNG());                
            }


            TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[3];
            TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[3];
            var posesError = OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
            if(posesError != EVRCompositorError.None) {
                ModConsole.Error("Error in WaitGetPoses");
                ModConsole.Print(posesError);
            }

            OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseStanding);
            OpenVR.Compositor.CompositorBringToFront();

            VRTextureBounds_t bounds = new VRTextureBounds_t();
            bounds.vMin = bounds.uMin = 0;
            bounds.vMax = bounds.uMax = 1;
            var compError = OpenVR.Compositor.Submit(EVREye.Eye_Left, ref leftEyeTexture, ref bounds, EVRSubmitFlags.Submit_Default);
            if(compError != EVRCompositorError.None) {
                ModConsole.Error("Compositor left error");
                ModConsole.Print(compError);
                if(compError == EVRCompositorError.TextureUsesUnsupportedFormat) {
                    ModConsole.Print("You probably need to enable -force-d3d11 launch option in Steam");
                }
                hadError = compError != EVRCompositorError.DoNotHaveFocus;
            }
            compError = OpenVR.Compositor.Submit(EVREye.Eye_Right, ref rightEyeTexture, ref bounds, EVRSubmitFlags.Submit_Default);
            if (compError != EVRCompositorError.None) {
                ModConsole.Print("Compositor right error");
                ModConsole.Print(compError);
                hadError = compError != EVRCompositorError.DoNotHaveFocus;
            }

            OpenVR.Compositor.PostPresentHandoff();

            //Compositor_FrameTiming timing = new Compositor_FrameTiming();
            //OpenVR.Compositor.GetFrameTiming(ref timing, 0);
            //ModConsole.Print(timing.m_nNumFramePresents);
        }
        
        // Update is called once per frame
        public override void Update() {            
            
        }       

        bool OpenVRInit() {
            var error = EVRInitError.None;
            vrSystem = OpenVR.Init(ref error, EVRApplicationType.VRApplication_Scene);
            if (error != EVRInitError.None || !OpenVR.IsHmdPresent()) {
                ModConsole.Error("Error in VR init");
                ModConsole.Print(error);
                return false;
            }

            /*var helper = new GameObject("OpenVRHelper");
            helper.AddComponent<OpenVRHelper>();*/
            return true;
        }

        ~mscvr() {
            OpenVR.Shutdown();
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
