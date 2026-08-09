Shader "CueWarp/GentlemanClassicCue"
{
    Properties
    {
        _AshColor1 ("Ash Wood Base", Color) = (0.88, 0.76, 0.62, 1.0)
        _AshColor2 ("Ash Wood Grain", Color) = (0.58, 0.44, 0.32, 1.0)
        _EbonyColor ("Ebony Butt Splice", Color) = (0.08, 0.08, 0.1, 1.0)
        _RosewoodColor ("Rosewood Accent", Color) = (0.35, 0.16, 0.12, 1.0)
        _IvoryColor ("Ivory Joint Collar", Color) = (0.96, 0.94, 0.88, 1.0)
        _BrassColor ("Golden Brass Ferrule", Color) = (0.86, 0.70, 0.25, 1.0)
        _Glossiness ("Wood Polish Glossiness", Range(0.0, 1.0)) = 0.88
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" "Queue"="Geometry" "RenderPipeline"="UniversalPipeline" }
        LOD 200

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 uv           : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 normalWS     : TEXCOORD0;
                float2 uv           : TEXCOORD1;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _AshColor1;
                float4 _AshColor2;
                float4 _EbonyColor;
                float4 _RosewoodColor;
                float4 _IvoryColor;
                float4 _BrassColor;
                float _Glossiness;
            CBUFFER_END

            Varyings vert(Attributes v)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(v.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(v.normalOS);
                OUT.uv = v.uv;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.uv;
                half3 finalColor = half3(0,0,0);

                // 1. Segment the cylinder cue length along UV.y (0 is butt, 1 is tip)
                if (uv.y > 0.96)
                {
                    // Golden Brass Ferrule on the tip
                    finalColor = _BrassColor.rgb;
                }
                else if (uv.y > 0.92)
                {
                    // Polished Ivory Joint Collar
                    finalColor = _IvoryColor.rgb;
                }
                else if (uv.y < 0.22)
                {
                    // Dark Ebony Chevron butt splicing
                    float chevron = sin(uv.x * 6.283 * 4.0 + uv.y * 35.0);
                    float splice = step(0.0, chevron);
                    finalColor = lerp(_EbonyColor.rgb, _RosewoodColor.rgb, splice);
                }
                else
                {
                    // Ash Wood Grain Shaft
                    // Procedural Wood rings and streaks
                    float ringNoise = sin(uv.x * 3.1415 * 8.0 + sin(uv.y * 6.0) * 8.0);
                    float streakNoise = sin(uv.y * 120.0 + cos(uv.x * 24.0) * 35.0);
                    float woodPattern = saturate((ringNoise * 0.4 + streakNoise * 0.6) * 0.5 + 0.5);
                    
                    finalColor = lerp(_AshColor1.rgb, _AshColor2.rgb, woodPattern);
                }

                // 2. Classy lighting (Standard diffuse Lambert + subtle glossy highlight)
                float3 normal = normalize(IN.normalWS);
                
                // Add soft ambient/directional light response
                float3 mainLightDir = float3(0.5, 1.0, 0.2);
                float NdotL = saturate(dot(normal, normalize(mainLightDir)));
                
                // Premium polished gloss reflection
                float3 viewDir = float3(0, 0, 1);
                float3 halfDir = normalize(mainLightDir + viewDir);
                float NdotH = saturate(dot(normal, halfDir));
                float specular = pow(NdotH, 64.0) * _Glossiness * 0.4;

                half3 litColor = finalColor * (NdotL * 0.85 + 0.25) + half3(specular, specular, specular);

                return half4(litColor, 1.0);
            }
            ENDHLSL
        }
    }
}
