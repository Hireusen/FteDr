Shader "Custom/CylinderWallShader"
{
    Properties
    {
        [HDR] _Color ("Main Color (HDR)", Color) = (0, 0.5, 1, 1)
        _MainTex ("Noise Texture", 2D) = "white" {}
        _ScrollSpeed ("Scroll Speed (X, Y)", Vector) = (0, -0.2, 0, 0)
        _FresnelPower ("Fresnel Power", Range(0.1, 10.0)) = 2.0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }
        LOD 100
        Cull Off 
        ZWrite Off 
        Blend SrcAlpha One 

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
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewDir : TEXCOORD1;
                float3 normal : NORMAL;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _ScrollSpeed;
            float _FresnelPower;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                
                // 텍스처 스크롤 애니메이션 (시간에 따라 UV 이동)
                o.uv = TRANSFORM_TEX(v.uv, _MainTex) + _Time.y * _ScrollSpeed.xy;
                
                // 프레넬 계산을 위한 월드 기준 시선 벡터와 노멀
                o.viewDir = normalize(ObjSpaceViewDir(v.vertex));
                o.normal = normalize(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // 양면(Cull Off) 렌더링 시, 안쪽 면을 볼 때 노멀이 뒤집혀 프레넬이 깨지는 것을 방지 (절댓값 처리)
                float dotProduct = dot(i.normal, i.viewDir);
                float fresnel = saturate(1.0 - abs(dotProduct));
                fresnel = pow(fresnel, _FresnelPower);

                // 노이즈 텍스처 샘플링
                fixed4 texColor = tex2D(_MainTex, i.uv);
                
                // 최종 컬러 계산 (색상 * 텍스처 * 프레넬 외곽선 강도)
                fixed4 finalColor = _Color * texColor * fresnel;
                
                // Additive 블렌딩이므로 알파값은 투명도이자 발광 강도로 작용
                finalColor.a = _Color.a * texColor.a * fresnel;
                
                return finalColor;
            }
            ENDCG
        }
    }
}