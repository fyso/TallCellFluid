using DParticle;
using GPUDPP;
using UnityEngine;

public class SimulatorGPUCache
{
    public ComputeBuffer HashCount { get { return m_HashCount; } }
    public ComputeBuffer HashOffset { get { return m_HashOffset; } }
    public ComputeBuffer CellIndexCache { get { return m_CellIndexCache; } }
    public ComputeBuffer InnerSortIndexCache { get { return m_InnerSortIndexCache; } }
    public Particle ParticleCache { get { return m_ParticleCache; } set { m_ParticleCache = value; } }
    public GPUScanHillis GPUScan { get { return m_GPUScan; } }
    public GPUScanHillisPlan GPUScanHillisCache { get { return m_GPUScanHillisCache; } }
    public RenderTexture WaterSurfaceMinInterlockedCahce { get { return m_WaterSurfaceMinInterlockedCahce; } }
    public RenderTexture WaterSurfaceMaxInterlockedCahce { get { return m_WaterSurfaceMaxInterlockedCahce; } }

    public SimulatorGPUCache(int vMaxParticleCount, Vector2Int vResolutionXZ)
    {
        m_GPUScan = new GPUScanHillis();
        m_GPUScanHillisCache = new GPUScanHillisPlan();

        m_ParticleCache = new Particle(vMaxParticleCount);

        m_HashCount = new ComputeBuffer(vMaxParticleCount * 2, sizeof(uint));
        m_HashOffset = new ComputeBuffer(vMaxParticleCount * 2, sizeof(uint));

        m_CellIndexCache = new ComputeBuffer(vMaxParticleCount, sizeof(uint));
        m_InnerSortIndexCache = new ComputeBuffer(vMaxParticleCount, sizeof(uint));

        m_WaterSurfaceMinInterlockedCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };

        m_WaterSurfaceMaxInterlockedCahce = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RInt)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Bilinear,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    ~SimulatorGPUCache()
    {
        m_HashCount.Release();
        m_HashOffset.Release();
        m_CellIndexCache.Release();
        m_InnerSortIndexCache.Release();
    }

    private GPUScanHillis m_GPUScan;
    private GPUScanHillisPlan m_GPUScanHillisCache;
    private ComputeBuffer m_HashCount;
    private ComputeBuffer m_HashOffset;
    private ComputeBuffer m_CellIndexCache;
    private ComputeBuffer m_InnerSortIndexCache;
    private Particle m_ParticleCache;

    private RenderTexture m_WaterSurfaceMinInterlockedCahce;
    private RenderTexture m_WaterSurfaceMaxInterlockedCahce;
}