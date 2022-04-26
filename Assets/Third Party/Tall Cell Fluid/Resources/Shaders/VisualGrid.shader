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

            static float vertices[108] = {
                0.0f, 0.0f, 0.0f, // 0
                1.0f, 0.0f, 0.0f, // 1
                0.0f, 1.0f, 0.0f, // 3

                0.0f, 1.0f, 0.0f, // 3
                1.0f, 0.0f, 0.0f, // 1
                1.0f, 1.0f, 0.0f, // 2

                1.0f, 0.0f, 0.0f, // 1
                1.0f, 0.0f, -1.0f, // 5
                1.0f, 1.0f, 0.0f, // 2

                1.0f, 1.0f, 0.0f, // 2
                1.0f, 0.0f, -1.0f, // 5
                1.0f, 1.0f, -1.0f, // 6
        
                1.0f, 0.0f, -1.0f, // 5
                0.0f, 0.0f, -1.0f, // 4
                1.0f, 1.0f, -1.0f, // 6
        
                1.0f, 1.0f, -1.0f, // 6
                0.0f, 0.0f, -1.0f, // 4
                0.0f, 1.0f, -1.0f, // 7

                0.0f, 0.0f, -1.0f, // 4
                0.0f, 0.0f, 0.0f, // 0
                0.0f, 1.0f, -1.0f, // 7

                0.0f, 1.0f, -1.0f, // 7
                0.0f, 0.0f, 0.0f, // 0
                0.0f, 1.0f, 0.0f, // 3

                0.0f, 1.0f, 0.0f, // 3
                1.0f, 1.0f, 0.0f, // 2
                0.0f, 1.0f, -1.0f, // 7

                0.0f, 1.0f, -1.0f, // 7
                1.0f, 1.0f, 0.0f, // 2
                1.0f, 1.0f, -1.0f, // 6

                0.0f, 0.0f, 0.0f, // 0
                0.0f, 0.0f, -1.0f, // 4
                1.0f, 0.0f, 0.0f, // 1

                1.0f, 0.0f, 0.0f, // 1
                0.0f, 0.0f, -1.0f, // 4
                1.0f, 0.0f, -1.0f // 5
            };
            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            v2f VisualGridVert(uint vertexID : SV_VertexID, uint instanceID: SV_InstanceID)
            {
                float4 vertex = float4(vertices[vertexID * 3], vertices[vertexID * 3 + 1], vertices[vertexID * 3 + 2], 1);
                v2f o;
                o.vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, vertex));
                
                return o;
            }

            float4 VisualGridFrag(v2f i) : SV_Target
            {
                return float4(1,1,1,1);
            }

            ENDCG
        }
    }
}