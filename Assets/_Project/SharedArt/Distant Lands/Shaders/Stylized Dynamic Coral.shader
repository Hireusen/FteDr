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


		float3 mod3D289( float3 x ) { return x - floor( x / 289.0 ) * 289.0; }
		float4 mod3D289( float4 x ) { return x - floor( x / 289.0 ) * 289.0; }
		float4 permute( float4 x ) { return mod3D289( ( x * 34.0 + 1.0 ) * x ); }
		float4 taylorInvSqrt( float4 r ) { return 1.79284291400159 - r * 0.85373472095314; }

		float snoise( float3 v )
		{
			const float2 C = float2( 1.0 / 6.0, 1.0 / 3.0 );
			float3 i = floor( v + dot( v, C.yyy ) );
			float3 x0 = v - i + dot( i, C.xxx );
			float3 g = step( x0.yzx, x0.xyz );
			float3 l = 1.0 - g;
			float3 i1 = min( g.xyz, l.zxy );
			float3 i2 = max( g.xyz, l.zxy );
			float3 x1 = x0 - i1 + C.xxx;
			float3 x2 = x0 - i2 + C.yyy;
			float3 x3 = x0 - 0.5;
			i = mod3D289( i);
			float4 p = permute( permute( permute( i.z + float4( 0.0, i1.z, i2.z, 1.0 ) ) + i.y + float4( 0.0, i1.y, i2.y, 1.0 ) ) + i.x + float4( 0.0, i1.x, i2.x, 1.0 ) );
			float4 j = p - 49.0 * floor( p / 49.0 );  
			float4 x_ = floor( j / 7.0 );
			float4 y_ = floor( j - 7.0 * x_ );  
			float4 x = ( x_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 y = ( y_ * 2.0 + 0.5 ) / 7.0 - 1.0;
			float4 h = 1.0 - abs( x ) - abs( y );
			float4 b0 = float4( x.xy, y.xy );
			float4 b1 = float4( x.zw, y.zw );
			float4 s0 = floor( b0 ) * 2.0 + 1.0;
			float4 s1 = floor( b1 ) * 2.0 + 1.0;
			float4 sh = -step( h, 0.0 );
			float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
			float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
			float3 g0 = float3( a0.xy, h.x );
			float3 g1 = float3( a0.zw, h.y );
			float3 g2 = float3( a1.xy, h.z );
			float3 g3 = float3( a1.zw, h.w );
			float4 norm = taylorInvSqrt( float4( dot( g0, g0 ), dot( g1, g1 ), dot( g2, g2 ), dot( g3, g3 ) ) );
			g0 *= norm.x;
			g1 *= norm.y;
			g2 *= norm.z;
			g3 *= norm.w;
			float4 m = max( 0.6 - float4( dot( x0, x0 ), dot( x1, x1 ), dot( x2, x2 ), dot( x3, x3 ) ), 0.0 );
			m = m* m;
			m = m* m;
			float4 px = float4( dot( x0, g0 ), dot( x1, g1 ), dot( x2, g2 ), dot( x3, g3 ) );
			return 42.0 * dot( m, px);
		}

		float3 mod2D289( float3 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		float2 mod2D289( float2 x ) { return x - floor( x * ( 1.0 / 289.0 ) ) * 289.0; }
		float3 permute( float3 x ) { return mod2D289( ( ( x * 34.0 ) + 1.0 ) * x ); }

		float snoise( float2 v )
		{
			const float4 C = float4( 0.211324865405187, 0.366025403784439, -0.577350269189626, 0.024390243902439 );
			float2 i = floor( v + dot( v, C.yy ) );
			float2 x0 = v - i + dot( i, C.xx );
			float2 i1;
			i1 = ( x0.x > x0.y ) ? float2( 1.0, 0.0 ) : float2( 0.0, 1.0 );
			float4 x12 = x0.xyxy + C.xxzz;
			x12.xy -= i1;
			i = mod2D289( i );
			float3 p = permute( permute( i.y + float3( 0.0, i1.y, 1.0 ) ) + i.x + float3( 0.0, i1.x, 1.0 ) );
			float3 m = max( 0.5 - float3( dot( x0, x0 ), dot( x12.xy, x12.xy ), dot( x12.zw, x12.zw ) ), 0.0 );
			m = m * m;
			m = m * m;
			float3 x = 2.0 * frac( p * C.www ) - 1.0;
			float3 h = abs( x ) - 0.5;
			float3 ox = floor( x + 0.5 );
			float3 a0 = x - ox;
			m *= 1.79284291400159 - 0.85373472095314 * ( a0 * a0 + h * h );
			float3 g;
			g.x = a0.x * x0.x + h.x * x0.y;
			g.yz = a0.yz * x12.xz + h.yz * x12.yw;
			return 130.0 * dot( m, g );
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