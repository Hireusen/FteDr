// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Stylized Coral"
{
	Properties
	{
		[HDR]_TopColor1("Top Color", Color) = (0.3160377,1,0.695684,1)
		[HDR]_MainColor1("Main Color", Color) = (0.3160377,1,0.695684,1)
		[HDR]_Emmision1("Emmision", Color) = (0,0,0,1)
		_GradientSmoothness2("Gradient Smoothness", Float) = 0.5
		_GradientOffset2("Gradient Offset", Float) = 0
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
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows instancing
		
		struct Input
		{
			float3 worldPos;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		uniform float4 _MainColor1;
		uniform float4 _TopColor1;
		uniform float _GradientOffset2;
		uniform float _GradientSmoothness2;
		uniform float4 _Emmision1;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			UNITY_SETUP_INSTANCE_ID(i);

			float3 ase_vertex3Pos = mul( unity_WorldToObject, float4( i.worldPos , 1 ) );
			float4 lerpResult9 = lerp( _MainColor1 , _TopColor1 , saturate( ( ( distance( ase_vertex3Pos , float3( 0,0,0 ) ) - _GradientOffset2 ) * _GradientSmoothness2 ) ));
			o.Albedo = lerpResult9.rgb;
			o.Emission = ( lerpResult9 * _Emmision1 ).rgb;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}