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

    public SimulatorGPUCache(int vMaxParticleCount)
    {
        m_GPUScan = new GPUScanHillis();
        m_GPUScanHillisCache = new GPUScanHillisPlan();

        m_ParticleCache = new Particle(vMaxParticleCount);

        m_HashCount = new ComputeBuffer(vMaxParticleCount * 2, sizeof(uint));
        m_HashOffset = new ComputeBuffer(vMaxParticleCount * 2, sizeof(uint));

        m_CellIndexCache = new ComputeBuffer(vMaxParticleCount, sizeof(uint));
        m_InnerSortIndexCache = new ComputeBuffer(vMaxParticleCount, sizeof(uint));
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
}