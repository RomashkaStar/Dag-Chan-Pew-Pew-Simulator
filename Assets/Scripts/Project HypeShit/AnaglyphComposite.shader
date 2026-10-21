Shader "Custom/AnaglyphComposite"
{
    Properties
    {
        [PerRendererData] _MainTex ("Left World", 2D) = "black" {}
        _RightTex ("Right World", 2D) = "black" {}
        _LeftHudTex ("Left HUD", 2D) = "black" {}
        _RightHudTex ("Right HUD", 2D) = "black" {}
        _HasStereoHUD ("Enable Stereo HUD", Float) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        Pass
        {
            Cull Off
            ZWrite Off
            ZTest Always
            Blend One Zero

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            sampler2D _MainTex;
            sampler2D _RightTex;
            sampler2D _LeftHudTex;
            sampler2D _RightHudTex;
            float _HasStereoHUD;

            struct appdata { float4 vertex : POSITION; float2 uv : TEXCOORD0; };
            struct v2f { float4 vertex : SV_POSITION; float2 uv : TEXCOORD0; };

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float3 left = tex2D(_MainTex, i.uv).rgb;
                float3 right = tex2D(_RightTex, i.uv).rgb;

                if (_HasStereoHUD > 0.5)
                {
                    float4 hudLeft = tex2D(_LeftHudTex, i.uv);
                    float4 hudRight = tex2D(_RightHudTex, i.uv);

                    // UI RenderTextures are premultiplied by alpha when rendered
                    // over a transparent black background.
                    left = left * (1.0 - hudLeft.a) + hudLeft.rgb;
                    right = right * (1.0 - hudRight.a) + hudRight.rgb;
                }

                return fixed4(left.r, right.g, right.b, 1.0);
            }
            ENDCG
        }
    }
}
