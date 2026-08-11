// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Stylized Dynamic Coral"
{
	Properties
	{
		[HDR]_TopColor("Top Color", Color) = (0.3160377,1,0.695684,1)
		[HDR]_MainColor("Main Color", Color) = (0.3160377,1,0.695684,1)
		[HDR]_Emmision("Emmision", Color) = (0,0,0,1)
		_MainWaveAmount("Main Wave Amount", Float) = 0.3
		_WaveSpeed("Wave Speed", Float) = 0.5
		_MainWaveScale("Main Wave Scale", Float) = 1
		_GradientSmoothness1("Gradient Smoothness", Float) = 0.5
		_WaveHeightMultiplier("Wave Height Multiplier", Float) = 1
		_FlutterAmount("Flutter Amount", Float) = 0.3
		_GradientOffset1("Gradient Offset", Float) = 0
		_FlutterSpeed("Flutter Speed", Float) = 0.5
		_FlutterScale("Flutter Scale", Float) = 1
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry+0" "IsEmissive" = "true"  }
		Cull Back
		CGPROGRAM
		#include "UnityShaderVariables.cginc"
		#pragma target 3.0
		#pragma instancing_options forwardadd
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc instancing 
		
		struct Input
		{
			float3 worldPos;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		uniform float _FlutterAmount;
		uniform float _FlutterSpeed;
		uniform float _FlutterScale;
		uniform float _MainWaveAmount;
		uniform float _WaveHeightMultiplier;
		uniform float _WaveSpeed;
		uniform float _MainWaveScale;
		uniform float4 _MainColor;
		uniform float4 _TopColor;
		uniform float _GradientOffset1;
		uniform float _GradientSmoothness1;
		uniform float4 _Emmision;

		float snoise( float3 v ) {
			v *= 6.0; 
			float x = sin(v.x * 1.3 + v.z * 0.8) * cos(v.y * 1.5 - v.x * 0.5);
			float y = cos(v.x * 0.9 + v.y * 1.1) * sin(v.z * 1.4 - v.y * 0.7);
			return (x + y) * 0.5;
		}

		float snoise( float2 v ) {
			v *= 6.0; 
			return sin(v.x * 1.3 + v.y * 0.8) * cos(v.x * 0.9 - v.y * 1.1);
		}

		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT( Input, o );
			UNITY_TRANSFER_INSTANCE_ID(v, o);

			float3 ase_worldPos = mul( unity_ObjectToWorld, v.vertex );
			float temp_output_59_0 = ( _FlutterSpeed * _Time.y );
			float3 appendResult63 = (float3(temp_output_59_0 , temp_output_59_0 , temp_output_59_0));
			float3 temp_output_64_0 = ( ase_worldPos + appendResult63 );
			float temp_output_62_0 = ( 1.0 / _FlutterScale );
			float simplePerlin3D70 = snoise( temp_output_64_0*temp_output_62_0 );
			float simplePerlin3D69 = snoise( temp_output_64_0*( temp_output_62_0 * 0.5 ) );
			float3 appendResult67 = (float3(simplePerlin3D70 , 0.0 , simplePerlin3D69));
			float4 transform52 = mul(unity_ObjectToWorld,float4( 0,0,0,1 ));
			float3 ase_vertex3Pos = v.vertex.xyz;
			float3 appendResult51 = (float3(transform52.x , ( ase_vertex3Pos.y * _WaveHeightMultiplier ) , transform52.z));
			float temp_output_36_0 = ( _WaveSpeed * _Time.y );
			float3 appendResult38 = (float3(temp_output_36_0 , temp_output_36_0 , temp_output_36_0));
			float3 temp_output_39_0 = ( appendResult51 + appendResult38 );
			float temp_output_16_0 = ( 1.0 / _MainWaveScale );
			float simplePerlin2D7 = snoise( temp_output_39_0.xy*temp_output_16_0 );
			float simplePerlin3D8 = snoise( temp_output_39_0*( temp_output_16_0 * 0.8 ) );
			float3 appendResult17 = (float3(simplePerlin2D7 , 0.0 , simplePerlin3D8));
			float clampResult42 = clamp( ase_vertex3Pos.y , 0.0 , 100000.0 );
			v.vertex.xyz += ( float4( ( ( _FlutterAmount * appendResult67 ) + ( _MainWaveAmount * float3( 0.01,0.01,0.01 ) * appendResult17 * clampResult42 ) ) , 0.0 ) * v.color ).rgb;
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			UNITY_SETUP_INSTANCE_ID(i);

			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 lerpResult44 = lerp( _MainColor , _TopColor , saturate( ( ( ase_vertex3Pos.y - _GradientOffset1 ) * _GradientSmoothness1 ) ));
			o.Albedo = lerpResult44.rgb;
			o.Emission = ( lerpResult44 * _Emmision ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}