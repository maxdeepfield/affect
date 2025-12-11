Shader "AFFECT/Foliage/GrassInstanced"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Color", Color) = (0.3, 0.5, 0.2, 1)
        _Cutoff ("Alpha Cutoff", Range(0, 1)) = 0.5
        _WindDirection ("Wind Direction", Vector) = (1, 0.35, 0, 0)
        _WindFrequency ("Wind Frequency", Float) = 1.6
        _WindAmplitude ("Wind Amplitude", Float) = 0.35
    }
    
    SubShader
    {
        Tags { "RenderType"="TransparentCutout" "Queue"="AlphaTest" }
        LOD 100
        Cull Off
        
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 4.5
            
            #include "UnityCG.cginc"
            
            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };
            
            struct v2f
            {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD1;
                float3 worldPos : TEXCOORD2;
            };
            
            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float _Cutoff;
            float4 _WindDirection;
            float _WindFrequency;
            float _WindAmplitude;
            
            StructuredBuffer<float4> _MatrixBuffer;
            StructuredBuffer<float4> _ColorBuffer;
            
            v2f vert(appdata v, uint instanceID : SV_InstanceID)
            {
                v2f o;
                
                // Read instance data
                uint matrixBase = instanceID * 4;
                float4 row0 = _MatrixBuffer[matrixBase];
                float4 row1 = _MatrixBuffer[matrixBase + 1];
                float4 row2 = _MatrixBuffer[matrixBase + 2];
                float4 row3 = _MatrixBuffer[matrixBase + 3];
                float4 color = _ColorBuffer[instanceID];
                
                // Build matrix
                float4x4 matrix = float4x4(row0, row1, row2, row3);
                
                // Transform vertex
                float4 worldPos = mul(matrix, v.vertex);
                
                // Wind animation
                float windStrength = v.uv.y * _WindAmplitude;
                float windTime = _Time.y * _WindFrequency;
                float windOffset = dot(worldPos.xz, _WindDirection.xy * 0.1);
                
                float3 windDisp = float3(
                    sin(windTime + windOffset) * _WindDirection.x,
                    0,
                    sin(windTime * 0.7 + windOffset) * _WindDirection.y
                ) * windStrength;
                
                worldPos.xyz += windDisp;
                
                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                o.color = color;
                o.worldPos = worldPos.xyz;
                
                // Normal transform
                float3x3 rotMatrix = float3x3(row0.xyz, row1.xyz, row2.xyz);
                o.worldNormal = normalize(mul(rotMatrix, v.normal));
                
                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 tex = tex2D(_MainTex, i.uv);
                clip(tex.a - _Cutoff);
                
                // Simple lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndotl = saturate(dot(i.worldNormal, lightDir) * 0.5 + 0.5);
                
                fixed4 col = i.color * tex;
                col.rgb *= ndotl;
                col.rgb += i.color.rgb * 0.2;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
