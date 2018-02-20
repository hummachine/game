// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Time of Day/Scattering"
{
	Properties
	{
		_MainTex ("Base", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "TOD_Base.cginc"
	#include "TOD_Scattering.cginc"

	uniform sampler2D _MainTex;
	uniform sampler2D_float _CameraDepthTexture;

	uniform float4x4 _FrustumCornersWS;
	uniform float4 _MainTex_TexelSize;

	struct v2f
	{
		float4 pos       : SV_POSITION;
		float2 uv        : TEXCOORD0;
#if UNITY_UV_STARTS_AT_TOP
		float2 uv1       : TEXCOORD1;
#endif
		float2 uv_depth  : TEXCOORD2;
#if TOD_OUTPUT_DITHERING
		float2 uv_dither : TEXCOORD3;
#endif
		float3 cameraRay : TEXCOORD4;
	};

	v2f vert(appdata_img v)
	{
		v2f o;

		half index = v.vertex.z;
		v.vertex.z = 0.1;

		o.pos = UnityObjectToClipPos(v.vertex);

		o.uv        = v.texcoord.xy;
		o.uv_depth  = v.texcoord.xy;

#if TOD_OUTPUT_DITHERING
		o.uv_dither = DitheringCoords(v.texcoord.xy);
#endif

#if UNITY_UV_STARTS_AT_TOP
		o.uv1 = v.texcoord.xy;
		if (_MainTex_TexelSize.y < 0)
			o.uv1.y = 1-o.uv1.y;
#endif

		o.cameraRay = _FrustumCornersWS[(int)index];

		return o;
	}

	half4 frag(v2f i) : COLOR
	{
		half4 color = tex2D(_MainTex, i.uv);

		float rawDepth = SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, i.uv_depth);
		float depth = Linear01Depth(rawDepth);
		float3 worldPos = _WorldSpaceCameraPos + depth * i.cameraRay;

#if UNITY_UV_STARTS_AT_TOP
		half4 mask = tex2D(TOD_SkyMask, i.uv1);
#else
		half4 mask = tex2D(TOD_SkyMask, i.uv);
#endif

		half4 scattering = AtmosphericScattering(i.cameraRay, worldPos, depth, mask);

#if TOD_OUTPUT_DITHERING
		scattering.rgb += DitheringColor(i.uv_dither);
#endif

#if !TOD_OUTPUT_HDR
		scattering = TOD_HDR2LDR(scattering);
#endif

#if !TOD_OUTPUT_LINEAR
		scattering = TOD_LINEAR2GAMMA(scattering);
#endif

		if (depth == 1)
		{
			color.rgb += scattering.rgb;
		}
		else
		{
			color.rgb = lerp(color.rgb, scattering.rgb, scattering.a);
		}

		return color;
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
			#pragma target 3.0
			#pragma multi_compile _ TOD_OUTPUT_HDR
			#pragma multi_compile _ TOD_OUTPUT_LINEAR
			#pragma multi_compile _ TOD_OUTPUT_DITHERING
			ENDCG
		}
	}

	Fallback Off
}
