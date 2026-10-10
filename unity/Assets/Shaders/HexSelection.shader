// The selected hex's outline band (MapScene.Select). Unlit, one colour, drawn after the hex overlay with a depth offset
// towards the camera, so the overlay's lines under the band's edge never z-fight with it, even at the camera's maximum
// distance where a 24-bit depth step is tens of metres (fix round 1, M2). Both faces: the band is seen from every yaw.
Shader "Statecraft/HexSelection"
{
    Properties
    {
        [MainColor] _BaseColor("Color", Color) = (1, 0.84, 0.38, 1)
    }
    SubShader
    {
        Tags { "RenderType" = "Opaque" "Queue" = "Geometry+10" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Unlit"
            Tags { "LightMode" = "UniversalForward" }
            Cull Off
            ZWrite On
            ZTest LEqual
            Offset -1, -4
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; };
            Varyings Vert(Attributes input)
            {
                Varyings o;
                o.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return o;
            }
            half4 Frag(Varyings input) : SV_Target { return _BaseColor; }
            ENDHLSL
        }
    }
}
