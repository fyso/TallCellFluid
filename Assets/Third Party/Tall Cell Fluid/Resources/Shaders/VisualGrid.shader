Shader "Custom/VisualGrid"
{
    Properties
    {
        _Color("Grid Color", Color) = (.25, .5, .5, 1)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" }

        Pass
        {
            CGPROGRAM
            #pragma vertex VisualGridVert
            #pragma fragment VisualGridFrag
            #pragma enable_d3d11_debug_symbols 
            #include "UnityCG.cginc"

            float3 MinPos;
            float CellLength;
            int ResolutionX;
            Texture2D<float> TerrainHeight;

            static float vertices[108] = {
                -0.5f, -0.5f, 0.5f, // 0
                0.5f, -0.5f, 0.5f, // 1
                -0.5f, 0.5f, 0.5f, // 3

                -0.5f, 0.5f, 0.5f, // 3
                0.5f, -0.5f, 0.5f, // 1
                0.5f, 0.5f, 0.5f, // 2

                0.5f, -0.5f, 0.5f, // 1
                0.5f, -0.5f, -0.5f, // 5
                0.5f, 0.5f, 0.5f, // 2

                0.5f, 0.5f, 0.5f, // 2
                0.5f, -0.5f, -0.5f, // 5
                0.5f, 0.5f, -0.5f, // 6
        
                0.5f, -0.5f, -0.5f, // 5
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, 0.5f, -0.5f, // 6
        
                0.5f, 0.5f, -0.5f, // 6
                -0.5f, -0.5f, -0.5f, // 4
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, -0.5f, -0.5f, // 4
                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, 0.5f, -0.5f, // 7
                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, 0.5f, 0.5f, // 3

                -0.5f, 0.5f, 0.5f, // 3
                0.5f, 0.5f, 0.5f, // 2
                -0.5f, 0.5f, -0.5f, // 7

               -0.5f, 0.5f, -0.5f, // 7
                0.5f, 0.5f, 0.5f, // 2
                0.5f, 0.5f, -0.5f, // 6

               -0.5f, -0.5f, 0.5f, // 0
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, -0.5f, 0.5f, // 1

                0.5f, -0.5f, 0.5f, // 1
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, -0.5f, -0.5f // 5
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
            };

            v2f VisualGridVert(uint vertexID : SV_VertexID, uint instanceID: SV_InstanceID)
            {
                uint2 terrainCellIndex = uint2(instanceID % ResolutionX, instanceID / ResolutionX);
                float terrainHeight = TerrainHeight[terrainCellIndex];
                float3 pos = MinPos +
                    float3((vertices[vertexID * 3] + terrainCellIndex.x + 0.5) * CellLength,
                           vertices[vertexID * 3 + 1] * terrainHeight + terrainHeight * 0.5,
                           (vertices[vertexID * 3 + 2] + terrainCellIndex.y + 0.5) * CellLength);

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                return o;
            }

            float4 VisualGridFrag(v2f i) : SV_Target
            {
                return float4(0.462, 0.302, 0.223, 1);
            }

            ENDCG
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex VisualGridVert
            #pragma fragment VisualGridFrag
            #pragma enable_d3d11_debug_symbols 
            #include "UnityCG.cginc"

            float3 MinPos;
            float CellLength;
            int ResolutionX;
            int ResolutionY;
            Texture2D<float> TerrainHeight;
            Texture2D<float> TallCellHeight;

            int TallCellShowInfoMode; // -1 is no draw, 0 is to draw scalar, 1 is to draw direction, 2 is to draw size
            Texture2D<float3> TopVelocity;
            Texture2D<float3> BottomVelocity;
            Texture2D<float> ShowTopValue;
            Texture2D<float> ShowBottomValue;
            float4 MinShowColor;
            float4 MaxShowColor;

            static float vertices[108] = {
                -0.5f, -0.5f, 0.5f, // 0
                0.5f, -0.5f, 0.5f, // 1
                -0.5f, 0.5f, 0.5f, // 3

                -0.5f, 0.5f, 0.5f, // 3
                0.5f, -0.5f, 0.5f, // 1
                0.5f, 0.5f, 0.5f, // 2

                0.5f, -0.5f, 0.5f, // 1
                0.5f, -0.5f, -0.5f, // 5
                0.5f, 0.5f, 0.5f, // 2

                0.5f, 0.5f, 0.5f, // 2
                0.5f, -0.5f, -0.5f, // 5
                0.5f, 0.5f, -0.5f, // 6

                0.5f, -0.5f, -0.5f, // 5
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, 0.5f, -0.5f, // 6

                0.5f, 0.5f, -0.5f, // 6
                -0.5f, -0.5f, -0.5f, // 4
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, -0.5f, -0.5f, // 4
                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, 0.5f, -0.5f, // 7
                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, 0.5f, 0.5f, // 3

                -0.5f, 0.5f, 0.5f, // 3
                0.5f, 0.5f, 0.5f, // 2
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, 0.5f, -0.5f, // 7
                0.5f, 0.5f, 0.5f, // 2
                0.5f, 0.5f, -0.5f, // 6

                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, -0.5f, 0.5f, // 1

                0.5f, -0.5f, 0.5f, // 1
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, -0.5f, -0.5f // 5
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                float value : VALUE;
                float3 velocity : VELOCITY;
            };

            v2f VisualGridVert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                v2f o;
                uint2 tallCellIndex = uint2(instanceID % ResolutionX, instanceID / ResolutionX);
                if (TallCellShowInfoMode == 1)
                {
                    if(vertices[vertexID * 3 + 1] > 0) o.velocity = TopVelocity[tallCellIndex];
                    else o.velocity = BottomVelocity[tallCellIndex];
                }
                else if (TallCellShowInfoMode == 2)
                {
                    if (vertices[vertexID * 3 + 1] > 0) o.value = clamp(length(TopVelocity[tallCellIndex]), MinShowColor.w, MaxShowColor.w);
                    else o.value = clamp(length(BottomVelocity[tallCellIndex]), MinShowColor.w, MaxShowColor.w);
                }
                else if (TallCellShowInfoMode == 0)
                {
                    if (vertices[vertexID * 3 + 1] > 0) o.value = clamp(ShowTopValue[tallCellIndex], MinShowColor.w, MaxShowColor.w);
                    else  o.value = clamp(ShowBottomValue[tallCellIndex], MinShowColor.w, MaxShowColor.w);
                }

                float terrainHeight = TerrainHeight[tallCellIndex];
                float tallCellHeight = TallCellHeight[tallCellIndex];

                float3 pos = MinPos + 
                    float3((vertices[vertexID * 3] + tallCellIndex.x + 0.5) * CellLength,
                           vertices[vertexID * 3 + 1] * tallCellHeight + tallCellHeight * 0.5 + terrainHeight,
                           (vertices[vertexID * 3 + 2] + tallCellIndex.y + 0.5) * CellLength);

                o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                return o;
            }

            float4 VisualGridFrag(v2f i) : SV_Target
            {
                if (TallCellShowInfoMode == 1)
                {
                    return float4(i.velocity, 1);
                }
                else if (TallCellShowInfoMode == -1)
                {
                    return float4(0.262, 0.556, 0.858, 1);
                }
                else
                {
                    return float4(lerp(MinShowColor.xyz, MaxShowColor.xyz, i.value), 1);
                }
            }

            ENDCG
        }
            
        Pass
        {
            CGPROGRAM
            #pragma vertex VisualGridVert
            #pragma fragment VisualGridFrag
            #pragma enable_d3d11_debug_symbols 
            #include "UnityCG.cginc"

            float3 MinPos;
            float CellLength;
            int ResolutionX;
            int ResolutionY;
            Texture2D<float> TerrainHeight;
            Texture2D<float> TallCellHeight;

            int ShowInfoMode; // 0 is to draw scalar, 1 is to draw direction, 2 is to draw size
            Texture3D<float3> Velocity;
            Texture3D<float> ShowValue;
            float4 MinShowColor;
            float4 MaxShowColor;

            static float vertices[108] = {
                -0.5f, -0.5f, 0.5f, // 0
                0.5f, -0.5f, 0.5f, // 1
                -0.5f, 0.5f, 0.5f, // 3

                -0.5f, 0.5f, 0.5f, // 3
                0.5f, -0.5f, 0.5f, // 1
                0.5f, 0.5f, 0.5f, // 2

                0.5f, -0.5f, 0.5f, // 1
                0.5f, -0.5f, -0.5f, // 5
                0.5f, 0.5f, 0.5f, // 2

                0.5f, 0.5f, 0.5f, // 2
                0.5f, -0.5f, -0.5f, // 5
                0.5f, 0.5f, -0.5f, // 6

                0.5f, -0.5f, -0.5f, // 5
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, 0.5f, -0.5f, // 6

                0.5f, 0.5f, -0.5f, // 6
                -0.5f, -0.5f, -0.5f, // 4
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, -0.5f, -0.5f, // 4
                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, 0.5f, -0.5f, // 7
                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, 0.5f, 0.5f, // 3

                -0.5f, 0.5f, 0.5f, // 3
                0.5f, 0.5f, 0.5f, // 2
                -0.5f, 0.5f, -0.5f, // 7

                -0.5f, 0.5f, -0.5f, // 7
                0.5f, 0.5f, 0.5f, // 2
                0.5f, 0.5f, -0.5f, // 6

                -0.5f, -0.5f, 0.5f, // 0
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, -0.5f, 0.5f, // 1

                0.5f, -0.5f, 0.5f, // 1
                -0.5f, -0.5f, -0.5f, // 4
                0.5f, -0.5f, -0.5f // 5
            };
            struct v2f
            {
                float4 pos : SV_POSITION;
                nointerpolation float value : VALUE;
                nointerpolation float3 velocity : VELOCITY;
            };

            v2f VisualGridVert(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                uint3 regularCellIndex = uint3(instanceID % ResolutionX, instanceID / ResolutionX % ResolutionY, instanceID / (ResolutionX * ResolutionY));
                float terrainHeight = TerrainHeight[regularCellIndex.xz];
                float tallCellHeight = TallCellHeight[regularCellIndex.xz];
                float3 pos = MinPos +
                    (float3(vertices[vertexID * 3] + 0.5,
                            vertices[vertexID * 3 + 1],
                            vertices[vertexID * 3 + 2] + 0.5) + regularCellIndex) * CellLength;
                pos.y += 0.5 * CellLength + terrainHeight + tallCellHeight;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(pos, 1));
                if (ShowInfoMode == 1)
                {
                    o.velocity = Velocity[regularCellIndex];
                }
                else if(ShowInfoMode == 2)
                {
                    o.value = clamp(length(Velocity[regularCellIndex]), MinShowColor.w, MaxShowColor.w);
                }
                else
                {
                    o.value = clamp(ShowValue[regularCellIndex], MinShowColor.w, MaxShowColor.w);
                }
                return o;
            }

            float4 VisualGridFrag(v2f i) : SV_Target
            {
                if (ShowInfoMode == 1)
                {
                    return float4(i.velocity, 1);
                }
                else
                {
                    return float4(lerp(MinShowColor.xyz, MaxShowColor.xyz, i.value), 1);
                }
            }

            ENDCG
        }
    }
}