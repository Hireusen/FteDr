Shader "VFX/VFX_SampleTexByScreenPos"
{
    Properties
    {
        [HDR]_MainCol("MainCol", Color) = (1, 1, 1, 0)
        _MainTex("MainTex", 2D) = "white" {}
        _EffectTexAlphaByA2R("EffectTexAlphaByA2R", Range(0, 1)) = 0
        _EffectTex("EffectTex", 2D) = "white" {}
        _EffectTexMoveSpeed("EffectTexMoveSpeed", Vector) = (0, 0, 0, 0)
        _EffectTexWarpLength("EffectTexWarpLength", Vector) = (0, 0, 0, 0)
        _WarpTexA2R("WarpTexA2R", Range(0, 1)) = 0
        _WarpTex("WarpTex", 2D) = "white" {}
        _WarpTexMoveSpeed("WarpTexMoveSpeed", Vector) = (0, 0, 0, 0)
        _AlphaPower("AlphaPower", Float) = 1
        _AlphaMul("AlphaMul", Float) = 1
        _FinalAlpha("FinalAlpha", Range(0, 1)) = 1
    }
    SubShader
    {
         Tags
            {
                "RenderType" = "Transparent"
                "Queue" = "Transparent"
                "IgnoreProjector" = "True"
            }
        LOD 100

        Pass
        {

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            ZTest Off

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
         
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;              
                
            };

            struct v2f
            {               
                float4 vertex : SV_POSITION;
                float4 screenPos : TEXCOORD0;
            };

            float4 _MainCol;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            float _EffectTexAlphaByA2R;
            sampler2D _EffectTex;
            float4 _EffectTex_ST;
            float2 _EffectTexMoveSpeed;
            float2 _EffectTexWarpLength;

            sampler2D _WarpTex;
            float4 _WarpTex_ST;
            float _WarpTexMoveSpeed;
            float _WarpTexA2R;

            float _AlphaPower, _AlphaMul, _FinalAlpha;

            float2 PolarCoordinates(float2 UV, float2 Center)
            {
                float2 delta = UV - Center;
                float radius = length(delta) * 2 ;
                float angle = atan2(delta.x, delta.y) * 1.0 / 6.28;
                return float2(radius, angle);
            }

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);    
                o.screenPos = ComputeScreenPos(o.vertex);
                return o;
            }

            float4 frag (v2f i) : SV_Target
            {         
                float2 screenUV = i.screenPos.xy / i.screenPos.w;
#if UNITY_UV_STARTS_AT_TOP
                screenUV.y = 1 - screenUV.y;
#endif
                float alpha = tex2D(_MainTex, screenUV* _MainTex_ST.xy+ _MainTex_ST.zw).a;
                alpha = saturate(pow(alpha, _AlphaPower) * _AlphaMul);

                float2 polarUV = PolarCoordinates(screenUV, float2(0.5, 0.5));

                float2 warpTexUV = polarUV * _WarpTex_ST.xy + _WarpTex_ST.zw + _Time.y * _WarpTexMoveSpeed;
                float4 warpRGB = tex2D(_WarpTex, warpTexUV);
                float warpLength = lerp(warpRGB.a, warpRGB.r, _WarpTexA2R);

                float2 effectTexUV = polarUV * _EffectTex_ST.xy + _EffectTex_ST.zw + _Time.y * _EffectTexMoveSpeed+ warpLength* _EffectTexWarpLength;
                float4 effectRGBA = tex2D(_EffectTex, effectTexUV);
                float effect = lerp(effectRGBA.a, effectRGBA.r, _EffectTexAlphaByA2R);

                float4 col = alpha * effect * _MainCol;
                col.a *= _FinalAlpha;
                return col;
            }
            ENDCG
        }
    }
}
