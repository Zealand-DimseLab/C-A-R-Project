Shader "Custom/Grass_Indirect_Procedural_Normal" {
    Properties {
        _MainTex ("Texture", 2D) = "white" {}
        _BaseColor ("Healthy Green", Color) = (0.1, 0.5, 0.1, 1)
        _DryColor ("Dry Variation", Color) = (0.4, 0.4, 0.2, 1)
        _ColorVariationScale ("Variation Scale", Float) = 0.05
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
        Tags { "RenderType"="Opaque" "Queue"="Geometry" }
        Pass {
            CGPROGRAM
            #pragma target 4.5
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            struct InstanceData {
                float4x4 instanceMatrix;
            };

            // Denne buffer modtager vi fra C# (LOD0 eller LOD1)
            StructuredBuffer<InstanceData> _InstanceBuffer;

            struct appdata {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f {
                float4 pos : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
                float3 worldNormal : TEXCOORD3;
                float3 worldPos : TEXCOORD4;
            };

            float4 _BaseColor, _DryColor, _WindDirection;
            float _WindSpeed, _WindStrength, _ColorVariationScale, _BumpScale, _NormalUpBias;
            float _CurveStrength;
            sampler2D _MainTex;
            float4 _MainTex_TexelSize;

            float simpleNoise(float2 p) {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert (appdata v, uint instanceID : SV_InstanceID) {
                v2f o;

                // 1. Hent matrix direkte fra korrekt buffer
                float4x4 data = _InstanceBuffer[instanceID].instanceMatrix;

                // 2. Udtræk world position til støj/vind-beregning
                float3 worldOrigin = float3(data[0][3], data[1][3], data[2][3]);

                // 3. Højden (h) er nu super simpel. 
                // Da bunden er 0, er v.vertex.y direkte din højde-faktor.
                // Vi bruger saturate for en sikkerheds skyld (hvis din mesh er 1 enhed høj).
                float h = saturate(v.vertex.y);

                // 4. BØJNING OG VIND
                // Da h er 0 i bunden, vil 'totalBend' også være 0 i bunden.
                float curve = pow(h, 2.0) * _CurveStrength;
                float wave = sin(_Time.y * _WindSpeed + (worldOrigin.x * 0.5) + (worldOrigin.z * 0.3));
                float totalBend = curve + (wave * _WindStrength * h);

                // Påfør bøjning i lokal-rum
                v.vertex.xz += _WindDirection.xz * totalBend;

                // 5. WORLD TRANSFORMATION
                // Brug data-matricen direkte (ingen unity_ObjectToWorld)
                float4 worldPos = mul(data, v.vertex);

                o.pos = mul(UNITY_MATRIX_VP, worldPos);
                o.worldPos = worldPos.xyz;
                o.uv = v.uv;

                // 6. FARVE VARIATION
                float noise = simpleNoise(worldOrigin.xz * _ColorVariationScale);
                o.color = lerp(_BaseColor, _DryColor, noise);

                // 7. NORMALER
                float3 n = normalize(mul((float3x3)data, v.normal));
                o.worldNormal = normalize(lerp(n, float3(0,1,0), _NormalUpBias));

                return o;
            }

            fixed4 frag (v2f i) : SV_Target {
                // Simpel procedural normal fra tekstur (Bump)
                float h = tex2D(_MainTex, i.uv).g; 
                float h_right = tex2D(_MainTex, i.uv + float2(_MainTex_TexelSize.x, 0)).g;
                float h_up = tex2D(_MainTex, i.uv + float2(0, _MainTex_TexelSize.y)).g;

                float3 procNormal = normalize(float3((h - h_right) * _BumpScale, (h - h_up) * _BumpScale, 1.0));
                float3 finalNormal = normalize(i.worldNormal + procNormal.xyx * _BumpScale * 10.0);

                // Lighting
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                float ndl = saturate(dot(finalNormal, lightDir));
                
                // Fake Subsurface Scattering (lys gennem græs)
                float backLight = saturate(dot(i.worldNormal, -lightDir)) * 0.3;

                float3 lightColor = _LightColor0.rgb * (ndl + backLight);
                float3 ambient = UNITY_LIGHTMODEL_AMBIENT.rgb;

                fixed4 tex = tex2D(_MainTex, i.uv);
                return float4(tex.rgb * i.color.rgb * (lightColor + ambient), tex.a);
            }
            ENDCG
        }
    }
}
