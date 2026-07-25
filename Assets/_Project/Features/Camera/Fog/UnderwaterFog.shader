Shader "Hidden/UnderwaterFog"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _FogColor ("Fog Color", Color) = (0.08, 0.28, 0.4, 1)
        _FogStart ("Fog Start (m)", Float) = 15
        _FogEnd ("Fog End (m)", Float) = 60
        _FogMaxDensity ("Fog Max Density", Range(0,1)) = 1
    }
    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            sampler2D _CameraDepthTexture; // 카메라가 DepthTextureMode.Depth일 때 자동 제공
            float4 _FogColor;
            float _FogStart;
            float _FogEnd;
            float _FogMaxDensity;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);

                // 깊이 텍스처에서 이 픽셀의 카메라 기준 선형 거리(뷰 공간 z)를 구한다.
                float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv);
                float linearDepth = LinearEyeDepth(rawDepth); // 카메라로부터의 실제 거리(m)

                // Start~End 사이를 0~1로 매핑. Start 이내는 안개 없음, End 이상은 최대.
                float fogFactor = saturate((linearDepth - _FogStart) / max(0.001, _FogEnd - _FogStart));
                fogFactor *= _FogMaxDensity;

                // 먼 픽셀일수록 안개색으로 섞는다.
                col.rgb = lerp(col.rgb, _FogColor.rgb, fogFactor);
                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
