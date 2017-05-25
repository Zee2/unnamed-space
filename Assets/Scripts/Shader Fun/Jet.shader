
Shader "Custom/Jet" {
	Properties{
		_Color("Tint Color", Color) = (1,1,1,1)
		_MainTex("Jet Texture (RGB)", 2D) = "white" {}
		_Noise("Noise (A)", 2D) = "white" {}
		_Intensity("Thruster Intensity", Range(0,1)) = 0.5
	}

	Category{
		Tags{ "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
		Blend OneMinusDstColor One
		Cull Back Lighting Off ZWrite Off

		SubShader{
			Pass{

				CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag
				#pragma target 4.0
				
	
				#include "UnityCG.cginc"

				sampler2D _MainTex;
				float4 _MainTex_ST;
				sampler2D _Noise;
				float4 _Noise_ST;
				fixed4 _Color;

				struct appdata_t {
					float4 vertex : POSITION;
					
					float2 uv : TEXCOORD0;
					UNITY_VERTEX_INPUT_INSTANCE_ID
				};

				struct v2f {
					float4 vertex : SV_POSITION;
					float2 uv : TEXCOORD0;
					float4 seed : TEXCOORD1;
					UNITY_VERTEX_OUTPUT_STEREO
				};


				v2f vert(appdata_t v)
				{
					v2f o;
					UNITY_SETUP_INSTANCE_ID(v);
					UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
					o.vertex = UnityObjectToClipPos(v.vertex);
					o.uv = TRANSFORM_TEX(v.uv, _MainTex);
					o.seed = mul(_Object2World, v.vertex);
					return o;
				}


				half _Intensity;
				fixed4 frag(v2f i) : SV_Target
				{
					float4 c = tex2D(_MainTex, i.uv + float2(1-_Intensity + 0.02*sin(50*_Time.w + 5*i.seed.x) + 0.02*sin(60*_Time.w + 5*i.seed.x),0));
					//c *= (1+(sin(20 * _Time.w)/5));
					//c *= (sin(-i.uv.x*20 + 50*_Time.w)/7)+1;
					
					//c *= sin(10*i.uv.y + 5*_Time.w)/8 + 1;
					return c;
				
				}
				ENDCG
			}
		}
	}
}