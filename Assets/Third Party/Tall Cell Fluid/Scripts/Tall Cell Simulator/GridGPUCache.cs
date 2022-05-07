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

        TallCellScalarCahce1 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = 2,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellScalarCahce2 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = 2,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorCahce1 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = 6,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        TallCellVectorCahce2 = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = 6,
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

        RegularCellVectorYCache = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        RegularCellVectorZCache = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        VectorBForFineLevel = new RenderTexture(vResolutionXZ.x, vRegularCellYCount + 2, 0, RenderTextureFormat.RFloat)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Trilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        LastFrameTallCellHeightCache = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
        LastFrameVelocityCache = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat);
        SmoothPressureCache = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
    }

    ~GridGPUCache()
    {
        H1H2Cahce.Release();
        MaxMinCahce.Release();
        BackTallCellHeightCahce.Release();

        TallCellParticleCountCahce.Release();
        TallCellScalarCahce2.Release();
        TallCellScalarCahce1.Release();
        TallCellVectorCahce2.Release();
        TallCellVectorCahce1.Release();

        RegularCellScalarCahce.Release();
        RegularCellVectorXCache.Release();
        RegularCellVectorYCache.Release();
        RegularCellVectorZCache.Release();

        LastFrameTallCellHeightCache.Release();
        VectorBForFineLevel.Release();
    }

    //Temp cache
    public RenderTexture H1H2Cahce;
    public RenderTexture MaxMinCahce;
    public RenderTexture BackTallCellHeightCahce;

    public RenderTexture TallCellParticleCountCahce;
    public RenderTexture TallCellScalarCahce2;
    public RenderTexture TallCellScalarCahce1;
    public RenderTexture TallCellVectorCahce1;
    public RenderTexture TallCellVectorCahce2;

    public RenderTexture RegularCellScalarCahce;
    public RenderTexture RegularCellVectorXCache;
    public RenderTexture RegularCellVectorYCache;
    public RenderTexture RegularCellVectorZCache;

    public RenderTexture LastFrameTallCellHeightCache;
    public GridValuePerLevel LastFrameVelocityCache;
    public GridValuePerLevel SmoothPressureCache;

    public RenderTexture VectorBForFineLevel;
}
