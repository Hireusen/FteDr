// Made with Amplify Shader Editor
// Available at the Unity Asset Store - http://u3d.as/y3X 
Shader "Distant Lands/Stylized Terrain"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_MetallicTex("MetallicTex", 2D) = "white" {}
		_Color("Color", Color) = (0,0,0,0)
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
	}

	SubShader
	{
		Tags{ "RenderType" = "Opaque"  "Queue" = "Geometry-100" }
		Cull Back
		CGPROGRAM
		#pragma target 3.0
		#include "UnityPBSLighting.cginc"
		#pragma exclude_renderers gles vulkan 
		#pragma surface surf Standard keepalpha addshadow fullforwardshadows instancing 
		
		struct Input
		{
			float2 uv_texcoord;
			UNITY_VERTEX_INPUT_INSTANCE_ID
		};

		uniform float4 _Color;
		uniform sampler2D _MainTex;
		uniform float4 _MainTex_ST;
		uniform sampler2D _MetallicTex;
		uniform float4 _MetallicTex_ST;

		void surf( Input i , inout SurfaceOutputStandard o )
		{
			UNITY_SETUP_INSTANCE_ID(i);

			float2 uv_MainTex = i.uv_texcoord * _MainTex_ST.xy + _MainTex_ST.zw;
			float4 tex2DNode1 = tex2D( _MainTex, uv_MainTex );
			float4 lerpResult6 = lerp( _Color , tex2DNode1 , 1.0);
			o.Albedo = lerpResult6.rgb;
			float2 uv_MetallicTex = i.uv_texcoord * _MetallicTex_ST.xy + _MetallicTex_ST.zw;
			o.Metallic = tex2D( _MetallicTex, uv_MetallicTex ).r;
			o.Smoothness = tex2DNode1.a;
			o.Alpha = 1;
		}

		ENDCG
	}
	Fallback "Diffuse"
}