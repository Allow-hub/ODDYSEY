Shader "Custom/RainShaderURP"
{
    Properties
    {
        _MainTex                    ("Color (RGB) Alpha (A)", 2D) = "gray" {}
        _TintColor                  ("Tint Color (RGB)", Color) = (1, 1, 1, 1)
        _PointSpotLightMultiplier   ("Point/Spot Light Multiplier", Range(0, 10)) = 2
        _DirectionalLightMultiplier ("Directional Light Multiplier", Range(0, 10)) = 1
        _InvFade                    ("Soft Particles Factor", Range(0.01, 100.0)) = 1.0
        _AmbientLightMultiplier     ("Ambient Light Multiplier", Range(0, 1)) = 0.25
    }

    SubShader
    {
        Tags
        {
            "RenderType"     = "Transparent"
            "Queue"          = "Transparent"
            "IgnoreProjector" = "True"
            "RenderPipeline" = "UniversalPipeline"
        }
        LOD 100

        Pass
        {
            Name "RainURP"
            Tags { "LightMode" = "UniversalForward" }

            ZWrite Off
            Cull Back
            Blend SrcAlpha OneMinusSrcAlpha
            ColorMask RGB

            HLSLPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            #pragma multi_compile_fog

            // ソフトパーティクル
            #pragma multi_compile _ SOFTPARTICLES_ON

            // URP ライト関連
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            // ソフトパーティクル用カメラ深度
            #if defined(SOFTPARTICLES_ON)
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #endif

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float  _PointSpotLightMultiplier;
                float  _DirectionalLightMultiplier;
                float  _InvFade;
                float  _AmbientLightMultiplier;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color      : COLOR;
                float2 uv         : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float4 color       : COLOR0;
                float2 uv          : TEXCOORD0;
                #if defined(SOFTPARTICLES_ON)
                float4 projPos     : TEXCOORD1;
                #endif
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─── ライト計算 ───────────────────────────────────────────────

            /// <summary>
            /// URP の追加ライト（Point / Spot）の寄与を加算する。
            /// 旧来の unity_LightColor / unity_LightPosition に相当。
            /// </summary>
            float3 ApplyAdditionalLights(float3 posWS, float3 lightAccum)
            {
                int count = GetAdditionalLightsCount();
                for (int i = 0; i < count; i++)
                {
                    Light light   = GetAdditionalLight(i, posWS);
                    float atten   = light.distanceAttenuation * light.shadowAttenuation;
                    lightAccum   += light.color * atten * _PointSpotLightMultiplier;
                }
                return lightAccum;
            }

            /// <summary>
            /// メインライト（Directional）＋追加ライト＋アンビエントを合成して頂点カラーを返す。
            /// </summary>
            float4 LightForVertex(float3 posWS)
            {
                // アンビエント
                float3 lightAccum = SampleSH(float3(0, 1, 0)) * _AmbientLightMultiplier;

                // メインライト（通常 Directional）
                Light mainLight  = GetMainLight();
                float mainAtten  = mainLight.distanceAttenuation * mainLight.shadowAttenuation;

                // 旧実装と同様に高さ方向で強度を調整
                // （ライトが水平以下に向いていると雨に当たりにくい）
                float upDot     = dot(mainLight.direction, float3(0, 1, 0));
                float multiplier = saturate(upDot * 2.0 + 1.0);
                lightAccum      += mainLight.color * mainAtten * multiplier * _DirectionalLightMultiplier;

                // 追加ライト（Point / Spot）
                lightAccum = ApplyAdditionalLights(posWS, lightAccum);

                return float4(lightAccum, 1.0);
            }

            // ─── 頂点シェーダー ───────────────────────────────────────────

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                float3 posWS       = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS    = TransformWorldToHClip(posWS);
                OUT.uv             = TRANSFORM_TEX(IN.uv, _MainTex);

                float4 lightColor  = LightForVertex(posWS);
                OUT.color          = lightColor * IN.color * _TintColor;

                // アルファをライト強度に追従させる（旧実装と同じ挙動）
                float maxRGB       = max(OUT.color.r, max(OUT.color.g, OUT.color.b));
                float tintAlpha    = max(_TintColor.a, 0.0001);
                OUT.color         *= min(maxRGB, tintAlpha) / tintAlpha;

                #if defined(SOFTPARTICLES_ON)
                OUT.projPos        = ComputeScreenPos(OUT.positionHCS);
                OUT.projPos.z      = -TransformWorldToView(posWS).z; // eye depth
                #endif

                return OUT;
            }

            // ─── フラグメントシェーダー ───────────────────────────────────

            half4 frag(Varyings IN) : SV_Target
            {
                #if defined(SOFTPARTICLES_ON)
                // URP のカメラ深度テクスチャからシーン深度を取得
                float2 screenUV = IN.projPos.xy / IN.projPos.w;
                float  sceneZ   = LinearEyeDepth(
                    SampleSceneDepth(screenUV),
                    _ZBufferParams);
                float  partZ    = IN.projPos.z;
                IN.color.a     *= saturate(_InvFade * (sceneZ - partZ));
                #endif

                half4 tex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv);

                // アルファが極端に低いピクセルを破棄（旧 AlphaTest Greater 0.01 相当）
                clip(tex.a * IN.color.a - 0.01);

                return tex * IN.color;
            }
            ENDHLSL
        }
    }

    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}
