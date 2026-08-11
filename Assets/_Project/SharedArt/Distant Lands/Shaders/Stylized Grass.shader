// Upgrade NOTE: upgraded instancing buffer 'DistantLandsStylizedGrass' to new syntax.
// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Stylized Grass"
{
	Properties
	{
		_GrassTexture("Grass Texture", 2D) = "white" {}
		_AlphaClip("Alpha Clip", Float) = 0
		_TopColor("Top Color", Color) = (0.359336,0.8018868,0.5062882,0)
		_BottomColor("Bottom Color", Color) = (0.359336,0.8018868,0.5062882,0)
		_GradientAmount("Gradient Amount", Float) = 0
		_WindScale("Wind Scale", Float) = 0
		_WindSpeed("Wind Speed", Float) = 0
		_WindStrength("Wind Strength", Vector) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" }
		Cull Off
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma multi_compile_instancing
		#pragma instancing_options forwardadd
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc 
		struct Input
		{
			float3 worldPos;
			float4 vertexColor : COLOR;
			float2 uv_texcoord;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		uniform float _WindSpeed;
		uniform float _WindScale;
		uniform float4 _BottomColor;
		uniform float4 _TopColor;
		uniform float _GradientAmount;
		uniform sampler2D _GrassTexture;
		uniform float _AlphaClip;

		UNITY_INSTANCING_BUFFER_START(DistantLandsStylizedGrass)
			UNITY_DEFINE_INSTANCED_PROP(float4, _GrassTexture_ST)
#define _GrassTexture_ST_arr DistantLandsStylizedGrass
			UNITY_DEFINE_INSTANCED_PROP(float3, _WindStrength)
#define _WindStrength_arr DistantLandsStylizedGrass
		UNITY_INSTANCING_BUFFER_END(DistantLandsStylizedGrass)

		float SimpleNoise(float2 UV) {
			float x = sin(UV.x * 2.1 + UV.y * 1.3);
			float y = cos(UV.x * 1.2 - UV.y * 1.9);
			return (x + y) * 0.25 + 0.5; 
		}

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT( Input, o );
			UNITY_TRANSFER_INSTANCE_ID(v, o);

			float3 _WindStrength_Instance = UNITY_ACCESS_INSTANCED_PROP(_WindStrength_arr, _WindStrength);
			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float mulTime11 = _Time.y * 3.0;
			float2 uv_TexCoord17 = v.texcoord.xy + ( ase_worldPos + ( _WindSpeed * mulTime11 * 3.0 ) ).xy;
			float simpleNoise21 = SimpleNoise( uv_TexCoord17*_WindScale );
			simpleNoise21 = simpleNoise21*2 - 1;
			float4 transform25 = mul(unity_WorldToObject,float4( ( _WindStrength_Instance * simpleNoise21 * v.color.r ) , 0.0 ));
			v.vertex.xyz += transform25.xyz;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			UNITY_SETUP_INSTANCE_ID(i);

			float4 temp_cast_0 = (_GradientAmount).xxxx;
			float4 lerpResult40 = lerp( _BottomColor , _TopColor , saturate( pow( i.vertexColor , temp_cast_0 ) ));
			float4 _GrassTexture_ST_Instance = UNITY_ACCESS_INSTANCED_PROP(_GrassTexture_ST_arr, _GrassTexture_ST);
			float2 uv_GrassTexture = i.uv_texcoord * _GrassTexture_ST_Instance.xy + _GrassTexture_ST_Instance.zw;
			float4 tex2DNode29 = tex2D( _GrassTexture, uv_GrassTexture );
			clip( tex2DNode29.a - _AlphaClip);
			o.Albedo = ( lerpResult40 * tex2DNode29 ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}