Shader "VN/UISeparableBlur"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _BlurRadius ("Blur Radius", Float) = 1
        _BlurTexelSize ("Blur Texel Size", Vector) = (0.001, 0.001, 1, 1)
    }

    SubShader
    {
        Tags
        {
            "RenderPipeline"="UniversalPipeline"
        }

        ZWrite Off
        ZTest Always
        Cull Off

        Pass
        {
            Name "Horizontal"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragHorizontal

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _BlurRadius;
            float4 _BlurTexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 SampleBlur(float2 uv, float2 direction)
            {
                float2 offset = direction * _BlurRadius;

                half4 color = 0;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 4.0) * 0.05;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 3.0) * 0.09;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 2.0) * 0.12;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 1.0) * 0.15;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv)              * 0.18;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 1.0) * 0.15;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 2.0) * 0.12;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 3.0) * 0.09;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 4.0) * 0.05;

                return color;
            }

            half4 FragHorizontal(Varyings input) : SV_Target
            {
                return SampleBlur(input.uv, float2(_BlurTexelSize.x, 0));
            }

            ENDHLSL
        }

        Pass
        {
            Name "Vertical"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment FragVertical

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            float _BlurRadius;
            float4 _BlurTexelSize;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half4 SampleBlur(float2 uv, float2 direction)
            {
                float2 offset = direction * _BlurRadius;

                half4 color = 0;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 4.0) * 0.05;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 3.0) * 0.09;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 2.0) * 0.12;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv - offset * 1.0) * 0.15;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv)              * 0.18;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 1.0) * 0.15;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 2.0) * 0.12;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 3.0) * 0.09;
                color += SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv + offset * 4.0) * 0.05;

                return color;
            }

            half4 FragVertical(Varyings input) : SV_Target
            {
                return SampleBlur(input.uv, float2(0, _BlurTexelSize.y));
            }

            ENDHLSL
        }
    }
}