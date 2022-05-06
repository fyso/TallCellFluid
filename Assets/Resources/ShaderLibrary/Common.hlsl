﻿#ifndef CUSTOM_COMMON_INCLUDED
#define CUSTOM_COMMON_INCLUDED
#pragma enable_d3d11_debug_symbols

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/CommonMaterial.hlsl"
#include "UnityInput.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/UnityInstancing.hlsl"
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/SpaceTransforms.hlsl"

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
