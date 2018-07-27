using UnityEngine;
using System.Collections;
using System;
using MSCLoader;
using SharpDX.DXGI;
using Valve.VR;

namespace mscvr {    
    public class VRRenderer : IDisposable {
        private static string blitShader = @"
Shader ""Custom/SteamVR_Blit"" {
	Properties { _MainTex(""Base (RGB)"", 2D) = ""white"" {} }

	CGINCLUDE

# include ""UnityCG.cginc""

    sampler2D _MainTex;

	struct v2f {
    float4 pos : SV_POSITION;
		float2 tex : TEXCOORD0;
	};

v2f vert(appdata_base v) {
    v2f o;
    o.pos = v.vertex;
    o.tex = v.texcoord;
    return o;
}

float4 frag(v2f i) : COLOR {
		return tex2D(_MainTex, i.tex);
	}

	float4 frag_linear(v2f i) : COLOR {
		return pow(tex2D(_MainTex, i.tex), 1.0 / 2.2);
	}

	ENDCG

    SubShader {
    Pass {
        ZTest Always Cull Off ZWrite Off

            Fog { Mode Off }

        CGPROGRAM
#pragma vertex vert
#pragma fragment frag
            ENDCG

        }
    Pass {
        ZTest Always Cull Off ZWrite Off

            Fog { Mode Off }

        CGPROGRAM
#pragma vertex vert
#pragma fragment frag_linear
            ENDCG

        }
}
}

";

        private VRRig rig;
        private SharpDX.Direct3D11.Device device;
        private SharpDX.Direct3D11.Device unityRenderer;
        private VRTextureBounds_t bounds;
        private SharpDX.Direct3D11.Texture2D leftVRTexture;
        private SharpDX.Direct3D11.Texture2D rightVRTexture;
        private Texture_t leftEye;
        private Texture_t rightEye;
        private SharpDX.Direct3D11.Texture2D leftHandle;
        private SharpDX.Direct3D11.Texture2D rightHandle;
        private Texture2D leftUnityTexture;
        private Texture2D rightUnityTexture;
        private Material leftMat;
        private Material rightMat;
        private bool hadError;

        public VRRenderer(SharpDX.Direct3D11.Device d3d11Device, SharpDX.Direct3D11.Device unityDevice, VRRig rig) {
            this.rig = rig;
            device = d3d11Device;
            unityRenderer = unityDevice;

            bounds = new VRTextureBounds_t();
            bounds.vMin = bounds.uMin = 0;
            bounds.vMax = bounds.uMax = 1;

            ModConsole.Print("Creating description");
            var w = rig.leftTexture.Description.Width;
            var h = rig.leftTexture.Description.Height;
            var description = new SharpDX.Direct3D11.Texture2DDescription() {
                Width = (int)w,
                Height = (int)h,

                MipLevels = 1,
                ArraySize = 1,

                SampleDescription = new SampleDescription() { Count = 1 },

                BindFlags = SharpDX.Direct3D11.BindFlags.ShaderResource | SharpDX.Direct3D11.BindFlags.RenderTarget,
                CpuAccessFlags = SharpDX.Direct3D11.CpuAccessFlags.None,
                Format = Format.R8G8B8A8_UNorm,
                OptionFlags = SharpDX.Direct3D11.ResourceOptionFlags.Shared,
                Usage = SharpDX.Direct3D11.ResourceUsage.Default
            };
            
            System.Diagnostics.Trace.WriteLine("Creating D3D11 Textures");

            leftVRTexture = new SharpDX.Direct3D11.Texture2D(d3d11Device, description);            
            rightVRTexture = new SharpDX.Direct3D11.Texture2D(d3d11Device, description);            

            leftEye = new Texture_t() {
                eColorSpace = EColorSpace.Gamma,
                eType = ETextureType.DirectX,
                handle = leftVRTexture.NativePointer
            };
            rightEye = new Texture_t() {
                eColorSpace = EColorSpace.Gamma,
                eType = ETextureType.DirectX,
                handle = rightVRTexture.NativePointer
            };


            var sharedHandleLeft = leftVRTexture.QueryInterface<Resource>().SharedHandle;
            var sharedHandleRight = rightVRTexture.QueryInterface<Resource>().SharedHandle;

            leftHandle = unityRenderer.OpenSharedResource<SharpDX.Direct3D11.Texture2D>(sharedHandleLeft);
            rightHandle = unityRenderer.OpenSharedResource<SharpDX.Direct3D11.Texture2D>(sharedHandleRight);

            var viewDesc = new SharpDX.Direct3D11.ShaderResourceViewDescription() {
                Format = Format.R8G8B8A8_UNorm,
                Dimension = SharpDX.Direct3D.ShaderResourceViewDimension.Texture2D,
                Texture2D = new SharpDX.Direct3D11.ShaderResourceViewDescription.Texture2DResource() {
                    MipLevels = 1,
                    MostDetailedMip = 0
                }
            };
            var leftView = new SharpDX.Direct3D11.ShaderResourceView(unityDevice, leftHandle, viewDesc);
            var rightView = new SharpDX.Direct3D11.ShaderResourceView(unityDevice, rightHandle, viewDesc);

            leftUnityTexture = Texture2D.CreateExternalTexture(w, h, TextureFormat.ARGB32, false, false, leftView.NativePointer);
            rightUnityTexture = Texture2D.CreateExternalTexture(w, h, TextureFormat.ARGB32, false, false, rightView.NativePointer);
            
            /*leftMat = new Material(blitShader);
            leftMat.SetTexture(0, leftUnityTexture);
            
            rightMat = new Material(blitShader);
            rightMat.SetTexture(0, rightUnityTexture);*/
        }

