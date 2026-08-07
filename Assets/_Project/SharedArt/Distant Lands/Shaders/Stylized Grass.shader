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


		inline float noise_randomValue (float2 uv) { return frac(sin(dot(uv, float2(12.9898, 78.233)))*43758.5453); }
		inline float noise_interpolate (float a, float b, float t) { return (1.0-t)*a + (t*b); }

		inline float valueNoise (float2 uv)
		{
			float2 i = floor(uv);
			float2 f = frac( uv );
			f = f* f * (3.0 - 2.0 * f);
			uv = abs( frac(uv) - 0.5);
			float2 c0 = i + float2( 0.0, 0.0 );
			float2 c1 = i + float2( 1.0, 0.0 );
			float2 c2 = i + float2( 0.0, 1.0 );
			float2 c3 = i + float2( 1.0, 1.0 );
			float r0 = noise_randomValue( c0 );
			float r1 = noise_randomValue( c1 );
			float r2 = noise_randomValue( c2 );
			float r3 = noise_randomValue( c3 );
			float bottomOfGrid = noise_interpolate( r0, r1, f.x );
			float topOfGrid = noise_interpolate( r2, r3, f.x );
			float t = noise_interpolate( bottomOfGrid, topOfGrid, f.y );
			return t;
		}

		float SimpleNoise(float2 UV)
		{
			float t = 0.0;
			float freq = pow( 2.0, float( 0 ) );
			float amp = pow( 0.5, float( 3 - 0 ) );
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(1));
			amp = pow(0.5, float(3-1));
			t += valueNoise( UV/freq )*amp;
			freq = pow(2.0, float(2));
			amp = pow(0.5, float(3-2));
			t += valueNoise( UV/freq )*amp;
			return t;
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