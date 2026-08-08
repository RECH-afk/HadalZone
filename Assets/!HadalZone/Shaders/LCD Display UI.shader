Shader "RKS/LCD Display"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        [HDR] _Color ("Tint", Color) = (1,1,1,1)
        _PixelSize ("Pixel size", Range(0.001, 0.1)) = 0.01
        _Distortion ("Screen Curve / Vignette", Range(0, 1)) = 0.25
        _NoiseAmount ("Noise Amount", Range(0, 0.2)) = 0.03
        _Speed ("Sweep Beam Speed", Range(0, 5)) = 1
        [Toggle(UNITY_UI_ALPHACLIP)] _UseUIAlphaClip ("Use Alpha Clip", Float) = 0
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "RenderType" = "Transparent"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }

        Pass
        {
            Name "LCDUI"
            Tags { "LightMode" = "SRPDefaultUnlit" }
            Blend One OneMinusSrcAlpha
            ZWrite Off
            ZTest LEqual
            Cull Off
            ColorMask [_ColorMask]
            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            HLSLPROGRAM
            #pragma target 4.5
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile __ UNITY_UI_CLIP_RECT
            #pragma multi_compile __ UNITY_UI_ALPHACLIP

            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
            #include "Packages/com.unity.render-pipelines.high-definition/Runtime/ShaderLibrary/ShaderVariables.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float4 _Color;
            float _PixelSize;
            float _Distortion;
            float _NoiseAmount;
            float _Speed;
            float4 _ClipRect;

            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                float3 ws = TransformObjectToWorld(v.positionOS);
                o.positionCS = TransformWorldToHClip(ws);
                o.worldPosition = float4(ws, 1.0);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            float UnityGet2DClipping(float2 pos, float4 clipRect)
            {
                float2 inside = step(clipRect.xy, pos) * step(pos, clipRect.zw);
                return inside.x * inside.y;
            }

            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(127.1, 311.7))) * 43758.5453123);
            }

            float Bayer4x4(float2 c)
            {
                int x = ((int)floor(c.x)) & 3;
                int y = ((int)floor(c.y)) & 3;
                const float m[16] = {
                    0,  8,  2, 10,
                    12, 4, 14, 6,
                    3, 11, 1,  9,
                    15, 7, 13, 5
                };
                return (m[y * 4 + x] + 0.5) / 16.0;
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float t = _TimeParameters.x * _Speed;

                float2 c = i.uv - 0.5;
                float r2 = dot(c, c);
                float2 uv = 0.5 + c * (1.0 - _Distortion * 0.4 * r2);

                float ps = max(_PixelSize, 1e-4);
                float2 p = uv / ps;
                float2 cell = floor(p);
                float2 f = frac(p);

                float effect = smoothstep(1.0, 3.0, 1.0 / max(fwidth(p).x, fwidth(p).y));

                float4 far = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv);
                float4 near = SAMPLE_TEXTURE2D_LOD(_MainTex, sampler_MainTex, (cell + 0.5) * ps, 0.0);

                float bayer = Bayer4x4(cell) - 0.5;
                float3 q = saturate(floor(near.rgb * 8.0 + bayer + 0.5) / 8.0);

                float sx = (1.0 - f.x) * 3.0;
                int s = (int)clamp(floor(sx), 0.0, 2.0);
                float3 gain = s == 0
                    ? float3(1.0, 0.25, 0.25)
                    : (s == 1 ? float3(0.25, 1.0, 0.25)
                              : float3(0.25, 0.25, 1.0));
                float3 lcd = q * gain;

                float gx = saturate(min(f.x, 1.0 - f.x) / 0.12);
                float gy = saturate(min(f.y, 1.0 - f.y) / 0.12);
                float pixMask = lerp(0.15, 1.0, min(gx, gy));
                float sf = frac(sx);
                float subMask = lerp(0.4, 1.0, saturate(min(sf, 1.0 - sf) / 0.15));
                lcd *= pixMask * subMask;

                float4 tint = i.color * _Color;

                float bar = smoothstep(0.25, 0.0, abs(i.uv.y - frac(t * 0.12)));
                float sweep = 1.0 + 0.15 * bar;

                float vig = 1.0 - _Distortion * 0.7 * smoothstep(0.08, 0.42, r2);

                float noiseTime = floor(_TimeParameters.x * 12.0);
                float noiseVal = (hash(cell + noiseTime) - 0.5) * _NoiseAmount;
                float lum = dot(near.rgb, float3(0.299, 0.587, 0.114));
                noiseVal *= lum;

                float3 closeCol = (lcd + noiseVal) * tint.rgb * sweep * vig;
                float3 farCol = far.rgb * tint.rgb * sweep * vig;

                float3 col = lerp(farCol, closeCol, effect);
                float alpha = lerp(far.a, near.a, effect) * tint.a;

                #if defined(UNITY_UI_CLIP_RECT)
                    alpha *= UnityGet2DClipping(i.worldPosition.xy, _ClipRect);
                #endif
                #if defined(UNITY_UI_ALPHACLIP)
                    clip(alpha - 0.001);
                #endif

                return half4(col * alpha, alpha);
            }
            ENDHLSL
        }
    }
    Fallback "UI/Default"
}