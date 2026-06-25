Shader "StarForge/BlackHoleInfall"
{
    Properties
    {
        _NearColor ("Near Horizon Color", Color) = (0.9, 0.8, 1, 1)
        _FarColor ("Outer Light Color", Color) = (0.2, 0.55, 1, 1)
        _Speed ("Infall Speed", Range(0, 4)) = 1.2
        _Twist ("Gravity Bend", Range(2, 18)) = 9
        _Arms ("Filament Count", Range(3, 16)) = 8
        _Intensity ("Intensity", Range(0, 4)) = 1
        _InnerRadius ("Inner Radius", Range(0, 0.8)) = 0.2
        _OuterRadius ("Outer Radius", Range(0.4, 1.4)) = 1
        _Seed ("Layer Seed", Range(0, 12)) = 0
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "Queue" = "Transparent+55"
            "RenderPipeline" = "UniversalPipeline"
        }

        Pass
        {
            Name "ForwardUnlit"
            Tags { "LightMode" = "UniversalForward" }

            Cull Off
            ZWrite Off
            ZTest LEqual
            Blend One One

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
                half4 _NearColor;
                half4 _FarColor;
                half _Speed;
                half _Twist;
                half _Arms;
                half _Intensity;
                half _InnerRadius;
                half _OuterRadius;
                half _Seed;
            CBUFFER_END

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }

            half Hash11(half value)
            {
                return frac(sin(value * 127.1h) * 43758.5453h);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                half2 coordinates = input.uv * 2.0h - 1.0h;
                half radius = length(coordinates);
                half angle = atan2(coordinates.y, coordinates.x);
                half time = _Time.y;

                half outerFade = 1.0h - smoothstep(_OuterRadius - 0.22h, _OuterRadius, radius);
                half innerFade = smoothstep(_InnerRadius, _InnerRadius + 0.075h, radius);
                half envelope = innerFade * outerFade;
                half normalizedRadius = saturate(
                    (radius - _InnerRadius) / max(0.001h, _OuterRadius - _InnerRadius));
                half gravityCurve = (1.0h - normalizedRadius) * (1.0h - normalizedRadius);
                half bentAngle =
                    angle +
                    radius * _Twist +
                    gravityCurve * sin(angle * 2.0h + time * 0.7h + _Seed) * 1.7h;

                half laneNoise = Hash11(floor((angle + 3.14159h) * 7.5h + _Seed * 11.0h));
                half primaryLane = pow(
                    saturate(sin(bentAngle * _Arms + laneNoise * 5.0h) * 0.5h + 0.5h),
                    18.0h);
                half secondaryLane = pow(
                    saturate(sin(bentAngle * (_Arms * 0.53h) - 1.8h + _Seed) * 0.5h + 0.5h),
                    28.0h);
                half filaments = saturate(primaryLane + secondaryLane * 0.7h);

                // Constant-phase highlights move toward smaller radii as time advances.
                half pulsePhase = frac(
                    radius * (4.2h + laneNoise * 1.6h) +
                    time * _Speed +
                    laneNoise +
                    _Seed * 0.17h);
                half head = 1.0h - smoothstep(0.04h, 0.18h, abs(pulsePhase - 0.12h));
                half tail = smoothstep(0.12h, 0.2h, pulsePhase) *
                    (1.0h - smoothstep(0.2h, 0.82h, pulsePhase));
                half movingStreak = saturate(head + tail * 0.42h);

                half horizonBoost = pow(saturate(1.0h - normalizedRadius), 1.6h);
                half doppler = 0.72h + 0.28h * cos(angle + _Seed * 1.3h);
                half flicker = 0.82h +
                    0.12h * sin(time * 5.7h + _Seed) +
                    0.06h * sin(time * 13.1h + angle * 3.0h);

                half brightness = envelope * filaments * movingStreak;
                brightness *= (0.55h + horizonBoost * 1.7h) * doppler * flicker * _Intensity;

                half3 color = lerp(_FarColor.rgb, _NearColor.rgb, horizonBoost);
                color += _NearColor.rgb * horizonBoost * head * 0.18h;
                half alpha = saturate(brightness * (0.38h + horizonBoost * 0.42h));
                return half4(color * brightness, alpha);
            }
            ENDHLSL
        }
    }
}
