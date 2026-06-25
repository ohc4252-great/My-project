Shader "StarForge/BlackHoleDisk"
{
    Properties
    {
        _InnerColor ("Inner Color", Color) = (1, 0.75, 0.3, 1)
        _OuterColor ("Outer Color", Color) = (0.35, 0.08, 0.65, 1)
        _FlowSpeed ("Flow Speed", Range(-3, 3)) = 0.8
        _Turbulence ("Turbulence", Range(0, 3)) = 1
        _Asymmetry ("Doppler Asymmetry", Range(0, 0.8)) = 0.25
        _Intensity ("Intensity", Range(0, 3)) = 1
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
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha One

            HLSLPROGRAM
            #pragma target 2.0
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

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

            CBUFFER_START(UnityPerMaterial)
                half4 _InnerColor;
                half4 _OuterColor;
                half _FlowSpeed;
                half _Turbulence;
                half _Asymmetry;
                half _Intensity;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half Hash21(half2 coordinates)
            {
                coordinates = frac(coordinates * half2(123.34h, 456.21h));
                coordinates += dot(coordinates, coordinates + 45.32h);
                return frac(coordinates.x * coordinates.y);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half radial = saturate(input.uv.y);
                half phase = input.uv.x * 6.2831853h;
                half time = _Time.y * _FlowSpeed;

                half broadFlow = sin(phase * 2.0h - time * 2.4h + radial * 10.0h);
                half fineFlow = sin(phase * 6.0h + time * 3.1h - radial * 21.0h);
                half noise = Hash21(floor(half2(input.uv.x * 48.0h - time, radial * 20.0h)));
                half streams = saturate(
                    0.48h +
                    broadFlow * 0.25h * _Turbulence +
                    fineFlow * 0.13h * _Turbulence +
                    (noise - 0.5h) * 0.35h);

                half edgeFade =
                    smoothstep(0.0h, 0.08h, radial) *
                    (1.0h - smoothstep(0.78h, 1.0h, radial));
                half innerHeat = pow(saturate(1.0h - radial), 2.4h);
                half doppler = 1.0h + cos(phase) * _Asymmetry;

                half3 color = lerp(_InnerColor.rgb, _OuterColor.rgb, radial);
                color *= (0.65h + innerHeat * 1.4h + streams * 0.75h) * doppler * _Intensity;

                half alpha = edgeFade * (0.35h + streams * 0.65h) * saturate(_Intensity);
                return half4(color, alpha);
            }
            ENDHLSL
        }
    }
}
