Shader "Custom/Glow" {
Properties {
	_Strength("Strength", Range(0,1)) = 1
	_Gradient("Gradient", 2D) = "white" {}
	_Color1("Color", Color) = (1,1,1,1)
	_Color2("Color2", Color) = (1,1,1,1)
	_MainTex ("Particle Texture", 2D) = "white" {}
	_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	_FadePower("Edge Exponent", Range(0.01, 20.0)) = 1.0
	_DepthPower("Soft Exponent", Range(0.01, 20.0)) = 1.0
}

Category {
	Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" }
	Blend SrcAlpha One
	Cull Back Lighting Off ZWrite Off
	
	SubShader {
		Pass {
		
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma target 2.0
			#pragma multi_compile_particles
			#pragma multi_compile_fog

			#include "UnityCG.cginc"

			sampler2D _MainTex;
			sampler2D _Gradient;
			fixed4 _TintColor;
			half4 _Color1;
			half4 _Color2;
			
			struct appdata_t {
				float4 vertex : POSITION;
				float3 normal : NORMAL;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_VERTEX_INPUT_INSTANCE_ID
			};

			struct v2f {
				float3 viewDir : TEXCOORD1;
				float3 normal : TEXCOORD3;
				float4 vertex : SV_POSITION;
				fixed4 color : COLOR;
				float2 texcoord : TEXCOORD0;
				UNITY_FOG_COORDS(1)
				
				float4 projPos : TEXCOORD2;
				
				UNITY_VERTEX_OUTPUT_STEREO
			};
			
			float4 _MainTex_ST;

			v2f vert (appdata_t v)
			{
				v2f o;
				UNITY_SETUP_INSTANCE_ID(v);
				UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
				o.vertex = UnityObjectToClipPos(v.vertex);
				
				o.projPos = ComputeScreenPos (o.vertex);
				COMPUTE_EYEDEPTH(o.projPos.z);
				
				o.color = v.color;
				o.texcoord = TRANSFORM_TEX(v.texcoord,_MainTex);
				o.viewDir = WorldSpaceViewDir(v.vertex);
				o.normal = UnityObjectToWorldNormal(v.normal);
				UNITY_TRANSFER_FOG(o,o.vertex);

				return o;
			}

			sampler2D_float _CameraDepthTexture;
			float _InvFade;
			float _FadePower;
			float _DepthPower;
			float _Strength;
			fixed4 frag(v2f i) : SV_Target
			{
				//i.color = float4(1, 1, 1, 1);

				fixed4 col = _Color1;
				
				//i.color.a = 1;
				float sceneZ = clamp(0.04 + LinearEyeDepth (SAMPLE_DEPTH_TEXTURE_PROJ(_CameraDepthTexture, UNITY_PROJ_COORD(i.projPos))), -100, 100);
				float partZ = i.projPos.z;
				float fade = saturate (pow(_InvFade * (sceneZ-partZ), _DepthPower));
				//col.a = fade;
				col.a = fade;
				float adot = dot(normalize(i.viewDir), normalize(i.normal));
				col.a *= clamp(pow(adot, _FadePower), 0, 1);

				
				col.rgb = tex2D(_Gradient, float2(1 - clamp(adot*0.9, 0.01, 0.99), 0.5));

				//col.rgb = lerp(_Color1, _Color2, 1 - pow(adot, 20));
				
				//fixed4 col = lerp(half4(1,1,1,1), prev, prev.a);
				//UNITY_APPLY_FOG_COLOR(i.fogCoord, col, fixed4(1,1,1,1)); // fog towards white due to our blend mode
				//return partZ;
				_Strength *= clamp((i.projPos.z/2), 0, 1);
				
				col.a *= _Strength;
				return col;

			}
			ENDCG 
		}
	}
}
}
