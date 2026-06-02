Shader "Custom/URP_Grass_Indirect_Shadows" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Colors)]
        _BaseColor ("Bottom Color", Color) = (0.05, 0.2, 0.05, 1)
        _TopColor ("Top Color", Color) = (0.2, 0.5, 0.1, 1)
        _DryColor ("Dry Variation", Color) = (0.4, 0.4, 0.2, 1)
        _ColorVariationScale ("Variation Scale", Float) = 0.05
        
        [Header(Shape)]
        _CurveStrength ("Curve Strength", Float) = 0.5

        [Header(Fake Normals)]
        _BumpScale ("Bump Strength", Range(0, 0.1)) = 0.02
        _NormalUpBias ("Upwards Bias", Range(0, 1)) = 0.5

        [Header(Wind)]
        _WindSpeed ("Wind Speed", Float) = 1.5
        _WindStrength ("Wind Strength", Float) = 0.2
        _WindDirection ("Wind Direction", Vector) = (1, 0, 0.5, 0)
    }
    SubShader {
        Tags { "RenderType"="Opaque" "RenderPipeline"="UniversalPipeline" "Queue"="Geometry" }

        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

        struct GrassData {
            float3 Position;
            float Yaw;
            float Scale;
        };

        StructuredBuffer<GrassData> _InstanceBuffer;

        float4x4 GetInstanceMatrix(GrassData data) {
            float s = data.Scale;
            float angle = data.Yaw * (3.14159265f / 180.0f);
            float cosY = cos(angle);
            float sinY = sin(angle);
            return float4x4(
                cosY * s,  0, sinY * s, data.Position.x,
                0,         s, 0,        data.Position.y,
                -sinY * s, 0, cosY * s, data.Position.z,
                0,         0, 0,        1
            );
        }

        float simpleNoise(float2 p) {
            return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
        }

        CBUFFER_START(UnityPerMaterial)
            float4 _BaseColor, _TopColor, _DryColor, _WindDirection;
        float _WindSpeed, _WindStrength, _ColorVariationScale, _BumpScale, _NormalUpBias;
        float _CurveStrength;
        CBUFFER_END

        ENDHLSL

        // --- FORWARD LIT PASS ---
        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                half4 color : COLOR;
                float3 normalWS : TEXCOORD3;
                float3 positionWS : TEXCOORD4;
                float4 shadowCoord : TEXCOORD5;
            };

            sampler2D _MainTex;

            Varyings vert (Attributes v) {
                Varyings o = (Varyings)0;
                
                GrassData data = _InstanceBuffer[v.instanceID];
                float4x4 instanceMatrix = GetInstanceMatrix(data);
                float3 worldOrigin = data.Position;
                
                float h = saturate(v.positionOS.y);

                // Wind & Bending
                float curve = pow(h, 2.0) * _CurveStrength;
                float wave = sin(_Time.y * _WindSpeed + (worldOrigin.x * 0.5) + (worldOrigin.z * 0.3));
                float totalBend = curve + (wave * _WindStrength * h);
                v.positionOS.xz += _WindDirection.xz * totalBend;

                float3 positionWS = mul(instanceMatrix, v.positionOS).xyz;
                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.uv = v.uv;

                // Color Gradient
                half3 gradientColor = lerp(_BaseColor.rgb, _TopColor.rgb, h);
                float noise = simpleNoise(worldOrigin.xz * _ColorVariationScale);
                o.color.rgb = lerp(gradientColor, _DryColor.rgb, noise * 0.5);

                o.normalWS = normalize(mul((float3x3)instanceMatrix, v.normalOS));
                o.normalWS = normalize(lerp(o.normalWS, float3(0,1,0), _NormalUpBias));

                o.shadowCoord = TransformWorldToShadowCoord(positionWS);

                return o;
            }

            half4 frag (Varyings i) : SV_Target {
                Light mainLight = GetMainLight(i.shadowCoord);
                half shadow = mainLight.shadowAttenuation;

                float3 lightDir = mainLight.direction;
                float ndl = saturate(dot(i.normalWS, lightDir));
                float backLight = saturate(dot(i.normalWS, -lightDir)) * 0.3;

                half3 lightColor = mainLight.color * (ndl * shadow + backLight);
                half3 ambient = SampleSH(i.normalWS);

                half4 tex = tex2D(_MainTex, i.uv);
                return half4(tex.rgb * i.color.rgb * (lightColor + ambient), tex.a);
            }
            ENDHLSL
        }

        // --- SHADOW CASTER PASS ---
        Pass {
            Name "ShadowCaster"
            Tags { "LightMode"="ShadowCaster" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"

            struct Attributes {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                uint instanceID : SV_InstanceID;
            };

            struct Varyings {
                float4 positionCS : SV_POSITION;
            };

            float3 _LightDirection;

            Varyings vert (Attributes v) {
                Varyings o;
                GrassData data = _InstanceBuffer[v.instanceID];
                float4x4 instanceMatrix = GetInstanceMatrix(data);
                
                float3 positionWS = mul(instanceMatrix, v.positionOS).xyz;
                float3 normalWS = normalize(mul((float3x3)instanceMatrix, v.normalOS));

                // ApplyShadowBias helps prevent shadow artifacts
                o.positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, _LightDirection));
                return o;
            }

            half4 frag () : SV_Target {
                return 0;
            }
            ENDHLSL
        }
    }
}
