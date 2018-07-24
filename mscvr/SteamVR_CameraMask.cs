//========= Copyright 2014, Valve Corporation, All rights reserved. ===========
//
// Purpose: Masks out pixels that cannot be seen through the connected hmd.
//
//=============================================================================

using UnityEngine;
using System.Collections;
using UnityEngine.Rendering;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class SteamVR_CameraMask : MonoBehaviour {
    static Material material;
    static Mesh[] hiddenAreaMeshes = new Mesh[] { null, null };

    MeshFilter meshFilter;

    void Awake() {
        meshFilter = GetComponent<MeshFilter>();

        if (material == null)
            material = new Material(@"
Shader ""Custom / SteamVR_HiddenArea"" {


    CGINCLUDE

    # include ""UnityCG.cginc""

    float4 vert(appdata_base v) : SV_POSITION { return v.vertex;
    }
    float4 frag(float4 v : SV_POSITION) : COLOR { return float4(0,0,0,0);
}

ENDCG

SubShader {
    Tags { ""Queue"" = ""Background"" }
    Pass {
        ZTest Always Cull Off ZWrite On

            Fog { Mode Off }

        CGPROGRAM
#pragma vertex vert
#pragma fragment frag
            ENDCG

        }
}
}
");


        //material = new Material(Shader.Find("Custom/SteamVR_HiddenArea"));

        var mr = GetComponent<MeshRenderer>();
        mr.material = material;
        mr.shadowCastingMode = ShadowCastingMode.Off;
        mr.receiveShadows = false;

		mr.useLightProbes = false;

        mr.reflectionProbeUsage = ReflectionProbeUsage.Off;
    }

    public void Set(SteamVR vr, Valve.VR.EVREye eye) {
        int i = (int)eye;
        if (hiddenAreaMeshes[i] == null)
            hiddenAreaMeshes[i] = SteamVR_Utils.CreateHiddenAreaMesh(vr.hmd.GetHiddenAreaMesh(eye, Valve.VR.EHiddenAreaMeshType.k_eHiddenAreaMesh_Standard), vr.textureBounds[i]);
        meshFilter.mesh = hiddenAreaMeshes[i];
    }

    public void Clear() {
        meshFilter.mesh = null;
    }
}