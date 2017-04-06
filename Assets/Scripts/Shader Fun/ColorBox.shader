Shader "Custom/ColorBox" {
	Properties{
		_Color1("Color1", Color) = (1,1,1,1)
		_Color1Dir("Color1 Direction", Vector) = (1, 1, 1)
		_Color2("Color2", Color) = (1,1,1,1)
		_Color2Dir("Color2 Direction", Vector) = (1, 1, 1)
		_Color3("Color3", Color) = (1,1,1,1)
		_Color3Dir("Color3 Direction", Vector) = (1, 1, 1)

		_Factor("Factor", Range(0, 20)) = 1
		_Additive("Additive", Range(0, 4)) = 1
		_Multiplicative("Multiplicative", Range(0, 4)) = 1
		_Alpha("Alpha", Range(0, 1)) = 1
	}
	SubShader{
		Tags{ "Queue" = "Background" "RenderType" = "Transparent" }
		LOD 200
		Cull Off
		ZWrite On
		ZTest Less
		Lighting Off
		Blend One One

		CGPROGRAM
		// Physically based Standard lighting model, and enable shadows on all light types
		#pragma surface surf Mine
		//#pragma vertex vert
		//#pragma fragment frag
		
		float4 LightingMine(SurfaceOutput s, float3 lightDir, float atten) {
			float4 c;
			c.rgb = s.Albedo;
			c.a = s.Alpha;
			return c;
		}
		
		// Use shader model 3.0 target, to get nicer looking lighting
		#pragma target 4.0

		sampler2D _MainTex;
		//uniform sampler2D _CameraDepthTexture;
		struct Input {
			float2 uv_MainTex;
			float3 worldPos;
		};

		float _Factor;
		float _Additive;
		float _Multiplicative;
		float3 _Color1;
		float3 _Color1Dir;
		float3 _Color2;
		float3 _Color2Dir;
		float3 _Color3;
		float3 _Color3Dir;
		float _Alpha;
	
		
		void surf(Input IN, inout SurfaceOutput o) {
			float3 localPixel = normalize(IN.worldPos - _WorldSpaceCameraPos);
			o.Albedo = clamp(_Color1 * (pow(clamp(dot(localPixel, normalize(_Color1Dir)), 0, 1), _Factor)+_Additive), 0, 2);
			o.Albedo += clamp(_Color2 * (pow(clamp(dot(localPixel, normalize(_Color2Dir)), 0, 1), _Factor) + _Additive), 0, 2);
			o.Albedo += clamp(_Color3 * (pow(clamp(dot(localPixel, normalize(_Color3Dir)), 0, 1), _Factor) + _Additive), 0, 2);

			o.Albedo *= _Alpha;
			o.Alpha = 1;

		}
		
		

		ENDCG
	}
	FallBack "Diffuse"
}