Shader "UI/RadarScanUI"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        // Unity UI Mask(Stencil) 지원용 프로퍼티들
        [HideInInspector] _StencilComp ("Stencil Comparison", Float) = 8
        [HideInInspector] _Stencil ("Stencil ID", Float) = 0
        [HideInInspector] _StencilOp ("Stencil Operation", Float) = 0
        [HideInInspector] _StencilWriteMask ("Stencil Write Mask", Float) = 255
        [HideInInspector] _StencilReadMask ("Stencil Read Mask", Float) = 255
        [HideInInspector] _ColorMask ("Color Mask", Float) = 15

        _BackgroundColor ("Background Color", Color) = (0,0,0,0.3)
        _VignetteStrength ("Vignette Strength", Range(0,2)) = 1.1

        _GridColor ("Grid Color", Color) = (0,1,0,0.18)
        _GridRings ("Grid Rings", Range(0,8)) = 3
        _GridRingThickness ("Grid Ring Thickness", Range(0.0005,0.02)) = 0.004
        _GridLines ("Grid Lines", Range(0,16)) = 6
        _GridLineThickness ("Grid Line Thickness", Range(0.0005,0.02)) = 0.003

        _CrosshairColor ("Crosshair Color", Color) = (0,1,0,0.25)
        _CrosshairThickness ("Crosshair Thickness", Range(0.0005,0.02)) = 0.003

        _ScanColor ("Scan Color", Color) = (0,1,0,0.35)
        _ScanSpeed ("Scan Speed", Range(0,10)) = 1.5
        _ScanWidth ("Scan Width", Range(0.001,0.25)) = 0.06

        _NoiseStrength ("Noise Strength", Range(0,1)) = 0.08
        _NoiseScale ("Noise Scale", Range(1,64)) = 18
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            Name "RadarScanUI"

            Stencil
            {
                Ref [_Stencil]
                Comp [_StencilComp]
                Pass [_StencilOp]
                ReadMask [_StencilReadMask]
                WriteMask [_StencilWriteMask]
            }

            ColorMask [_ColorMask]

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

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
                float2 uv       : TEXCOORD0;
                float4 worldPos : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;

            fixed4 _BackgroundColor;
            float _VignetteStrength;

            fixed4 _GridColor;
            float _GridRings;
            float _GridRingThickness;
            float _GridLines;
            float _GridLineThickness;

            fixed4 _CrosshairColor;
            float _CrosshairThickness;

            fixed4 _ScanColor;
            float _ScanSpeed;
            float _ScanWidth;

            float _NoiseStrength;
            float _NoiseScale;

            v2f vert(appdata_t v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
                o.worldPos = v.vertex;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.texcoord;
                o.color = v.color * _Color;
                return o;
            }

            // cheap hash noise
            float hash21(float2 p)
            {
                p = frac(p * float2(123.34, 456.21));
                p += dot(p, p + 45.32);
                return frac(p.x * p.y);
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // UI clip rect
                #ifdef UNITY_UI_CLIP_RECT
                i.color.a *= UnityGet2DClipping(i.worldPos.xy, _ClipRect);
                #endif
                #ifdef UNITY_UI_ALPHACLIP
                clip(i.color.a - 0.001);
                #endif

                // sprite alpha mask (keeps circle sprite working)
                fixed4 sprite = tex2D(_MainTex, i.uv) * i.color;

                float2 p = i.uv - 0.5;
                float r = length(p);

                // circle mask (redundant with sprite alpha, but helps when sprite is white)
                float inside = step(r, 0.5);
                if (inside <= 0.0) return fixed4(0,0,0,0);

                // background + vignette
                float vignette = saturate(1.0 - (r / 0.5));
                vignette = pow(vignette, _VignetteStrength);
                fixed4 col = _BackgroundColor;
                col.rgb *= (0.65 + 0.35 * vignette);

                // rings
                if (_GridRings > 0.0)
                {
                    float ringIdx = r / 0.5 * _GridRings;
                    float ringFrac = abs(frac(ringIdx) - 0.5) * 2.0; // 0 at ring
                    float ringMask = smoothstep(_GridRingThickness, 0.0, ringFrac);
                    fixed4 gridCol = fixed4(_GridColor.r, _GridColor.g, _GridColor.b, 1.0);
                    col = lerp(col, gridCol, ringMask * _GridColor.a);
                }

                // radial lines
                if (_GridLines > 0.0)
                {
                    float ang = atan2(p.y, p.x); // [-pi, pi]
                    float a01 = (ang + UNITY_PI) / (2.0 * UNITY_PI); // [0,1]
                    float lineIdx = a01 * _GridLines;
                    float lineFrac = abs(frac(lineIdx) - 0.5) * 2.0;
                    float radialLineMask = smoothstep(_GridLineThickness, 0.0, lineFrac);
                    fixed4 gridCol2 = fixed4(_GridColor.r, _GridColor.g, _GridColor.b, 1.0);
                    col = lerp(col, gridCol2, radialLineMask * _GridColor.a);
                }

                // crosshair
                float cx = smoothstep(_CrosshairThickness, 0.0, abs(i.uv.x - 0.5));
                float cy = smoothstep(_CrosshairThickness, 0.0, abs(i.uv.y - 0.5));
                float cross = max(cx, cy);
                fixed4 crossCol = fixed4(_CrosshairColor.r, _CrosshairColor.g, _CrosshairColor.b, 1.0);
                col = lerp(col, crossCol, cross * _CrosshairColor.a);

                // scan sweep (rotating angle)
                float t = _Time.y * _ScanSpeed;
                float scanAng = frac(t / (2.0 * UNITY_PI)) * (2.0 * UNITY_PI);
                float ang2 = atan2(p.y, p.x);
                float dAng = abs(atan2(sin(ang2 - scanAng), cos(ang2 - scanAng))); // [0,pi]
                float scan = smoothstep(_ScanWidth, 0.0, dAng);
                // fade scan with distance (brighter near center)
                scan *= (0.35 + 0.65 * vignette);
                fixed4 scanCol = fixed4(_ScanColor.r, _ScanColor.g, _ScanColor.b, 1.0);
                col = lerp(col, scanCol, scan * _ScanColor.a);

                // noise
                float n = hash21(i.uv * _NoiseScale + _Time.yy);
                col.rgb += (n - 0.5) * _NoiseStrength;

                // final alpha: background alpha times sprite alpha
                col.a *= sprite.a;
                return col;
            }
            ENDCG
        }
    }
}

