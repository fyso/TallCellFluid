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

         Pass
        {
            Tags
            {
                "LightMode" = "DrawEllipsoids"
            }

            Blend Off
            ZTest On
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #pragma target 5.0
            #pragma vertex GenerateDepthPassVertex
            #pragma fragment SpriteGenerateDepthPassFrag
            #include "../ShaderLibrary/Common.hlsl"

            struct Anisotropy
            {
                float4 AniX;
                float4 AniY;
                float4 AniZ;
            };
            StructuredBuffer<Anisotropy> _AnisotropyBuffer;
            StructuredBuffer<float3> _ParticlePositionBuffer;
            Texture2D _SceneDepth;
            SamplerState _point_clamp_sampler;
            float _ParticlesRadius;

            float Sign(float x)
            {
                return x < 0.0 ? -1.0 : 1.0;
            }

            bool solveQuadratic(float a, float b, float c, out float minT, out float maxT)
            {
                if (a == 0.0 && b == 0.0)
                {
                    minT = maxT = 0.0;
                    return false;
                }

                float discriminant = b * b - 4.0 * a * c;

                if (discriminant < 0.0)
                {
                    return false;
                }

                float t = -0.5 * (b + Sign(b) * sqrt(discriminant));
                minT = t / a;
                maxT = c / t;

                if (minT > maxT)
                {
                    float tmp = minT;
                    minT = maxT;
                    maxT = tmp;
                }

                return true;
            }

            float DotInvW(float4 a, float4 b)
            {
                return a.x * b.x + a.y * b.y + a.z * b.z - a.w * b.w;
            }

            float sqr(float x)
            {
                return x * x;
            }

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                nointerpolation float4 invQ0 : TEXCOORD1;
                nointerpolation float4 invQ1 : TEXCOORD2;
                nointerpolation float4 invQ2 : TEXCOORD3;
                nointerpolation float4 invQ3 : TEXCOORD4;
            };
            Varyings GenerateDepthPassVertex(uint vertexID : SV_VertexID, uint instanceID : SV_InstanceID)
            {
                Varyings output;
                Anisotropy particleAnisotropy = _AnisotropyBuffer[instanceID];
                float3 particlePosition = _ParticlePositionBuffer[instanceID];

                float4x4 q;
                q._m00_m10_m20_m30 = float4(particleAnisotropy.AniX.xyz * particleAnisotropy.AniX.w, 0.0);
                q._m01_m11_m21_m31 = float4(particleAnisotropy.AniY.xyz * particleAnisotropy.AniY.w, 0.0);
                q._m02_m12_m22_m32 = float4(particleAnisotropy.AniZ.xyz * particleAnisotropy.AniZ.w, 0.0);
                q._m03_m13_m23_m33 = float4(particlePosition, 1.0);

                // transforms a normal to parameter space (inverse transpose of (q*modelview)^-T)
                float4x4 invClip = mul(UNITY_MATRIX_VP, q);

                // solve for the right hand bounds in homogenous clip space
                float a1 = DotInvW(invClip[3], invClip[3]);
                float b1 = -2.0f * DotInvW(invClip[0], invClip[3]);
                float c1 = DotInvW(invClip[0], invClip[0]);

                float xmin;
                float xmax;
                solveQuadratic(a1, b1, c1, xmin, xmax);

                // solve for the right hand bounds in homogenous clip space
                float a2 = DotInvW(invClip[3], invClip[3]);
                float b2 = -2.0f * DotInvW(invClip[1], invClip[3]);
                float c2 = DotInvW(invClip[1], invClip[1]);

                float ymin;
                float ymax;
                solveQuadratic(a2, b2, c2, ymin, ymax);

                // construct inverse quadric matrix (used for ray-casting in parameter space)
                float4x4 invq;
                invq._m00_m10_m20_m30 = float4(particleAnisotropy.AniX.xyz / particleAnisotropy.AniX.w, 0.0);
                invq._m01_m11_m21_m31 = float4(particleAnisotropy.AniY.xyz / particleAnisotropy.AniY.w, 0.0);
                invq._m02_m12_m22_m32 = float4(particleAnisotropy.AniZ.xyz / particleAnisotropy.AniZ.w, 0.0);
                invq._m03_m13_m23_m33 = float4(0.0, 0.0, 0.0, 1.0);

                invq = transpose(invq);
                invq._m03_m13_m23_m33 = -(mul(invq, float4(particlePosition, 1)));

                // transform a point from view space to parameter space
                invq = mul(invq, unity_MatrixIV);

                // pass down
                output.invQ0 = invq._m00_m10_m20_m30;
                output.invQ1 = invq._m01_m11_m21_m31;
                output.invQ2 = invq._m02_m12_m22_m32;
                output.invQ3 = invq._m03_m13_m23_m33;

                //switch (vertexID)
                //{
                //case 0:
                //    output.positionCS = float4(xmin, ymin, 0.5, 1.0);
                //    break;

                //case 1:
                //    output.positionCS = float4(xmin, ymax, 0.5, 1.0);
                //    break;

                //case 2:
                //    output.positionCS = float4(xmax, ymax, 0.5, 1.0);
                //    break;

                //case 3:
                //    output.positionCS = float4(xmax, ymin, 0.5, 1.0);
                //    break;
                //}

                switch (vertexID % 6)
                {
                case 0:
                    output.positionCS = float4(xmin, ymin, 0.5, 1.0);
                    break;

                case 1:
                    output.positionCS = float4(xmin, ymax, 0.5, 1.0);
                    break;

                case 2:
                    output.positionCS = float4(xmax, ymin, 0.5, 1.0);
                    break;

                case 3:
                    output.positionCS = float4(xmax, ymin, 0.5, 1.0);
                    break;

                case 4:
                    output.positionCS = float4(xmin, ymax, 0.5, 1.0);
                    break;

                case 5:
                    output.positionCS = float4(xmax, ymax, 0.5, 1.0);
                    break;
                }
                

                return output;
            }

            float SpriteGenerateDepthPassFrag(Varyings input) : SV_Depth
            {
                // transform from view space to parameter space
                //column_major
                float4x4 invQuadric;
                invQuadric._m00_m10_m20_m30 = input.invQ0;
                invQuadric._m01_m11_m21_m31 = input.invQ1;
                invQuadric._m02_m12_m22_m32 = input.invQ2;
                invQuadric._m03_m13_m23_m33 = input.invQ3;

                float4 ndcPos = float4(input.positionCS.x * (1.0 / _ScreenParams.x) * 2.0f - 1.0f, input.positionCS.y * (1.0 / _ScreenParams.y) * 2.0 - 1.0, 0.0f, 1.0);
                float4 viewDir = mul(unity_MatrixIP, ndcPos);

                // ray to parameter space
                float4 dir = mul(invQuadric, float4(viewDir.xyz, 0.0));
                float4 origin = invQuadric._m03_m13_m23_m33;

                // set up quadratric equation
                float a = sqr(dir.x) + sqr(dir.y) + sqr(dir.z);
                float b = dir.x * origin.x + dir.y * origin.y + dir.z * origin.z - dir.w * origin.w;
                float c = sqr(origin.x) + sqr(origin.y) + sqr(origin.z) - sqr(origin.w);

                float minT;
                float maxT;

                if (!solveQuadratic(a, 2.0 * b, c, minT, maxT))
                {
                    discard;
                }
                float3 eyePos = viewDir.xyz * minT;

                ndcPos = TransformWViewToHClip(float4(eyePos, 1.0));
                ndcPos.z /= ndcPos.w;

                float sceneDepth = _SceneDepth.Sample(_point_clamp_sampler, GetUVFromCS(ndcPos)).x;
                if (ndcPos.z < sceneDepth) discard;

                return ndcPos.z;
            }

            ENDHLSL
        }
    }
}