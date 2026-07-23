Shader "Hidden/UnderwaterEffect"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _TintColor ("Tint Color", Color) = (0.1, 0.4, 0.55, 1)
        _TintStrength ("Tint Strength", Range(0,1)) = 0.3
        _VignetteColor ("Vignette Color", Color) = (0, 0.05, 0.1, 1)
        _VignetteStrength ("Vignette Strength", Range(0,2)) = 0.6
        _VignetteSoftness ("Vignette Softness", Range(0.01,1)) = 0.5
        _DistortStrength ("Distort Strength", Range(0,0.02)) = 0.003
        _DistortSpeed ("Distort Speed", Range(0,5)) = 1.0
        _DistortScale ("Distort Scale", Range(1,30)) = 8.0
        _Saturation ("Saturation", Range(0,2)) = 0.9
    }
    SubShader
    {
        // 포스트 이펙트: 깊이 테스트/컬링 없이 화면 전체에 1패스로 그린다.
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
            float4 _TintColor;
            float _TintStrength;
            float4 _VignetteColor;
            float _VignetteStrength;
            float _VignetteSoftness;
            float _DistortStrength;
            float _DistortSpeed;
            float _DistortScale;
            float _Saturation;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv;

                // 1) 물결 왜곡: 두 방향 사인파를 겹쳐 화면을 미세하게 일렁이게 한다.
                float t = _Time.y * _DistortSpeed;
                float2 offset;
                offset.x = sin(uv.y * _DistortScale + t) * _DistortStrength;
                offset.y = cos(uv.x * _DistortScale + t * 1.3) * _DistortStrength;
                uv += offset;

                fixed4 col = tex2D(_MainTex, uv);

                // 2) 채도 조정: 수중은 색이 빠져 보이므로 살짝 탈색한다.
                float luminance = dot(col.rgb, float3(0.299, 0.587, 0.114));
                col.rgb = lerp(float3(luminance, luminance, luminance), col.rgb, _Saturation);

                // 3) 물색 틴트: 화면 전체를 물빛 쪽으로 섞는다.
                col.rgb = lerp(col.rgb, _TintColor.rgb, _TintStrength);

                // 4) 비네팅: 화면 중심에서 멀수록 어두운 물색을 덮어 깊이감을 준다.
                float2 center = uv - 0.5;
                float dist = length(center);
                float vig = smoothstep(_VignetteSoftness, _VignetteSoftness - 0.3, dist);
                // vig: 중심(1) → 가장자리(0). 가장자리일수록 비네팅 색을 강하게.
                float vigAmount = (1.0 - vig) * _VignetteStrength;
                col.rgb = lerp(col.rgb, _VignetteColor.rgb, saturate(vigAmount));

                return col;
            }
            ENDCG
        }
    }
    Fallback Off
}
