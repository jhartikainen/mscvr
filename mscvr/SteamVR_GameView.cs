//========= Copyright 2014, Valve Corporation, All rights reserved. ===========
//
// Purpose: Handles rendering to the game view window
//
//=============================================================================

using UnityEngine;

[RequireComponent(typeof(Camera))]
public class SteamVR_GameView : MonoBehaviour {
    public float scale = 1.5f;
    public bool drawOverlay = true;

    static Material overlayMaterial;

    void OnEnable() {
        if (overlayMaterial == null) {
            //overlayMaterial = new Material(Shader.Find("Custom/SteamVR_Overlay"));

            overlayMaterial = new Material(@"
Shader ""Custom / SteamVR_Overlay"" {

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
        o.pos = v.vertex;
        o.tex = v.texcoord;
        return o;
    }

    float4 frag(v2f i) : COLOR {
		return tex2D(_MainTex, i.tex);
}

float4 frag_linear(v2f i) : COLOR {
		return pow(tex2D(_MainTex, i.tex), 2.2);
	}

	ENDCG

    SubShader {
    Pass {
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always Cull Off ZWrite Off
        Fog { Mode Off }

        CGPROGRAM
#pragma vertex vert
#pragma fragment frag
            ENDCG

        }
    Pass {
        Blend SrcAlpha OneMinusSrcAlpha
        ZTest Always Cull Off ZWrite Off
        Fog { Mode Off }

        CGPROGRAM
#pragma vertex vert
#pragma fragment frag_linear
            ENDCG

        }
}
}
");
        }
    }

    void OnPostRender() {
        var vr = SteamVR.instance;
        var camera = GetComponent<Camera>();
        var aspect = scale * camera.aspect / vr.aspect;

        var x0 = -scale;
        var x1 = scale;
        var y0 = aspect;
        var y1 = -aspect;

        var blitMaterial = SteamVR_Camera.blitMaterial;
        blitMaterial.mainTexture = SteamVR_Camera.GetSceneTexture(camera.hdr);

        GL.PushMatrix();
        GL.LoadOrtho();
#if !(UNITY_5_0)
        blitMaterial.SetPass(0);
#else
		blitMaterial.SetPass(QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
#endif
        GL.Begin(GL.QUADS);
        GL.TexCoord2(0.0f, 0.0f); GL.Vertex3(x0, y0, 0);
        GL.TexCoord2(1.0f, 0.0f); GL.Vertex3(x1, y0, 0);
        GL.TexCoord2(1.0f, 1.0f); GL.Vertex3(x1, y1, 0);
        GL.TexCoord2(0.0f, 1.0f); GL.Vertex3(x0, y1, 0);
        GL.End();
        GL.PopMatrix();

        var overlay = SteamVR_Overlay.instance;
        if (overlay && overlay.texture && overlayMaterial && drawOverlay) {
            var texture = overlay.texture;
            overlayMaterial.mainTexture = texture;

            var u0 = 0.0f;
            var v0 = 1.0f - (float)Screen.height / texture.height;
            var u1 = (float)Screen.width / texture.width;
            var v1 = 1.0f;

            GL.PushMatrix();
            GL.LoadOrtho();
#if !(UNITY_5_0)
            overlayMaterial.SetPass(QualitySettings.activeColorSpace == ColorSpace.Linear ? 1 : 0);
#else
			overlayMaterial.SetPass(0);
#endif
            GL.Begin(GL.QUADS);
            GL.TexCoord2(u0, v0); GL.Vertex3(-1, -1, 0);
            GL.TexCoord2(u1, v0); GL.Vertex3(1, -1, 0);
            GL.TexCoord2(u1, v1); GL.Vertex3(1, 1, 0);
            GL.TexCoord2(u0, v1); GL.Vertex3(-1, 1, 0);
            GL.End();
            GL.PopMatrix();
        }
    }
}