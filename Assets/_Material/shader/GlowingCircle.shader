// On the Fill Circle Image material
Shader "Custom/GlowingCircle"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _GlowIntensity ("Glow", Range(0, 5)) = 2
    }
    
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        
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
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
            };
            
            float4 _Color;
            float _GlowIntensity;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // Distance from center
                float2 center = float2(0.5, 0.5);
                float dist = distance(i.uv, center);
                
                // Radial gradient
                float alpha = 1.0 - smoothstep(0.3, 0.5, dist);
                
                // Glow effect
                float glow = 1.0 - dist * 2.0;
                glow = pow(glow, 3.0);
                
                fixed4 col = _Color;
                col.rgb *= _GlowIntensity * glow;
                col.a *= alpha;
                
                return col;
            }
            ENDCG
        }
    }
}