Shader "StarForge/PremiumRim"
{
    Properties
    {
        _Color ("Primary Color", Color) = (0.5, 0.8, 1, 1)
        _SecondaryColor ("Secondary Color", Color) = (1, 0.8, 0.4, 1)
        _FresnelPower ("Fresnel Power", Range(1, 10)) = 4
        _Intensity ("Intensity", Range(0, 3)) = 0.6
        _FlowSpeed ("Flow Speed", Range(-3, 3)) = 0.4
        _Pulse ("Pulse", Range(0, 2)) = 1
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+60"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "Premium Rim"
            Tags { "LightMode" = "UniversalForward" }

            Blend SrcAlpha One
            Cull Back
            ZWrite Off
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
                float3 normalWS : TEXCOORD1;
                float2 uv : TEXCOORD2;
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _Color;
                float4 _SecondaryColor;
                float _FresnelPower;
                float _Intensity;
                float _FlowSpeed;
                float _Pulse;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                VertexPositionInputs positionInputs = GetVertexPositionInputs(input.positionOS.xyz);
                output.positionCS = positionInputs.positionCS;
                output.positionWS = positionInputs.positionWS;
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = input.uv;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float3 normalWS = normalize(input.normalWS);
                float3 viewDirection = normalize(GetWorldSpaceViewDir(input.positionWS));
                float fresnel = pow(saturate(1.0 - dot(normalWS, viewDirection)), _FresnelPower);

                float flowA = sin((input.uv.x * 17.0 + input.uv.y * 7.0) + _Time.y * _FlowSpeed);
                float flowB = sin((input.uv.x * 5.0 - input.uv.y * 13.0) - _Time.y * _FlowSpeed * 0.63);
                float flow = saturate(0.5 + flowA * 0.28 + flowB * 0.22);
                float3 color = lerp(_Color.rgb, _SecondaryColor.rgb, flow);
                float alpha = fresnel * _Intensity * _Pulse * lerp(0.48, 1.0, flow);
                return half4(color * lerp(0.72, 1.08, flow), saturate(alpha));
            }
            ENDHLSL
        }
    }

    FallBack Off
}
