Shader "Custom/FlowingBeam"
{
    Properties
    {
        _MainTex ("Main Texture", 2D) = "white" {}
        _NoiseTex ("Noise Texture", 2D) = "white" {}
        _Color ("Beam Color", Color) = (0.3, 0.8, 1, 1)
        _GlowIntensity ("Glow Intensity", Range(0, 5)) = 2
        
        // Flow settings
        _FlowSpeed ("Flow Speed", Range(-5, 5)) = 1
        _FlowTiling ("Flow Tiling", Range(0.1, 10)) = 2
        
        // Noise settings
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.3
        _NoiseScale ("Noise Scale", Range(0.1, 10)) = 2
        _NoiseSpeed ("Noise Speed", Range(-2, 2)) = 0.5
        
        // Fade settings
        _FadeStart ("Fade Start", Range(0, 1)) = 0.7
        _FadePower ("Fade Power", Range(0.5, 5)) = 2
    }
    
    SubShader
    {
        Tags { 
            "RenderType"="Transparent" 
            "Queue"="Transparent"
            "IgnoreProjector"="True"
        }
        
        Blend SrcAlpha One  // Additive blending for glow
        ZWrite Off
        Cull Off
        
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
                float4 color : COLOR;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float2 flowUV : TEXCOORD1;
                float2 noiseUV : TEXCOORD2;
                float4 color : COLOR;
            };
            
            sampler2D _MainTex;
            sampler2D _NoiseTex;
            float4 _Color;
            float _GlowIntensity;
            
            float _FlowSpeed;
            float _FlowTiling;
            
            float _NoiseStrength;
            float _NoiseScale;
            float _NoiseSpeed;
            
            float _FadeStart;
            float _FadePower;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                
                // Calculate flowing UV
                // U direction flows along the beam
                float flowOffset = _Time.y * _FlowSpeed;
                o.flowUV = float2(v.uv.x * _FlowTiling + flowOffset, v.uv.y);
                
                // Calculate noise UV (different speed for variation)
                float noiseOffset = _Time.y * _NoiseSpeed;
                o.noiseUV = float2(v.uv.x * _NoiseScale + noiseOffset, v.uv.y * _NoiseScale);
                
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // === 1. Base Shape (soft edges) ===
                float distFromCenter = abs(i.uv.y - 0.5) * 2.0;
                float edgeFalloff = 1.0 - smoothstep(0.6, 1.0, distFromCenter);
                float core = 1.0 - pow(distFromCenter, 2.0);
                
                // === 2. Flowing Texture ===
                float4 flowTex = tex2D(_MainTex, i.flowUV);
                
                // === 3. Noise for variation ===
                float4 noiseTex = tex2D(_NoiseTex, i.noiseUV);
                float noiseValue = noiseTex.r; // Use red channel
                
                // Apply noise to brightness
                float noiseMod = lerp(1.0, noiseValue, _NoiseStrength);
                
                // === 4. Length-based fade out ===
                // i.uv.x goes from 0 (tail) to 1 (tip)
                // We want to fade out the tail (low x values)
                float lengthFade = smoothstep(0, _FadeStart, i.uv.x);
                lengthFade = pow(lengthFade, _FadePower);
                
                // === 5. Combine everything ===
                float intensity = core * edgeFalloff * noiseMod * lengthFade;
                
                // Apply flow texture to intensity
                intensity *= flowTex.r;
                
                // Final color
                fixed4 col = _Color * i.color;
                col.a *= intensity;
                col.rgb *= _GlowIntensity * intensity;
                
                return col;
            }
            ENDCG
        }
    }
}