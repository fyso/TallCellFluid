using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TallCellGridGPUCache
{
    public TallCellGridGPUCache(Vector2Int vResolutionXZ, float vCellLength, int vConstantCellNum)
    {
        H1H2Cahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        MaxMinCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RGFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        BackTallCellHeightCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellParticleCountCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellPow2HeightSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellHeightSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellHeightVelocityXSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellHeightVelocityYSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellHeightVelocityZSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVelocityXSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVelocityYSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVelocityZSumCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellWeightTempCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, vConstantCellNum, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVelocityXTempCache = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, vConstantCellNum, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVelocityYTempCache = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, vConstantCellNum, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVelocityZTempCache = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, vConstantCellNum, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~TallCellGridGPUCache()
    {
        H1H2Cahce.Release();
        MaxMinCahce.Release();
        BackTallCellHeightCahce.Release();

        TallCellParticleCountCahce.Release();
        TallCellPow2HeightSumCahce.Release();
        TallCellHeightSumCahce.Release();
        TallCellHeightVelocityXSumCahce.Release();
        TallCellHeightVelocityYSumCahce.Release();
        TallCellHeightVelocityZSumCahce.Release();
        TallCellVelocityXSumCahce.Release();
        TallCellVelocityYSumCahce.Release();
        TallCellVelocityZSumCahce.Release();

        RegularCellWeightTempCahce.Release();
        RegularCellVelocityXTempCache.Release();
        RegularCellVelocityYTempCache.Release();
        RegularCellVelocityZTempCache.Release();
    }

    //Temp cache
    public RenderTexture H1H2Cahce;
    public RenderTexture MaxMinCahce;
    public RenderTexture BackTallCellHeightCahce;

    public RenderTexture TallCellParticleCountCahce;
    public RenderTexture TallCellPow2HeightSumCahce;
    public RenderTexture TallCellHeightSumCahce;
    public RenderTexture TallCellHeightVelocityXSumCahce;
    public RenderTexture TallCellHeightVelocityYSumCahce;
    public RenderTexture TallCellHeightVelocityZSumCahce;
    public RenderTexture TallCellVelocityXSumCahce;
    public RenderTexture TallCellVelocityYSumCahce;
    public RenderTexture TallCellVelocityZSumCahce;

    public RenderTexture RegularCellWeightTempCahce;
    public RenderTexture RegularCellVelocityXTempCache;
    public RenderTexture RegularCellVelocityYTempCache;
    public RenderTexture RegularCellVelocityZTempCache;
}
