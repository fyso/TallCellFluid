﻿Shader "Custom/DrawFoam"
{
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "DrawFoam"
            }

            Blend One OneMinusSrcAlpha
            ZTest On
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma target 5.0

            #pragma vertex DrawFoamPassVS
            #pragma fragment DrawFoamPassFS
            #pragma multi_compile _ _OUTSIDE_FOAM
            #include "../ShaderLibrary/Common.hlsl"

            float _FoamRadius;
            float _Diffusion;
            float _MotionBlurScale;
            StructuredBuffer<float3> _FoamPositionBuffer;
            StructuredBuffer<float3> _FoamVelocityBuffer;
            StructuredBuffer<float> _FoamLifeTimeBuffer;

        #ifdef _OUTSIDE_FOAM
            Texture2D<float4> _FluidNormalRT;
        #endif

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : VAR_SCREEN_UV;
                nointerpolation float lifeTime : VAR_LIFETIME;
                nointerpolation float velocityFade : VAR_VELOCITY_FADE;
            };

            Varyings DrawFoamPassVS(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                float3 sphereCenterWS = _FoamPositionBuffer[instanceID];
                float3 sphereCenterVS = TransformWorldToView(sphereCenterWS);
                float3 VelocityVS = _FoamVelocityBuffer[instanceID];

                float lifeTime = _FoamLifeTimeBuffer[instanceID];
                float lifeFade = lerp(_Diffusion, 1.0, min(1.0, lifeTime * 0.25f));
                float3 u = float3(0.0, _FoamRadius, 0.0) * lifeFade;
                float3 l = float3(_FoamRadius, 0.0, 0.0) * lifeFade;

                float velocityFade = 1.0 / (lifeFade * lifeFade);
                float velocityLength = length(VelocityVS) * _MotionBlurScale;

                if (velocityLength > 0.5)
                {
                    float len = max(_FoamRadius, velocityLength * 0.016);
                    velocityFade = min(1.0, 2.0 / (len / _FoamRadius));

                    u = normalize(VelocityVS) * max(_FoamRadius, velocityLength * 0.016);	// assume 60hz
                    l = normalize(cross(u, float3(0.0, 0.0, -1.0))) * _FoamRadius;
                }

                Varyings output;
                output.lifeTime = lifeTime;
                output.velocityFade = velocityFade;

                switch (vertexID)
                {
                    case 0:
                        output.uv = float2(-1, -1);
                        output.positionCS = TransformWViewToHClip(sphereCenterVS - u - l);
                        break;

                    case 1:
                        output.uv = float2(-1, 1);
                        output.positionCS = TransformWViewToHClip(sphereCenterVS - u + l);
                        break;

                    case 2:
                        output.uv = float2(1, -1);
                        output.positionCS = TransformWViewToHClip(sphereCenterVS + u - l);
                        break;

                    case 3:
                        output.uv = float2(1, -1);
                        output.positionCS = TransformWViewToHClip(sphereCenterVS + u - l);
                        break;

                    case 4:
                        output.uv = float2(-1, 1);
                        output.positionCS = TransformWViewToHClip(sphereCenterVS - u + l);
                        break;

                    case 5:
                        output.uv = float2(1, 1);
                        output.positionCS = TransformWViewToHClip(sphereCenterVS + u + l);
                        break;
                }

                return output;
            }

            float4 DrawFoamPassFS(Varyings input) : SV_Target
            {
                float xy_PlaneProj = dot(input.uv, input.uv);
                if (xy_PlaneProj > 1.0f) discard;

            #ifdef _OUTSIDE_FOAM
                float fluidDepth = _FluidNormalRT.Load(int3(input.positionCS.xy, 0)).w;
                if (input.positionCS.z < fluidDepth) discard;
            #endif

                float lifeFade = min(1.0, input.lifeTime * 0.125);
                float alpha = lifeFade * input.velocityFade * sqrt(1.0f - xy_PlaneProj);
                return float4(alpha, alpha, alpha, alpha);
            }

            ENDHLSL
        }
    }
}