        public void Render() {
            if (hadError) {
                return;
            }

            System.Diagnostics.Trace.WriteLine("Attempting to copy resource");

            //unityRenderer.ImmediateContext.CopyResource(rig.leftTexture, leftHandle);
            //unityRenderer.ImmediateContext.CopyResource(rig.rightTexture, rightHandle);

            //unityRenderer.ImmediateContext.Flush();

            //Potential methods to speed this up:
            //1. Graphics.Blit
            //2. Using same method as SteamVR_Camera, eg. use material with the steamvr blit shader
            //      to which you then assign each texture at a time and render into it

            RenderTexture.active = rig.leftRt;
            leftUnityTexture.ReadPixels(new Rect(0, 0, leftUnityTexture.width, leftUnityTexture.height), 0, 0);                       
            RenderTexture.active = rig.rightRt;
            rightUnityTexture.ReadPixels(new Rect(0, 0, rightUnityTexture.width, rightUnityTexture.height), 0, 0);
            RenderTexture.active = null;
            leftUnityTexture.Apply();
            rightUnityTexture.Apply();

            //Graphics.Blit(rig.leftRt, leftMat);
            //Graphics.Blit(rig.rightRt, rightMat);

            System.Diagnostics.Trace.WriteLine("Finished copy resource?");

            TrackedDevicePose_t[] renderPoses = new TrackedDevicePose_t[3];
            TrackedDevicePose_t[] gamePoses = new TrackedDevicePose_t[3];
            var posesError = OpenVR.Compositor.WaitGetPoses(renderPoses, gamePoses);
            if (posesError != EVRCompositorError.None) {
                ModConsole.Error("Error in WaitGetPoses");
                ModConsole.Print(posesError);
            }

            OpenVR.Compositor.SetTrackingSpace(ETrackingUniverseOrigin.TrackingUniverseStanding);
            OpenVR.Compositor.CompositorBringToFront();
            
            var compError = OpenVR.Compositor.Submit(EVREye.Eye_Left, ref leftEye, ref bounds, EVRSubmitFlags.Submit_Default);
            if (compError != EVRCompositorError.None) {
                ModConsole.Error("Compositor left error");
                ModConsole.Print(compError);
                if (compError == EVRCompositorError.TextureUsesUnsupportedFormat) {
                    ModConsole.Print("You probably need to enable -force-d3d11 launch option in Steam");
                }
                hadError = compError != EVRCompositorError.DoNotHaveFocus;
            }
            compError = OpenVR.Compositor.Submit(EVREye.Eye_Right, ref rightEye, ref bounds, EVRSubmitFlags.Submit_Default);
            if (compError != EVRCompositorError.None) {
                ModConsole.Print("Compositor right error");
                ModConsole.Print(compError);
                hadError = compError != EVRCompositorError.DoNotHaveFocus;
            }

            OpenVR.Compositor.PostPresentHandoff();
        }

        public void Dispose() {
            leftVRTexture.Dispose();
            rightVRTexture.Dispose();

            leftHandle.Dispose();
            rightHandle.Dispose();
        }
    }
}
