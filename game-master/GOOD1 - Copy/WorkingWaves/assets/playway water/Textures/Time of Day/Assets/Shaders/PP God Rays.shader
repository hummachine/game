// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/Time of Day/God Rays"
{
	Properties
	{
		_MainTex ("Base", 2D) = "white" {}
	}

	CGINCLUDE
	#include "UnityCG.cginc"
	#include "TOD_Base.cginc"

	struct v2f
	{
		float4 pos : SV_POSITION;
		float2 uv  : TEXCOORD0;
		#if UNITY_UV_STARTS_AT_TOP
		float2 uv1 : TEXCOORD1;
		#endif
	};

	uniform sampler2D _MainTex;

	uniform half4 _LightColor;
	uniform half4 _MainTex_TexelSize;

	v2f vert(appdata_img v)
	{
		v2f o;

		o.pos = UnityObjectToClipPos(v.vertex);
		o.uv = v.texcoord.xy;

		#if UNITY_UV_STARTS_AT_TOP
		o.uv1 = v.texcoord.xy;
		if (_MainTex_TexelSize.y < 0)
			o.uv1.y = 1-o.uv1.y;
		#endif

		return o;
	}

	half4 frag_screen(v2f i) : COLOR
	{
		half4 colorA = tex2D(_MainTex, i.uv);
		#if UNITY_UV_STARTS_AT_TOP
		half4 colorB = tex2D(TOD_SkyMask, i.uv1);
		#else
		half4 colorB = tex2D(TOD_SkyMask, i.uv);
		#endif
		half4 depthMask = saturate(colorB * _LightColor);
		return 1.0f - (1.0f-colorA) * (1.0f-depthMask);
	}

	half4 frag_add(v2f i) : COLOR
	{
		half4 colorA = tex2D(_MainTex, i.uv);
		#if UNITY_UV_STARTS_AT_TOP
		half4 colorB = tex2D(TOD_SkyMask, i.uv1);
		#else
		half4 colorB = tex2D(TOD_SkyMask, i.uv);
		#endif
		half4 depthMask = saturate(colorB * _LightColor);
		return colorA + depthMask;
	}
	ENDCG

	Subshader
	{
		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_screen
			ENDCG
		}

		Pass
		{
			ZTest Always Cull Off ZWrite Off

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag_add
			ENDCG
		}
	}

	Fallback Off
}
