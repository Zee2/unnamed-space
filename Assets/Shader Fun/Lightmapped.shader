Shader "Custom/Lightmapped" {
Properties {
	_Color ("Main Color", Color) = (1,1,1,1)
	_MainTex ("Base (RGB)", 2D) = "white" {}
	_Occlusion("Ambient Occlusion", 2D) = "white" {}
	_OcclusionFactor("Occlusion Factor", Range(0, 5)) = 1
	_LightMap ("Lightmap (RGB)", 2D) = "black" {}
	_LightmapAtten("Lightmap Power", Range(0,5)) = 1
}

SubShader {
	LOD 200
	Tags { "RenderType" = "Opaque"}
CGPROGRAM
#pragma surface surf Standard fullforwardshadows
struct Input {
  float2 uv_MainTex;
  float2 uv2_LightMap;
  
};
sampler2D _MainTex;
sampler2D _LightMap;
sampler2D _Occlusion;
fixed4 _Color;
float _LightmapAtten;
float _OcclusionFactor;
void surf (Input IN, inout SurfaceOutputStandard o)
{
  o.Albedo = tex2D (_MainTex, IN.uv_MainTex).rgb * _Color;
  o.Occlusion = pow(tex2D(_Occlusion, IN.uv_MainTex).r, _OcclusionFactor);
  half4 lm = tex2D (_LightMap, IN.uv2_LightMap);
  o.Emission = _LightmapAtten * lm.rgb * o.Albedo.rgb;
  o.Alpha = 1;
}
ENDCG
}
FallBack "Legacy Shaders/Lightmapped/VertexLit"
}
