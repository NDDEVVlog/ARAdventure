Shader "Custom/URP_StencilMask"
{
    Properties
    {
        [IntRange] _StencilID ("Stencil ID", Range(0, 255)) = 1
    }
    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque" 
            "Queue" = "Geometry-1" 
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "StencilMask"
            ColorMask 0      // Không vẽ màu
            ZWrite Off       // Thường tắt để mask không ảnh hưởng depth
            // ZTest LEqual   // Có thể bật nếu cần

            Stencil
            {
                Ref [_StencilID]
                Comp Always
                Pass Replace     // Ghi giá trị _StencilID vào stencil buffer
            }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
            };

            Varyings vert (Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                return output;
            }

            half4 frag (Varyings input) : SV_Target
            {
                return 0;   // Không trả về màu
            }
            ENDHLSL
        }
    }
}