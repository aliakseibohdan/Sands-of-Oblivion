Shader "Custom/Outline"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1,0.8,0,1)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.01
        _OutlineEnabled ("Outline Enabled", Float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass // Outline pass
        {
            Cull Front
            ZWrite Off
            
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            float _OutlineWidth;
            float4 _OutlineColor;
            float _OutlineEnabled;

            v2f vert (appdata v)
            {
                v2f o;
                if (_OutlineEnabled > 0.5)
                {
                    float3 normal = normalize(v.normal);
                    float3 outlineOffset = normal * _OutlineWidth;
                    o.pos = UnityObjectToClipPos(v.vertex + outlineOffset);
                }
                else
                {
                    o.pos = UnityObjectToClipPos(v.vertex);
                }
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return _OutlineEnabled > 0.5 ? _OutlineColor : fixed4(0,0,0,0);
            }
            ENDCG
        }

        Pass // Main texture pass
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
                float4 pos : SV_POSITION;
            };

            sampler2D _MainTex;

            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                return tex2D(_MainTex, i.uv);
            }
            ENDCG
        }
    }
}