Shader "Custom/Invert Colors"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            sampler2D _MainTex;

            fixed4 frag (v2f i) : SV_Target
            {
                float ps = 1.0/1600.0;

                fixed4 col = tex2D(_MainTex, i.uv - (i.uv % (5 * ps)));

                // col.rgb = -8.0 * col.rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(0.0, ps)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(0.0, -ps)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(ps, 0.0)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(-ps, 0.0)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(ps, ps)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(-ps, ps)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(ps, -ps)).rgb;
                // col.rgb += tex2D(_MainTex, i.uv + fixed2(-ps, -ps)).rgb;

                // col.rgb += fixed3(0.1f, 0.0f, 0.2f);

                return col;
            }
            ENDCG
        }
    }
}
