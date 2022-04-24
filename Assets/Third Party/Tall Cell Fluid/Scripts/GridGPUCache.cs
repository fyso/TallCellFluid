using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GridGPUCache
{
    public GridGPUCache(Vector2Int vResolutionXZ, float vCellLength, int vRegularCellYCount)
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

        TallCellScalarCahce2 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellScalarCahce1 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorXCahce2 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorYCahce2 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorZCahce2 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorXCahce1 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorYCahce1 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorZCahce1 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellScalarCahce = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVectorXCache = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVelocityYCache = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVelocityZCache = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~GridGPUCache()
    {
        H1H2Cahce.Release();
        MaxMinCahce.Release();
        BackTallCellHeightCahce.Release();

        TallCellParticleCountCahce.Release();
        TallCellScalarCahce2.Release();
        TallCellScalarCahce1.Release();
        TallCellVectorXCahce2.Release();
        TallCellVectorYCahce2.Release();
        TallCellVectorZCahce2.Release();
        TallCellVectorXCahce1.Release();
        TallCellVectorYCahce1.Release();
        TallCellVectorZCahce1.Release();

        RegularCellScalarCahce.Release();
        RegularCellVectorXCache.Release();
        RegularCellVelocityYCache.Release();
        RegularCellVelocityZCache.Release();
    }

    //Temp cache
    public RenderTexture H1H2Cahce;
    public RenderTexture MaxMinCahce;
    public RenderTexture BackTallCellHeightCahce;

    public RenderTexture TallCellParticleCountCahce;
    public RenderTexture TallCellScalarCahce2;
    public RenderTexture TallCellScalarCahce1;
    public RenderTexture TallCellVectorXCahce2;
    public RenderTexture TallCellVectorYCahce2;
    public RenderTexture TallCellVectorZCahce2;
    public RenderTexture TallCellVectorXCahce1;
    public RenderTexture TallCellVectorYCahce1;
    public RenderTexture TallCellVectorZCahce1;

    public RenderTexture RegularCellScalarCahce;
    public RenderTexture RegularCellVectorXCache;
    public RenderTexture RegularCellVelocityYCache;
    public RenderTexture RegularCellVelocityZCache;
}
