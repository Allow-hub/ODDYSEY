Shader "Custom/ElectricCardHighlight"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}

        _GlowColor    ("Glow Color",     Color)        = (0.3, 0.8, 1.0, 1.0)
        _GlowRadius   ("Glow Radius",    Range(0, 2))  = 1.0
        _GlowStrength ("Glow Strength",  Range(0, 5))  = 2.0
        _Intensity    ("Intensity",      Range(0, 1))  = 0

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
        Blend One One      // 加算合成
        ColorMask [_ColorMask]

        Pass
        {
            CGPROGRAM
            #pragma vertex   vert
            #pragma fragment frag
            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            sampler2D _MainTex;
            float4    _MainTex_ST;
            fixed4    _GlowColor;
            float     _GlowRadius;
            float     _GlowStrength;
            float     _Intensity;
            float4    _ClipRect;

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                float2 texcoord : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPos = v.vertex;
                OUT.vertex   = UnityObjectToClipPos(v.vertex);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                // UV の中心(0.5, 0.5)からの距離
                float2 centered = uv - 0.5;
                float  dist     = length(centered);

                // 中心が最も明るく、外側にいくほど暗くなるグラデーション
                // 1 - smoothstep で dist=0 のとき 1、dist=radius のとき 0
                float glow = 1.0 - smoothstep(0.0, _GlowRadius * 0.5, dist);
                glow = pow(glow, 2.0) * _GlowStrength;

                float3 col = _GlowColor.rgb * glow * _Intensity;

                float alpha = 1.0;
                #ifdef UNITY_UI_CLIP_RECT
                alpha *= UnityGet2DClipping(IN.worldPos.xy, _ClipRect);
                #endif

                return fixed4(col * alpha, 1.0);
            }
            ENDCG
        }
    }
}
