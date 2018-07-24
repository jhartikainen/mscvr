//========= Copyright 2015, Valve Corporation, All rights reserved. ===========
//
// Purpose: Flips the camera output back to normal for D3D.
//
//=============================================================================

using UnityEngine;
using System.Collections;

public class SteamVR_CameraFlip : MonoBehaviour {
    static Material blitMaterial;

    void OnEnable() {
        if (blitMaterial == null)
            blitMaterial = new Material(@"Shader ""Custom / SteamVR_BlitFlip"" {
    Properties { _MainTex(""Base (RGB)"", 2D) = ""white"" { }
            }


    CGINCLUDE

# include ""UnityCG.cginc""

    sampler2D _MainTex;


    struct v2f {
        float4 pos : SV_POSITION;
		float2 tex : TEXCOORD0;
	};

    v2f vert(appdata_base v) {
        v2f o;
        o.pos = mul(UNITY_MATRIX_MVP, v.vertex);
        o.tex.x = v.texcoord.x;
        o.tex.y = 1 - v.texcoord.y;
        return o;
    }

    float4 frag(v2f i) : COLOR {
		return tex2D(_MainTex, i.tex);
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
}
}");
                
                //blitMaterial = new Material(Shader.Find("Custom/SteamVR_BlitFlip"));
    }

    void OnRenderImage(RenderTexture src, RenderTexture dest) {
        Graphics.Blit(src, dest, blitMaterial);
    }
}