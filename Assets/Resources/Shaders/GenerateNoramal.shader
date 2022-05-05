Shader "Custom/GenerateNoramal"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        [Toggle(_RECONSTRUCT_HIGH_QUALITY_NORMAL)] _ReconstructHighQulityNormalToggle("Reconstruct High-Qulity Normal", Float) = 1
    }

    SubShader
    {
        Pass
        {
            Tags
            {
                "LightMode" = "GenerateNoramal"
            }
            Blend Off
            ZWrite Off
            ZTest Off
            Cull Off

            HLSLPROGRAM
            #include "../ShaderLibrary/Common.hlsl"
            #include "../ShaderLibrary/FullScreenPassVS.hlsl"
            #pragma fragment CalculateNormalPassFrag
            #pragma shader_feature _RECONSTRUCT_HIGH_QUALITY_NORMAL

            Texture2D _SmoothFluidDepthRT;
            SamplerState _point_clamp_sampler;

            float4 CalculateNormalPassFrag(Varyings input) : SV_TARGET
            {
                float2 texelSize = 1. / _ScreenParams.xy;

                float2 texCoord = input.uv;
                #if _RECONSTRUCT_HIGH_QUALITY_NORMAL
                    float2 texCoordRight = texCoord + float2(texelSize.x, 0);
                    float2 texCoordLeft = texCoord + float2(-texelSize.x, 0);
                    float2 texCoordUp = texCoord + float2(0, texelSize.y);
                    float2 texCoordDown = texCoord + float2(0, -texelSize.y);
                #endif

                float depth = _SmoothFluidDepthRT.Sample(_point_clamp_sampler, texCoord).x;
                float3 positionVS = GetPositionVSFromDepth(texCoord, depth, true);

                #if _RECONSTRUCT_HIGH_QUALITY_NORMAL
                    float3 positionRightVS = GetPositionVSFromDepthTex(_SmoothFluidDepthRT, _point_clamp_sampler, texCoordRight);
                    float3 positionLeftVS = GetPositionVSFromDepthTex(_SmoothFluidDepthRT, _point_clamp_sampler, texCoordLeft);
                    float3 positionUpVS = GetPositionVSFromDepthTex(_SmoothFluidDepthRT, _point_clamp_sampler, texCoordUp);
                    float3 positionDownVS = GetPositionVSFromDepthTex(_SmoothFluidDepthRT, _point_clamp_sampler, texCoordDown);

                    float3 ddx = positionRightVS - positionVS;
                    float3 ddx2 = positionVS - positionLeftVS;
                    if (abs(ddx.z) > abs(ddx2.z))
                    {
                        ddx = ddx2;
                    }

                    float3 ddy = positionUpVS - positionVS;
                    float3 ddy2 = positionVS - positionDownVS;
                    if (abs(ddy.z) > abs(ddy2.z))
                    {
                        ddy = ddy2;
                    }
                    float3 normalVS = normalize(cross(ddx, ddy));
                    float3 normalWS = mul(unity_MatrixIV, float4(normalVS, 0));
                #else
                    float3 normalVS = normalize(cross(ddx(positionVS), ddy(positionVS)));
                    float3 normalWS = mul(unity_MatrixIV, float4(normalVS, 0));
                #endif

                normalWS = normalize(normalWS);
                return float4(normalWS, depth);
            }

            ENDHLSL
        }

    }
}