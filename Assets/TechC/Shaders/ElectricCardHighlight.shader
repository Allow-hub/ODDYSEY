Shader "Custom/ElectricCardHighlight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        [Header(Glow)]
        _GlowColor    ("Glow Color",      Color)         = (0.3, 0.8, 1.0, 1.0)
        _GlowIntensity("Glow Intensity",  Range(0, 5))   = 2.0
        _GlowWidth    ("Edge Glow Width", Range(0, 0.5)) = 0.08

        [Header(Scanline)]
        _ScanlineColor    ("Scanline Color",     Color)         = (0.3, 0.9, 1.0, 1.0)
        _ScanlineSpeed    ("Scanline Speed",     Range(0, 5))   = 1.5
        _ScanlineDensity  ("Scanline Density",   Range(1, 100)) = 20.0
        _ScanlineThickness("Scanline Thickness", Range(0, 1))   = 0.06

        [Header(Hologram)]
        _HoloColor    ("Hologram Color",   Color)       = (0.0, 0.6, 1.0, 1.0)
        _HoloSpeed    ("Hologram Speed",   Range(0, 5)) = 2.0
        _HoloStrength ("Hologram Strength",Range(0, 1)) = 0.2

        [Header(Flicker)]
        _FlickerSpeed ("Flicker Speed",    Range(0, 20)) = 8.0
        _FlickerAmount("Flicker Strength", Range(0, 1))  = 0.15

        [Header(Intensity)]
        _Intensity ("Overall Intensity", Range(0, 1)) = 0

        // C# から毎フレーム渡す時間
        _LocalTime ("Local Time", Float) = 0

        // Unity UI 内部プロパティ
        _StencilComp     ("Stencil Comparison", Float) = 8
        _Stencil         ("Stencil ID",         Float) = 0
        _StencilOp       ("Stencil Operation",  Float) = 0
        _StencilWriteMask("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask",  Float) = 255
        _ColorMask       ("Color Mask",         Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"             = "Transparent"
            "IgnoreProjector"   = "True"
            "RenderType"        = "Transparent"
            "PreviewType"       = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Stencil
        {
            Ref      [_Stencil]
            Comp     [_StencilComp]
            Pass     [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask[_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        // ▼ 加算合成：背景に光を足すだけ。黒=透明になる
        Blend One One

        ColorMask [_ColorMask]

        Pass
        {
            Name "ElectricAdditive"

            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma target   2.0
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;

            fixed4 _GlowColor;
            float  _GlowIntensity;
            float  _GlowWidth;

            fixed4 _ScanlineColor;
            float  _ScanlineSpeed;
            float  _ScanlineDensity;
            float  _ScanlineThickness;

            fixed4 _HoloColor;
            float  _HoloSpeed;
            float  _HoloStrength;

            float  _FlickerSpeed;
            float  _FlickerAmount;
            float  _Intensity;
            float  _LocalTime;

            float4 _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            // ─── ユーティリティ ────────────────────────────────────────

            float fsin(float x) { return sin(x) * 0.5 + 0.5; }

            float randVal(float seed)
            {
                return frac(sin(seed * 127.1 + 311.7) * 43758.5453);
            }

            // 縁だけ光る（内側は 0、縁に近いほど 1）
            float EdgeGlow(float2 uv, float width)
            {
                float2 edge = min(uv, 1.0 - uv);
                float  d    = min(edge.x, edge.y);
                return 1.0 - smoothstep(0.0, width, d);
            }

            // 上から下へ流れるスキャンライン
            float Scanline(float2 uv, float t)
            {
                float scanVal = frac(uv.y * _ScanlineDensity - t * _ScanlineSpeed);
                float s0 = smoothstep(0.0,                    _ScanlineThickness, scanVal);
                float s1 = smoothstep(_ScanlineThickness * 2.0, _ScanlineThickness, scanVal);
                return s0 * s1; // 加算なので輝く部分だけ正値にする
            }

            // 横方向に揺れるホログラム
            float3 HologramShift(float2 uv, float t)
            {
                float shift = sin(uv.y * 8.0 + t * _HoloSpeed) * 0.02;
                float r = fsin(uv.x + shift       + t * 0.5);
                float g = fsin(uv.x - shift       + t * 0.7);
                float b = fsin(uv.x + shift * 0.5 + t * 0.9);
                return float3(r, g, b) * _HoloStrength;
            }

            // ランダムちらつき
            float Flicker(float t)
            {
                float n = randVal(floor(t * _FlickerSpeed)) * 2.0 - 1.0;
                return 1.0 + n * _FlickerAmount;
            }

            // ─── 頂点シェーダー ────────────────────────────────────────

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPos = v.vertex;
                OUT.vertex   = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color    = v.color;
                return OUT;
            }

            // ─── フラグメントシェーダー ────────────────────────────────

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float  t  = _LocalTime;

                // エッジグロー（縁だけ光る）
                float  glow    = EdgeGlow(uv, _GlowWidth);
                float3 glowCol = _GlowColor.rgb * glow * _GlowIntensity;

                // スキャンライン
                float  scan    = Scanline(uv, t);
                float3 scanCol = _ScanlineColor.rgb * scan;

                // ホログラム
                float3 holoCol = HologramShift(uv, t) * _HoloColor.rgb;

                // 合成（加算なので全て足すだけ）
                float3 col = (glowCol + scanCol + holoCol) * Flicker(t);

                // _Intensity でフェードイン/アウト（0=消灯, 1=全灯）
                col *= _Intensity;

                // UIクリッピング対応
                float alpha = 1.0;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
                #endif

                // 加算合成なので alpha は rgb に掛けて返す（SV_Target の a は無視される）
                return fixed4(col * alpha, 1.0);
            }
            ENDCG
        }
    }
}
