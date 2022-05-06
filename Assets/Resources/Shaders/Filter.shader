Shader "Custom/Filter"
{
    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "NarrowRangeFilter2D"
            }
            Blend Off
            ZTest Off
            ZWrite On
            Cull Off

            HLSLPROGRAM
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/FullScreenPassVS.hlsl"
            #pragma fragment NarrowRangeFilterPassFrag
            #define MAX_FILTERSIZE_2D 32

            Texture2D _FluidDepthRT;
            SamplerState _point_clamp_sampler;
            float _ParticlesRadius;
            float _FilterRadiusWS;
            float _ClampRatio;
            float _ThresholdRatio;

            float NarrowRangeFilterPassFrag(Varyings input) : SV_Depth
            {
                if (_FilterRadiusWS == 0) return _FluidDepthRT.Sample(_point_clamp_sampler, input.uv).x;

                float3 positionVS = GetPositionVSFromDepthTex(_FluidDepthRT, _point_clamp_sampler, input.uv, true);

                float standardDeviation = _ScreenParams.y * _FilterRadiusWS * 0.5f / positionVS.z * glstate_matrix_projection[1][1];
                int filterSize = min(MAX_FILTERSIZE_2D, standardDeviation * 3);
                //int filterSize = 20;

                float lower_clamp = positionVS.z - _ParticlesRadius * _ClampRatio;

                float threshold = _ParticlesRadius * _ThresholdRatio;
                float upper = positionVS.z + threshold;
                float lower = positionVS.z - threshold;

                float2 texelSize = 1.0f / _ScreenParams.xy;
                float sumDepthValue = 0;
                float sumWeight = 0;
                for (int i = -filterSize; i <= filterSize; ++i)
                {
                    for (int k = -filterSize; k <= filterSize; ++k)
                    {
                        float2 texCoordOffset = float2(i, k) * texelSize;
                        float sampleDepth = GetDepthVSFromDepthTex(_FluidDepthRT, _point_clamp_sampler, input.uv + texCoordOffset);

                        float weight = 1;
                        if (standardDeviation != 0)
                        {
                            float2 distance = float2(i, k) / standardDeviation;
                            weight = exp(-0.5 * dot(distance, distance));
                        }

                        if (sampleDepth > upper)
                        {
                            weight = 0;
                        }
                        else if (sampleDepth < lower)
                        {
                            sampleDepth = lower_clamp;
                        }
                        else
                        {
                            upper = max(upper, sampleDepth + threshold);
                            lower = min(lower, sampleDepth - threshold);
                        }

                        sumDepthValue += (sampleDepth * weight);
                        sumWeight += weight;
                    }
                }
                positionVS.z = sumDepthValue / sumWeight;

                return GetDepthCSFromDepthVS(positionVS.z);
            }
            ENDHLSL
        }
    }
}