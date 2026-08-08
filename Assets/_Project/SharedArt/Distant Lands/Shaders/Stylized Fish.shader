// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Stylized Fish"
{
	Properties
	{
		_Atlas("Atlas", 2D) = "white" {}
		_WaveAmount("Wave Amount", Vector) = (0,0,0,0)
		_TimeScale("Time Scale", Vector) = (1,1,0,0)
		_WaveWidth("Wave Width", Vector) = (1,1,0,0)
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
		#pragma instancing_options forwardadd
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows vertex:vertexDataFunc instancing 
		
		struct Input
		{
			float2 uv_texcoord;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		uniform float2 _WaveWidth;
		uniform float2 _TimeScale;
		uniform float2 _WaveAmount;
		uniform sampler2D _Atlas;
		uniform float4 _Atlas_ST;


		float3 RotateAroundAxis( float3 center, float3 original, float3 u, float angle )
		{
			original -= center;
			float C = cos( angle );
			float S = sin( angle );
			float t = 1 - C;
			float m00 = t * u.x * u.x + C;
			float m01 = t * u.x * u.y - S * u.z;
			float m02 = t * u.x * u.z + S * u.y;
			float m10 = t * u.x * u.y + S * u.z;
			float m11 = t * u.y * u.y + C;
			float m12 = t * u.y * u.z - S * u.x;
			float m20 = t * u.x * u.z - S * u.y;
			float m21 = t * u.y * u.z + S * u.x;
			float m22 = t * u.z * u.z + C;
			float3x3 finalMatrix = float3x3( m00, m01, m02, m10, m11, m12, m20, m21, m22 );
			return mul( finalMatrix, original ) + center;
		}


		void vertexDataFunc( inout appdata_full v, out Input o )
		{
			UNITY_SETUP_INSTANCE_ID(v);
			UNITY_INITIALIZE_OUTPUT( Input, o );
			UNITY_TRANSFER_INSTANCE_ID(v, o);

			float3 ase_vertex3Pos = v.vertex.xyz;
			float temp_output_58_0 = abs( ase_vertex3Pos.z );
			float mulTime39 = _Time.y * _TimeScale.x;
			float3 ase_objectScale = float3( length( unity_ObjectToWorld[ 0 ].xyz ), length( unity_ObjectToWorld[ 1 ].xyz ), length( unity_ObjectToWorld[ 2 ].xyz ) );
			float3 temp_output_40_0 = ( ( ase_objectScale * float3( 50,50,50 ) ) + ( 0.0 - temp_output_58_0 ) );
			float3 rotatedValue64 = RotateAroundAxis( float3( 0,0,0 ), ase_vertex3Pos, float3(0,1,0), ( temp_output_58_0 * sin( ( _WaveWidth.x * ( mulTime39 + temp_output_40_0 ) ) ) * _WaveAmount.x ).x );
			float mulTime70 = _Time.y * _TimeScale.y;
			float3 rotatedValue60 = RotateAroundAxis( float3( 0,0,0 ), ase_vertex3Pos, float3(0,0,1), ( _WaveAmount.y * sin( ( _WaveWidth.y * ( mulTime70 + temp_output_40_0 ) ) ) * temp_output_58_0 ).x );
			v.vertex.xyz += ( ( rotatedValue64 - ase_vertex3Pos ) + ( rotatedValue60 - ase_vertex3Pos ) );
			v.vertex.w = 1;
		}

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			UNITY_SETUP_INSTANCE_ID(i);

			float2 uv_Atlas = i.uv_texcoord * _Atlas_ST.xy + _Atlas_ST.zw;
			o.Albedo = tex2D( _Atlas, uv_Atlas ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}