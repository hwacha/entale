// Made with this tutorial: https://roystan.net/articles/toon-shader.html

Shader "HighlightableToon"
{
	Properties
	{
		_Color("Color", Color) = (0.5, 0.65, 1, 1)
		_MainTex("Main Texture", 2D) = "white" {}	
		_Ramp("Ramp Texture", 2D) = "white" {}
		[HDR] _AmbientColor("Ambient Color", Color) = (0.4,0.4,0.4,1)
        _HighlightColor ("HighlightColor", Color) = (1, 0, 0, 1)
        _IsHighlighted ("IsHighlighted", Int) = 0
	}
	SubShader
	{
		Tags
		{
			"LightMode" = "ForwardBase"
			"PassFlags" = "OnlyDirectional"
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			#pragma multi_compile_fwdbase
			
			#include "UnityCG.cginc"
			#include "Lighting.cginc"
			#include "AutoLight.cginc"

			struct appdata
			{
				float4 vertex : POSITION;				
				float4 uv : TEXCOORD0;
				float3 normal : NORMAL;
			};

			struct v2f
			{
				float4 pos : SV_POSITION;
				float2 uv : TEXCOORD0;
				float3 worldNormal : NORMAL;
				SHADOW_COORDS(2)
			};

			sampler2D _MainTex;
			float4 _MainTex_ST;
			sampler2D _Ramp;
			
			v2f vert (appdata v)
			{
				v2f o;
				o.pos = UnityObjectToClipPos(v.vertex);
				o.uv = TRANSFORM_TEX(v.uv, _MainTex);
				o.worldNormal = UnityObjectToWorldNormal(v.normal);
				TRANSFER_SHADOW(o)
				return o;
			}
			
			float4 _Color;
			float4 _AmbientColor;
			fixed4 _HighlightColor;
			int _IsHighlighted;

			float4 frag (v2f i) : SV_Target
			{

				float3 normal = normalize(i.worldNormal);

				// Directional + Ambient
				float shadow = SHADOW_ATTENUATION(i);
				float NdotL = dot(_WorldSpaceLightPos0, normal);
				float2 uv = float2(1 - (NdotL * 0.5 + 0.49), 0.5);
				float lightIntensity = tex2D (_Ramp, uv);
				float4 light = lightIntensity * shadow * _LightColor0;

				// Final color
				float4 sample = tex2D(_MainTex, i.uv);
				float4 c = _Color * sample * (_AmbientColor + light);
				if(_IsHighlighted) {
					c *= _HighlightColor;
				}
				return c;

			}
			ENDCG
		}
		UsePass "Legacy Shaders/VertexLit/SHADOWCASTER"
	}
}