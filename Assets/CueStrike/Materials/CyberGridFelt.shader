Shader "Custom/URP/CyberGridFelt"
{
    Properties
    {
        _BaseColor("Background Felt Color", Color) = (0.05, 0.05, 0.08, 1)
        _GridColor("Neon Grid Color", Color) = (0.0, 0.8, 1.0, 1)
        _GridFrequency("Grid Frequency", Range(5, 50)) = 20.0
        _LineWidth("Grid Line Width", Range(0.01, 0.2)) = 0.05
        
        _FuzzColor("Fuzz Edge Color", Color) = (0.1, 0.1, 0.2, 1)
        _FuzzPower("Fuzz Power", Range(1, 10)) = 5.0
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Geometry"
        }
        LOD 300

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_l9x
            #pragma target 2.0

            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

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
                float4 positionCS   : SV_POSITION;
                float3 positionWS   : TEXCOORD3;
                float2 uv           : TEXCOORD0;
                float3 normalWS     : TEXCOORD4;
                float3 viewDirWS    : TEXCOORD5;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseColor;
                float4 _GridColor;
                float _GridFrequency;
                float _LineWidth;
                float4 _FuzzColor;
                float _FuzzPower;
            CBUFFER_END

            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = vertexInput.positionCS;
                output.positionWS = vertexInput.positionWS;
                output.uv = input.uv;

                VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, float4(0,0,0,0));
                output.normalWS = normalInput.normalWS;
                output.viewDirWS = GetWorldSpaceViewDir(vertexInput.positionWS);

                return output;
            }

            float4 LitPassFragment(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirWS = normalize(input.viewDirWS);

                // Draw neon grid lines using uv
                float2 grid = frac(input.uv * _GridFrequency);
                float lineX = step(1.0 - _LineWidth, grid.x);
                float lineY = step(1.0 - _LineWidth, grid.y);
                float gridMask = saturate(lineX + lineY);

                // Mix grid and background albedo
                float3 baseAlbedo = lerp(_BaseColor.rgb, _GridColor.rgb, gridMask);

                // Fuzzy velvet edge lighting
                float NdotV = saturate(dot(normalWS, viewDirWS));
                float fuzz = pow(1.0 - NdotV, _FuzzPower);
                float3 finalAlbedo = lerp(baseAlbedo, _FuzzColor.rgb, fuzz);

                // Light calculation
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float NdotL = saturate(dot(normalWS, lightDir));

                // Add emissive glow to the grid lines
                float3 emission = _GridColor.rgb * gridMask * 1.5f;

                float3 diffuse = finalAlbedo * mainLight.color * NdotL * mainLight.shadowAttenuation;
                float3 ambient = _BaseColor.rgb * 0.1;

                float3 finalColor = diffuse + ambient + emission;

                return float4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    FallBack "Universal Render Pipeline/Simple Lit"
}
