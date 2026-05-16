Shader "Custom/SpriteUnlitShadowCaster"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        [Header(Shadow)]
        _ShadowAlphaCutoff ("Shadow Alpha Cutoff", Range(0.0, 1.0)) = 0.5
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
        }

        // ─── Pass 1: Unlit 描画 ───────────────────────────────────────────
        Pass
        {
            Name "SpriteUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _ShadowAlphaCutoff;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
                float4 color       : COLOR;
            };

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color       = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 col = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                clip(col.a - 0.01);
                return col;
            }
            ENDHLSL
        }

        // ─── Pass 2: ShadowCaster ─────────────────────────────────────────
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }

            ZWrite On
            ZTest LEqual
            Cull Off
            ColorMask 0

            HLSLPROGRAM
            #pragma vertex   vertShadow
            #pragma fragment fragShadow
            #pragma multi_compile_shadowcaster

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float3 _LightDirection;
            float3 _LightPosition;

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float  _ShadowAlphaCutoff;
            CBUFFER_END

            struct AttributesShadow
            {
                float4 positionOS : POSITION;
                float3 normalOS   : NORMAL;
                float2 uv         : TEXCOORD0;
            };

            struct VaryingsShadow
            {
                float4 positionHCS : SV_POSITION;
                float2 uv          : TEXCOORD0;
            };

            float4 GetShadowPositionHClip(float3 positionOS, float3 normalOS)
            {
                float3 posWS  = TransformObjectToWorld(positionOS);
                float3 normWS = TransformObjectToWorldNormal(normalOS);

                #if _CASTING_PUNCTUAL_LIGHT_SHADOW
                    float3 lightDir = normalize(_LightPosition - posWS);
                #else
                    float3 lightDir = _LightDirection;
                #endif

                float invNdotL = 1.0 - saturate(dot(normWS, lightDir));
                posWS += normWS * (invNdotL * 0.001);

                float4 posHCS = TransformWorldToHClip(posWS);

                #if UNITY_REVERSED_Z
                    posHCS.z = min(posHCS.z, posHCS.w * UNITY_NEAR_CLIP_VALUE);
                #else
                    posHCS.z = max(posHCS.z, posHCS.w * UNITY_NEAR_CLIP_VALUE);
                #endif

                return posHCS;
            }

            VaryingsShadow vertShadow(AttributesShadow IN)
            {
                VaryingsShadow OUT;
                OUT.positionHCS = GetShadowPositionHClip(IN.positionOS.xyz, IN.normalOS);
                OUT.uv          = TRANSFORM_TEX(IN.uv, _MainTex);
                return OUT;
            }

            half4 fragShadow(VaryingsShadow IN) : SV_Target
            {
                half alpha = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv).a;
                clip(alpha - _ShadowAlphaCutoff);
                return 0;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
