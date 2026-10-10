// Settlement kits and trees (art spike, issues #31 #32 #34 #37). Vertex colour is the sRGB albedo; its alpha picks a
// facade's window pattern (KitMesh.Facade), drawn from uv0 = the wall's (along, up) position in real metres, so storeys and
// windows keep real proportions on symbolic-size buildings, box-filtered so they fade to their mean instead of shimmering
// when a storey is a pixel or two. Lit like the terrain (the sun with its shadows, the trilight ambient through SH) and
// hazed with the terrain's own ScHazeAmount, so kits sit in the same air as the ground. GPU instancing for the trees, with a
// per-instance tint hashed from the instance's origin (no per-instance buffer: WebGL 2 has instanced arrays only).
// Shadow bias at kit scale (#37): the caster's depth and normal offsets are the sun's (_ShadowBias: about one shadow texel,
// ~100 m at the gameplay zoom) times _KitDepthBias and _KitNormalBias, so a 300 m house's shadow starts at its walls
// instead of a texel away; the receiver samples its shadow _KitReceiveOffset metres out along its normal, which keeps the
// lit walls free of acne without pushing the cast shadow away.
Shader "Statecraft/Kit"
{
    Properties
    {
        _BaseColor ("Tint", Color) = (1, 1, 1, 1)
        _Jitter ("Per-instance tint jitter", Range(0, 0.5)) = 0
        _WindowDark ("Window darkness", Range(0, 1)) = 0.55
        _KitDepthBias ("Caster depth bias (x the sun's)", Range(0, 1)) = 0.25
        _KitNormalBias ("Caster normal bias (x the sun's)", Range(0, 1)) = 0.0
        _KitReceiveOffset ("Receiver normal offset (m)", Float) = 60
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry" "RenderPipeline" = "UniversalPipeline" }
        HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
        CBUFFER_START(UnityPerMaterial)
            half4 _BaseColor;
            half _Jitter, _WindowDark, _KitDepthBias, _KitNormalBias;
            float _KitReceiveOffset;
        CBUFFER_END
        ENDHLSL

        Pass
        {
            Name "Kit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile_fragment _ _SHADOWS_SOFT _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Assets/Shaders/StatecraftMap.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION; float3 normalOS : NORMAL; half4 color : COLOR; float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };
            struct Varyings
            {
                float4 positionCS : SV_POSITION; float3 positionWS : TEXCOORD0; float3 normalWS : TEXCOORD1;
                half4 color : TEXCOORD2; float2 uv : TEXCOORD3; half tint : TEXCOORD4;
            };

            Varyings Vert(Attributes v)
            {
                Varyings o;
                UNITY_SETUP_INSTANCE_ID(v);
                VertexPositionInputs p = GetVertexPositionInputs(v.positionOS.xyz);
                o.positionCS = p.positionCS; o.positionWS = p.positionWS;
                o.normalWS = TransformObjectToWorldNormal(v.normalOS);
                o.color = v.color; o.uv = v.uv;
                float3 origin = TransformObjectToWorld(float3(0, 0, 0));
                o.tint = ScHash(origin.xz * 0.0137 + 3.1);
                return o;
            }

            // A 1D band [a, b] of a period-1 pattern, box-filtered over the pixel's footprint w (in periods).
            float Band(float x, float a, float b, float w)
            {
                float lo = saturate((frac(x) - a) / max(w, 1e-4) + 0.5), hi = saturate((frac(x) - b) / max(w, 1e-4) + 0.5);
                return lo - hi;
            }

            // Window mask (1 = glass) and its mean over a whole storey, for each facade style.
            float Windows(float style, float2 uv, float2 fw, out float mean)
            {
                float storey = 3.3, bay = style > 0.75 ? 1.7 : (style > 0.4 ? 4.2 : 3.0);
                float wv = fw.y / storey, wu = fw.x / bay;
                float a0, a1, b0, b1;
                if (style > 0.75) { a0 = 0.38; a1 = 0.80; b0 = 0.10; b1 = 0.95; }        // white city: ribbon windows
                else if (style > 0.4) { a0 = 0.40; a1 = 0.72; b0 = 0.38; b1 = 0.62; }   // village / moshava: a few small windows
                else { a0 = 0.36; a1 = 0.74; b0 = 0.34; b1 = 0.64; }                    // stone: arched-height windows
                mean = (a1 - a0) * (b1 - b0);
                float m = Band(uv.y / storey, a0, a1, wv) * Band(uv.x / bay, b0, b1, wu);
                // Fade to the mean once a storey is under about three pixels: no moire at the gameplay zoom.
                return lerp(mean, m, saturate(1.5 - wv * 3.0)) * step(0.0, uv.y);
            }

            half4 Frag(Varyings i) : SV_Target
            {
                float3 n = normalize(i.normalWS);
                half3 albedo = SRGBToLinear(i.color.rgb) * _BaseColor.rgb;
                // Per-instance (tree) tint: brighter or darker, a touch warmer or cooler.
                float t = i.tint - 0.5;
                albedo *= 1.0 + _Jitter * t * 2.0;
                albedo.r *= 1.0 + _Jitter * (frac(i.tint * 7.31) - 0.5);
                float style = i.color.a;
                float2 fw = fwidth(i.uv);   // outside the branch: derivatives need uniform control flow
                if (style > 0.1)
                {
                    float mean;
                    float glass = Windows(style, i.uv, fw, mean);
                    // White-city balconies: a lighter slab line and its shadow under it, every storey.
                    if (style > 0.75)
                    {
                        float fv = fw.y / 3.3;
                        float slab = Band(i.uv.y / 3.3, 0.28, 0.36, fv), under = Band(i.uv.y / 3.3, 0.36, 0.42, fv);
                        float fade = saturate(1.5 - fv * 3.0);
                        albedo *= 1.0 + 0.10 * lerp(0.08, slab, fade) - 0.22 * lerp(0.06, under, fade);
                    }
                    albedo *= 1.0 - _WindowDark * glass;
                    // Contact darkening in the bottom real 3 m of a wall.
                    albedo *= lerp(0.72, 1.0, saturate(i.uv.y / 3.0));
                }
                Light sun = GetMainLight(TransformWorldToShadowCoord(i.positionWS + n * _KitReceiveOffset));
                float ndl = saturate(dot(n, sun.direction));
                half3 lit = albedo * (SampleSH(n) + sun.color * ndl * sun.shadowAttenuation);
                float h = _ScHaze.a * ScHazeAmount(i.positionWS);
                return half4(lerp(lit, _ScHaze.rgb, h), 1.0);
            }
            ENDHLSL
        }
        Pass
        {
            Name "ShadowCaster"
            Tags { "LightMode" = "ShadowCaster" }
            ZWrite On ZTest LEqual ColorMask 0
            Cull Back
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Shadows.hlsl"
            float3 _LightDirection;
            struct Attributes { float4 positionOS : POSITION; float3 normalOS : NORMAL; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 Vert(Attributes v) : SV_POSITION
            {
                UNITY_SETUP_INSTANCE_ID(v);
                float3 positionWS = TransformObjectToWorld(v.positionOS.xyz);
                float3 normalWS = TransformObjectToWorldNormal(v.normalOS);
                float invNdotL = 1.0 - saturate(dot(_LightDirection, normalWS));
                positionWS += _LightDirection * _ShadowBias.x * _KitDepthBias;
                positionWS += normalWS * (invNdotL * _ShadowBias.y * _KitNormalBias);
                return ApplyShadowClamping(TransformWorldToHClip(positionWS));
            }
            half4 Frag() : SV_Target { return 0; }
            ENDHLSL
        }
        Pass
        {
            Name "DepthOnly"
            Tags { "LightMode" = "DepthOnly" }
            ZWrite On
            ColorMask R
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #pragma multi_compile_instancing
            struct Attributes { float4 positionOS : POSITION; UNITY_VERTEX_INPUT_INSTANCE_ID };
            float4 Vert(Attributes v) : SV_POSITION { UNITY_SETUP_INSTANCE_ID(v); return TransformObjectToHClip(v.positionOS.xyz); }
            half Frag() : SV_Target { return 0; }
            ENDHLSL
        }
    }
}
