Shader "Custom/DrawFluidParticles"
{
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "DrawSphere"
            }

            Blend Off
            ZTest On
            ZWrite On

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex GenerateDepthPassVertex
            #pragma fragment SpriteGenerateDepthPassFrag
            #pragma multi_compile _ _OCCLUSIONCULLDEBUG
            #include "../ShaderLibrary/Common.hlsl"

            StructuredBuffer<float3> _ParticlePositionBuffer;
            StructuredBuffer<uint> SurfaceGrid_RW;

            Texture2D _SceneDepth;
            SamplerState _point_clamp_sampler;
            float _ParticlesRadius;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : VAR_SCREEN_UV;
                nointerpolation float3 sphereCenterVS : VAR_POSITION_VS;
                #ifdef _OCCLUSIONCULLDEBUG
                   nointerpolation float3 cellOfParticleIndex3D : VAR_CELLINDEX3D;
                #endif
            };

            Varyings Clip()
            {
                Varyings output;
                output.positionCS = float4(100, 100, 100, 1);
                return output;
            }

            Varyings GenerateDepthPassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings output;

                switch (vertexID)//TODO: use equilateral triangle
                {
                case 0:
                    output.uv = float2(-1, -1);
                    break;

                case 1:
                    output.uv = float2(-1, 3);
                    break;

                case 2:
                    output.uv = float2(3, -1);
                    break;
                }

                output.sphereCenterVS = TransformWorldToView(_ParticlePositionBuffer[instanceID]);
                float3 posVS = output.sphereCenterVS;
                #ifdef _OCCLUSIONCULLDEBUG
                    posVS = mul(unity_MatrixVHistory, float4(_ParticlePositionBuffer[instanceID], 1.0f));
                #endif
                int3 tex3DIndex = viewPos2Index3D(posVS);
                #ifdef _OCCLUSIONCULLDEBUG
                    if (any(tex3DIndex < 0) || any(tex3DIndex > int3(_PerspectiveGridDimX, _PerspectiveGridDimY, _PerspectiveGridDimZ)))
                        output.cellOfParticleIndex3D = float3(1, instanceID, 0);
                    else
                    {
                        uint  cellLinerIndex = tex3DIndex2Liner(tex3DIndex);
                        uint  isSurface = SurfaceGrid_RW[cellLinerIndex];
                        output.cellOfParticleIndex3D = float3(cellLinerIndex, instanceID, isSurface);
                    }
                #else
                    if (any(tex3DIndex < 0) || any(tex3DIndex > int3(_PerspectiveGridDimX, _PerspectiveGridDimY, _PerspectiveGridDimZ)))
                        return Clip();
                    uint  cellLinerIndex = tex3DIndex2Liner(tex3DIndex);
                    uint  isSurface = SurfaceGrid_RW[cellLinerIndex];
                    if (isSurface != 1) return Clip();
                #endif
                output.positionCS = TransformWViewToHClip(output.sphereCenterVS + float3(_ParticlesRadius * output.uv, 0.0f));
                return output;
            }

            struct Targets
            {
                float depth : SV_Depth;
                #ifdef _OCCLUSIONCULLDEBUG
                    float4 gridIndexDebug : SV_Target0;
                #endif
            };
            Targets SpriteGenerateDepthPassFrag(Varyings input)
            {
                float3 normalVS;
                normalVS.xy = input.uv;
                float xy_PlaneProj = dot(normalVS.xy, normalVS.xy);
                if (xy_PlaneProj > 1.0f) discard;
                normalVS.z = sqrt(1.0f - xy_PlaneProj);

                float3 positionVS = input.sphereCenterVS.xyz + normalVS * _ParticlesRadius;
                if (-positionVS.z <= unity_CameraWorldClipPlanes[4]) positionVS.z = -unity_CameraWorldClipPlanes[4];
                float4 positionCS = TransformWViewToHClip(positionVS);
                float fluidDepth = positionCS.z / positionCS.w;
                float sceneDepth = _SceneDepth.Sample(_point_clamp_sampler, GetUVFromCS(positionCS)).x;
                if (fluidDepth < sceneDepth) discard;

                Targets output;
                output.depth = fluidDepth;
                #ifdef _OCCLUSIONCULLDEBUG
                    float3 cellIndex3D = input.cellOfParticleIndex3D;
                    //if (cellIndex3D.z == 0) output.gridIndexDebug = float4(1, 0, 0, 1.0);
                    //else output.gridIndexDebug = float4(0, 1, 0, 1.0);
                    output.gridIndexDebug = float4(cellIndex3D, 1.0);
                #endif
                return output;
            }

            ENDHLSL
        }
    }
}