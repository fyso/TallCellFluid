#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
#pragma multi_compile _ _DEPTHSPLIT_NONLINEAR
#pragma enable_d3d11_debug_symbols

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

uint _PerspectiveGridDimX;
uint _PerspectiveGridDimY;
uint _PerspectiveGridDimZ;
uint _PerspectiveGridLengthX;
uint _PerspectiveGridLengthY;
float _SampleRadioInv;
float _LeftPlane;
float _DownPlane;
float _NearPlane;
float _FarPlane;

int3 viewPos2Index3D(float3 viewPos)
{
    float parm1 = 0.5f * glstate_matrix_projection[1][1] / viewPos.z;
    float aspectInv = _ScreenParams.y / _ScreenParams.x;
    float ndcX = viewPos.x * parm1 * aspectInv + 0.5;
    int index_X = 0;
    if (ndcX < 0 || ndcX > 1)
        index_X = -1;
    else
    {
        ndcX = (viewPos.x - _LeftPlane) / _PerspectiveGridLengthX;
        index_X = floor(ndcX * _PerspectiveGridDimX);
    }
    
    float ndcY = viewPos.y * parm1 + 0.5;
    int index_Y = 0;
    if (ndcY < 0 || ndcY > 1)
        index_Y = -1;
    else
    {
        ndcY = (viewPos.y - _DownPlane) / _PerspectiveGridLengthY;
        index_Y = floor(ndcY * _PerspectiveGridDimY);
    }
    
    #ifdef _DEPTHSPLIT_NONLINEAR
            int index_Z = floor(log(-viewPos.z / _NearPlane) * _SampleRadioInv);
    #else
        int index_Z = floor((-viewPos.z - _NearPlane) / _SampleRadioInv * _PerspectiveGridDimZ);
    #endif

    return int3(index_X, index_Y, index_Z);
}


uint tex3DIndex2Liner(uint3 tex3DIndex)
{
    return (tex3DIndex.z * _PerspectiveGridDimY + tex3DIndex.y) * _PerspectiveGridDimX + tex3DIndex.x;
}

//------———————-----深度转换为位置————————————————
float3 GetPositionVSFromDepth(float2 texCoord, float depth, bool isDiscard = false)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif
    if (isDiscard && depth >= 1.0)
    {
        discard;
        return 0;
    }

    float4 positionNDC = float4(float3(texCoord, depth) * 2.0 - 1.0, 1.0);
    float4 positionVS = mul(unity_MatrixIP, positionNDC);
    return positionVS.xyz / positionVS.w;
}

float3 GetPositionVSFromDepthTex(Texture2D depthTex, SamplerState sampler_MainTex, float2 texCoord, bool isDiscard = false)
{
    float depth = depthTex.Sample(sampler_MainTex, texCoord).x;
    return GetPositionVSFromDepth(texCoord, depth, isDiscard);
}

float3 GetPositionWSFromDepth(float2 texCoord, float depth, bool isDiscard)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif
    if (isDiscard && depth >= 1.0)
    {
        discard;
        return 0;
    }

    float4 positionNDC = float4(float3(texCoord, depth) * 2.0 - 1.0, 1.0);
    float4 positionWS = mul(unity_MatrixIVP, positionNDC);
    return positionWS.xyz / positionWS.w;
}

float3 GetPositionWSFromDepth(float2 texCoord, float depth)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif

    float4 positionNDC = float4(float3(texCoord, depth) * 2.0 - 1.0, 1.0);
    float4 positionWS = mul(unity_MatrixIVP, positionNDC);
    return positionWS.xyz / positionWS.w;
}

float3 GetPositionWSFromDepthTex(Texture2D depthTex, SamplerState sampler_MainTex, float2 texCoord, bool isDiscard = false)
{
    float depth = depthTex.Sample(sampler_MainTex, texCoord).x;
    return GetPositionWSFromDepth(texCoord, depth, isDiscard);
}

//------———————-----获取各种空间的深度————————————————
float GetDepthVS(float depth)
{
#if UNITY_REVERSED_Z
    depth = 1 - depth;
#endif

    depth = depth * 2.0f - 1.0f;
    return unity_MatrixIP[2][3] / (unity_MatrixIP[3][2] * depth + unity_MatrixIP[3][3]);
}

float GetDepthVSFromDepthTex(Texture2D depthTex, SamplerState sampler_MainTex, float2 texCoord)
{
    float depth = depthTex.SampleLevel(sampler_MainTex, texCoord, 0).x;
    return GetDepthVS(depth);
}

float GetDepthCSFromVS(float depthVS)
{
    return (UNITY_MATRIX_P[2][2] * depthVS + UNITY_MATRIX_P[2][3]) / (UNITY_MATRIX_P[3][2] * depthVS);
}

float GetDepthCSFromDepthVS(float DepthVS)
{
    return (glstate_matrix_projection[2][2] * DepthVS + glstate_matrix_projection[2][3])
            / (glstate_matrix_projection[3][2] * DepthVS);
}





//------———————-----工具函数————————————————
float2 GetUVFromCS(float4 positionCS)
{
    float2 uv = 0.5f * positionCS.xy / positionCS.w + 0.5f;
    if (_ProjectionParams.x < 0.0) uv.y = 1.0 - uv.y;
    return uv;
}
#endif
