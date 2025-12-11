Shader "AFFECT/Foliage/ProceduralGrass"
{
    Properties
    {
        _BottomColor ("Bottom Color", Color) = (0.32, 0.5, 0.3, 1)
        _TopColor ("Top Color", Color) = (0.55, 0.8, 0.45, 1)
        _AlphaCutoff ("Alpha Cutoff", Range(0, 1)) = 0.25
        _WindDirection ("Wind Direction", Vector) = (1, 0.3, 0, 0)
        _WindAmplitude ("Wind Amplitude", Range(0, 1)) = 0.35
        _WindFrequency ("Wind Speed", Range(0, 10)) = 2.0
        _BendStrength ("Bend Strength", Range(0, 1)) = 0.6
        _AmbientStrength ("Ambient Strength", Range(0, 2)) = 0.65
        _InstanceColor ("Instance Color", Color) = (1, 1, 1, 1)
    }

    SubShader
    {
        Tags { "RenderPipeline"="UniversalRenderPipeline" "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 200
        Cull Off
        ZWrite On
        AlphaToMask On
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _BottomColor;
                float4 _TopColor;
                float _AlphaCutoff;
                float4 _WindDirection;
                float _WindAmplitude;
                float _WindFrequency;
                float _BendStrength;
                float _AmbientStrength;
            CBUFFER_END

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float4, _InstanceColor)
                UNITY_DEFINE_INSTANCED_PROP(float, _SwayOffset)
            UNITY_INSTANCING_BUFFER_END(Props)

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 normalWS   : TEXCOORD0;
                float3 positionWS : TEXCOORD1;
                float2 uv         : TEXCOORD2;
                float fogFactor   : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);

                float2 dir = _WindDirection.xy;
                float dirLen = max(length(dir), 0.0001);
                dir /= dirLen;

                float swayOffset = UNITY_ACCESS_INSTANCED_PROP(Props, _SwayOffset);
                float tip = saturate(input.uv.y);
                float sway = sin((_Time.y * _WindFrequency) + swayOffset) * _WindAmplitude * tip;
                float bend = _BendStrength * tip;

                float3 posOS = input.positionOS.xyz;
                posOS.xz += dir * (sway + bend * sway * 0.5);

                float3 normalOS = normalize(input.normalOS);
                float3 positionWS = TransformObjectToWorld(posOS);
                output.positionCS = TransformWorldToHClip(positionWS);
                output.normalWS = TransformObjectToWorldNormal(normalOS);
                output.positionWS = positionWS;
                output.uv = input.uv;
                output.fogFactor = ComputeFogFactor(output.positionCS.z);
                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);

                float2 uv = input.uv;
                float edge = smoothstep(0.0, 0.05, uv.x) * smoothstep(0.0, 0.05, 1.0 - uv.x);
                edge *= smoothstep(0.0, 0.1, 1.0 - uv.y);
                clip(edge - _AlphaCutoff);

                float3 normalWS = normalize(input.normalWS);
                Light mainLight = GetMainLight();
                float ndl = saturate(dot(normalWS, mainLight.direction));

                float3 albedo = lerp(_BottomColor.rgb, _TopColor.rgb, saturate(uv.y));
                float4 instanceColor = UNITY_ACCESS_INSTANCED_PROP(Props, _InstanceColor);
                albedo *= instanceColor.rgb;

                float3 lit = albedo * (mainLight.color * ndl + _AmbientStrength);
                lit = MixFog(lit, input.fogFactor);
                return half4(lit, 1.0);
            }
            ENDHLSL
        }
    }
}
