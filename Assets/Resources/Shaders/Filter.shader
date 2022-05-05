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

            Texture2D _FluidDepthRT;
            SamplerState _point_clamp_sampler;
            float _ParticlesRadius;
            float _FilterRadius;
            float _ClampRatio;
            float _ThresholdRatio;
            float _Sigma;

            float NarrowRangeFilterPassFrag(Varyings input) : SV_Depth
            {
                if (_FilterRadius == 0) return _FluidDepthRT.Sample(_point_clamp_sampler, input.uv).x;

                float3 positionVS = GetPositionVSFromDepthTex(_FluidDepthRT, _point_clamp_sampler, input.uv, true);

                float2 texelSize = 1.0f / _ScreenParams.xy;

                float standardDeviation = _ParticlesRadius * _FilterRadius;
                float meanU = _ParticlesRadius * _ClampRatio;
                float threshold = _ParticlesRadius * _ThresholdRatio;

                float radio = 0.5f * _ScreenParams.y * abs(glstate_matrix_projection[1][1]);
                float K = radio * standardDeviation;
                int standardDeviation1 = int(ceil(K / (abs(positionVS.z) + 1)));
                int filterSizeCS = min(MAX_FILTERSIZE_2D, standardDeviation1);
                float sigma = float(filterSizeCS) * _Sigma;

                float upper = positionVS.z + threshold;
                float lower = positionVS.z - threshold;
                float lower_clamp = positionVS.z - meanU;

                float sumDepthValue = 0;
                float sumWeight = 0;

                for (int i = -filterSizeCS; i <= filterSizeCS; ++i)
                {
                    for (int k = -filterSizeCS; k <= filterSizeCS; ++k)
                    {
                        float2 texCoordOffset = float2(i, k) * texelSize;
                        float sampleDepth = GetDepthVSFromDepthTex(_FluidDepthRT, _point_clamp_sampler, input.uv + texCoordOffset);
                        float gaussian_Value = CalculateGaussianDistributionWeight2D(float2(i, k), sigma);
                        float weight = gaussian_Value;

                        ModifiedGaussianFilter(sampleDepth, weight, upper, lower, lower_clamp, threshold);
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