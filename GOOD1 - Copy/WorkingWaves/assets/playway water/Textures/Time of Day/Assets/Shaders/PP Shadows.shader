// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Time of Day/Cloud Shadows"
{
	Properties
	{
		_MainTex ("Base", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "TOD_Base.cginc"
	#include "TOD_Clouds.cginc"

	uniform sampler2D _MainTex;
	uniform sampler2D_float _CameraDepthTexture;

	uniform float4x4 _FrustumCornersWS;
	uniform float4 _MainTex_TexelSize;

	struct v2f
	{
		float4 pos       : SV_POSITION;
		float2 uv        : TEXCOORD0;
		float2 uv_depth  : TEXCOORD1;
		float3 cameraRay : TEXCOORD2;
	};

	v2f vert(appdata_img v)
	{
		v2f o;

		half index = v.vertex.z;
		v.vertex.z = 0.1;

		o.pos = UnityObjectToClipPos(v.vertex);

		o.uv       = v.texcoord.xy;
		o.uv_depth = v.texcoord.xy;

		#if UNITY_UV_STARTS_AT_TOP
		if (_MainTex_TexelSize.y < 0)
			o.uv.y = 1-o.uv.y;
		#endif

		o.cameraRay = _FrustumCornersWS[(int)index];

		return o;
	}

	half4 frag(v2f i) : COLOR
	{
		half4 sceneColor = tex2D(_MainTex, i.uv);

		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv_depth);
		float depth = Linear01Depth(rawDepth);
		float3 cameraToWorldPos = depth * i.cameraRay;

		half a = CloudShadow(cameraToWorldPos);

		a *= smoothstep(TOD_CloudShadowCutoff, TOD_CloudShadowCutoff + TOD_CloudShadowFade, Luminance(sceneColor.rgb)) * TOD_CloudShadowIntensity;

		if (depth == 1)
		{
			return sceneColor;
		}
		else
		{
			return half4(sceneColor.rgb * (1-a), sceneColor.a);
		}
	}
	ENDCG

	SubShader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile _ TOD_OUTPUT_LINEAR
			#pragma multi_compile _ TOD_CLOUDS_DENSITY
			#pragma multi_compile _ TOD_CLOUDS_BUMPED
			ENDCG
		}
	}

	Fallback Off
}
