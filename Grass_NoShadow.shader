Shader "Custom/URP_Grass_NoShadow" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _Cutoff ("Alpha Cutoff", Range(0,1)) = 0.5

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
        #pragma multi_compile _ LOD_FADE_CROSSFADE

        TEXTURE2D(_MainTex);
        SAMPLER(sampler_MainTex);

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
        float _ColorVariationScale, _BumpScale, _NormalUpBias, _WindSpeed, _WindStrength;
        float _CurveStrength, _Cutoff;
        CBUFFER_END

        float3 GrassEffectWind(float3 positionOS, float3 worldOrigin) {
            float h = saturate(positionOS.y);

            float curve = pow(h, 2.0) * _CurveStrength;
            float wave = sin(_Time.y * _WindSpeed + (worldOrigin.x * 0.5) + (worldOrigin.z * 0.3));
            float totalBend = curve + (wave * _WindStrength * h);
            positionOS.xz += _WindDirection.xz * totalBend;

            return positionOS;
        }
        ENDHLSL

        // --- FORWARD LIT PASS ---
        // VERTEX SHADING
        Pass {
            Name "ForwardLit"
            Tags { "LightMode"="UniversalForward" }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag
            
            // REMOVED: Shadow multi_compile keywords

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
                // REMOVED: shadowCoord
            };

            Varyings vert (Attributes v) {
                Varyings o = (Varyings)0;
                
                GrassData data = _InstanceBuffer[v.instanceID];

                float4x4 instanceMatrix = GetInstanceMatrix(data);
                float3 worldOrigin = data.Position;
                
                float h = saturate(v.positionOS.y);

                float3 appliedGrassEffect = GrassEffectWind(v.positionOS.xyz, worldOrigin);
                float3 positionWS = mul(instanceMatrix, float4(appliedGrassEffect,1)).xyz;

                o.positionCS = TransformWorldToHClip(positionWS);
                o.positionWS = positionWS;
                o.uv = v.uv;

                half3 gradientColor = lerp(_BaseColor.rgb, _TopColor.rgb, h);
                float noise = simpleNoise(worldOrigin.xz * _ColorVariationScale);
                o.color.rgb = lerp(gradientColor, _DryColor.rgb, noise * 0.5);

                o.normalWS = normalize(mul((float3x3)instanceMatrix, v.normalOS));
                o.normalWS = normalize(lerp(o.normalWS, float3(0,1,0), _NormalUpBias));

                // REMOVED: TransformWorldToShadowCoord calculation

                return o;
            }

            // FRAGMENT SHADING
            half4 frag (Varyings i) : SV_Target {
#ifdef LOD_FADE_CROSSFADE
                // FIX: Swap out non-existent/custom screenSpaceDither function for native URP crossfading
                LODFadeCrossFade(i.positionCS.xy);
#endif
                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.uv);

                clip(tex.a - _Cutoff);

                // FIX: GetMainLight called without parameters bypasses shadow-map lookups
                Light mainLight = GetMainLight();

                float3 lightDir = mainLight.direction;
                float ndl = saturate(dot(i.normalWS, lightDir));
                float backLight = saturate(dot(i.normalWS, -lightDir)) * 0.3;

                // FIX: Shadow attenuation calculation removed entirely
                half3 lightColor = mainLight.color * (ndl + backLight);
                half3 ambient = SampleSH(i.normalWS);

                return half4(tex.rgb * i.color.rgb * (lightColor + ambient), tex.a);
            }
            ENDHLSL
        }
    }
}
