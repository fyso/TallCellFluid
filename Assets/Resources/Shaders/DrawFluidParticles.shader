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
            #include "../ShaderLibrary/Common.hlsl"

            StructuredBuffer<float3> _ParticlePositionBuffer;
            Texture2D _SceneDepth;
            SamplerState _point_clamp_sampler;
            float _ParticlesRadius;

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : VAR_SCREEN_UV;
                nointerpolation float3 sphereCenterVS : VAR_POSITION_VS;
            };
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
                output.positionCS = TransformWViewToHClip(output.sphereCenterVS + float3(_ParticlesRadius * output.uv, 0.0f));

                return output;
            }

            float SpriteGenerateDepthPassFrag(Varyings input) : SV_Depth
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

                return fluidDepth;
            }

            ENDHLSL
        }
    }
}