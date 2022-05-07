Shader "Custom/VisualParticle"
{
    Properties
    {
        _ParticleRadius ("Radius", float) = 0.25
        _ParticleColor ("Color", Color) = (.25, .5, .5, 1)
        [KeywordEnum(VELOCITY, PARTICLETYPE)] _visualDataType("Visual DataType", float) = 0
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            Cull Off
            CGPROGRAM
            #pragma vertex GenerateDepthPassVertex
            #pragma fragment SpriteGenerateDepthPassFrag
            #pragma enable_d3d11_debug_symbols

            #pragma multi_compile_local __ _VISUALDATATYPE_VELOCITY _VISUALDATATYPE_PARTICLETYPE

            #include "UnityCG.cginc"
            #include "Lighting.cginc"

            float4x4 GetViewToHClipMatrix()
            {
                return UNITY_MATRIX_P;
            }

            float4x4 GetWorldToViewMatrix()
            {
                return UNITY_MATRIX_V;
            }

            float4 TransformWViewToHClip(float3 positionVS)
            {
                return mul(GetViewToHClipMatrix(), float4(positionVS, 1.0));
            }

            float3 TransformWorldToView(float3 positionWS)
            {
                return mul(GetWorldToViewMatrix(), float4(positionWS, 1.0)).xyz;
            }

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 col : TEXCOORD0;
                float2 uv : VAR_SCREEN_UV;
            };

            struct Targets
            {
                float4 fluidDepth : SV_TARGET0;
            };

            uniform float _ParticleRadius;
            uniform float4 _ParticleColor;

            StructuredBuffer<float3> _particlePositionBuffer;
            StructuredBuffer<float3> _particleVelocityBuffer;
            StructuredBuffer<uint> _particleFilterBuffer;

            Varyings GenerateDepthPassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings result;
                float3 positionWS = _particlePositionBuffer[instanceID];
                float3 sphereCenter = float4(TransformWorldToView(positionWS), 1.0f).xyz;
                switch (vertexID)
                {
                    case 0:
                        result.uv = float2(-1, -1);
                        break;

                    case 1:
                        result.uv = float2(-1, 3);
                        break;

                    case 2:
                        result.uv = float2(3, -1);
                        break;
                    }
                result.positionCS = TransformWViewToHClip(sphereCenter + float3(_ParticleRadius * result.uv, 0.0f));

#if _VISUALDATATYPE_VELOCITY
                float3 Velocity = _particleVelocityBuffer[instanceID];
                float ClampVel = clamp(length(Velocity), 0.0f, 20.0f) / 20.0f;
                result.col = ClampVel * float4(1.0f, 1.0f, 1.0f, 0.0f) + _ParticleColor;
                //result.col = float4(Velocity, 1.0f);
#elif _VISUALDATATYPE_PARTICLETYPE
                uint filter = _particleFilterBuffer[instanceID];
                if(filter == 0)
                    result.col = float4(1.0f, 0.0f, 0.0f, 1.0f);
                else if (filter == 1)
                    result.col = float4(0.0f, 1.0f, 0.0f, 1.0f);
                else if (filter == 2)
                    result.col = float4(0.0f, 0.0f, 1.0f, 1.0f);
                else if (filter == 3)
                    result.col = float4(1.0f, 1.0f, 1.0f, 1.0f);
                else
                    result.col = float4(0.0f, 0.0f, 0.0f, 1.0f);
#else
                result.col = _ParticleColor;
#endif

                return result;
            }

            Targets SpriteGenerateDepthPassFrag(Varyings input)
            {
                Targets result;

                float3 normalVS;
                normalVS.xy = input.uv;
                float xy_PlaneProj = dot(normalVS.xy, normalVS.xy);
                if (xy_PlaneProj > 1.0f) discard;

                result.fluidDepth = input.col;
                return result;
            }

            ENDCG
        }
    }
}