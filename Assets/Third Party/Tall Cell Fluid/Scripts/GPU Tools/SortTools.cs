using GPUDPP;
using UnityEngine;
using DParticle;

public class ParticleSortTools
{
    public ParticleSortTools()
    {
        m_GPUBufferClear = new GPUBufferClear();

        GPUCountingSortHashCS = Resources.Load<ComputeShader>("Shaders/GPUCountingSortHashTools");
        InsertParticleIntoHashGridKernel = GPUCountingSortHashCS.FindKernel("insertParticleIntoHashGrid");
        CountingSortFullKernel = GPUCountingSortHashCS.FindKernel("countingSortFull");
    }

    public void SortOnlyTallCellParticle(DynamicParticle voTarget, SimulatorGPUCache vCache, Vector3 vMin, float vCellLength)
    {
        m_GPUBufferClear.ClraeUIntBufferWithZero(vCache.HashCount);
        GPUCountingSortHashCS.EnableKeyword("HashTallCell");

        GPUCountingSortHashCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
        GPUCountingSortHashCS.SetFloat("HashGridCellLength", vCellLength);
        GPUCountingSortHashCS.SetInt("TargetParticleType", Simulator.OnlyTallCellParticleTypeIndex);

        GPUCountingSortHashCS.SetInt("ParticleCountArgumentOffset", DynamicParticle.ParticleCountArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleSplitPointArgumentOffset", DynamicParticle.DifferParticleSplitPointArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleCountArgumentOffset", DynamicParticle.DifferParticleCountArgumentOffset);

        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleIndirectArgment_R", voTarget.Argument);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.MainParticle.Position);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vCache.CellIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vCache.InnerSortIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", vCache.HashCount);
        GPUCountingSortHashCS.DispatchIndirect(InsertParticleIntoHashGridKernel, voTarget.Argument, (uint)(DynamicParticle.DifferParticleXGridCountArgumentOffset + Simulator.OnlyTallCellParticleTypeIndex * 3) * 4);

        RearrangePartileData(voTarget, vCache);

        GPUCountingSortHashCS.DisableKeyword("HashTallCell");
    }

    public void ZSortFullParticle(DynamicParticle voTarget, SimulatorGPUCache vCache, Vector3 vMin, float vCellLength)
    {
        m_GPUBufferClear.ClraeUIntBufferWithZero(vCache.HashCount);

        GPUCountingSortHashCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
        GPUCountingSortHashCS.SetFloat("HashGridCellLength", vCellLength);
        GPUCountingSortHashCS.SetInt("TargetParticleType", -1);

        GPUCountingSortHashCS.SetInt("ParticleCountArgumentOffset", DynamicParticle.ParticleCountArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleSplitPointArgumentOffset", DynamicParticle.DifferParticleSplitPointArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleCountArgumentOffset", DynamicParticle.DifferParticleCountArgumentOffset);

        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleIndirectArgment_R", voTarget.Argument);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.MainParticle.Position);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vCache.CellIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vCache.InnerSortIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", vCache.HashCount);
        GPUCountingSortHashCS.DispatchIndirect(InsertParticleIntoHashGridKernel, voTarget.Argument);

        vCache.GPUScan.Scan(vCache.HashCount, vCache.HashOffset, vCache.GPUScanHillisCache);

        RearrangePartileData(voTarget, vCache);
    }

    public void SortIntersectCellParticle(DynamicParticle voTarget, SimulatorGPUCache vCache, Vector3 vMin, float vCellLength)
    {
        m_GPUBufferClear.ClraeUIntBufferWithZero(vCache.HashCount);
        GPUCountingSortHashCS.EnableKeyword("HashIntersectCell");

        GPUCountingSortHashCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
        GPUCountingSortHashCS.SetFloat("HashGridCellLength", vCellLength);
        GPUCountingSortHashCS.SetInt("TargetParticleType", Simulator.IntersectCellParticleTypeIndex);

        GPUCountingSortHashCS.SetInt("ParticleCountArgumentOffset", DynamicParticle.ParticleCountArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleSplitPointArgumentOffset", DynamicParticle.DifferParticleSplitPointArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleCountArgumentOffset", DynamicParticle.DifferParticleCountArgumentOffset);

        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleIndirectArgment_R", voTarget.Argument);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.MainParticle.Position);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vCache.CellIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vCache.InnerSortIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", vCache.HashCount);
        GPUCountingSortHashCS.DispatchIndirect(InsertParticleIntoHashGridKernel, voTarget.Argument, (uint)(DynamicParticle.DifferParticleXGridCountArgumentOffset + Simulator.IntersectCellParticleTypeIndex * 3) * 4);

        vCache.GPUScan.Scan(vCache.HashCount, vCache.HashOffset, vCache.GPUScanHillisCache);

        RearrangePartileData(voTarget, vCache);

        GPUCountingSortHashCS.DisableKeyword("HashIntersectCell");
    }

    public void SortRegularCellParticle(DynamicParticle voTarget, Grid vGrid, SimulatorGPUCache vCache, Vector3 vMin, float vCellLength)
    {
        m_GPUBufferClear.ClraeUIntBufferWithZero(vCache.HashCount);
        GPUCountingSortHashCS.EnableKeyword("HashRegularCell");

        GPUCountingSortHashCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
        GPUCountingSortHashCS.SetFloat("HashGridCellLength", vCellLength);
        GPUCountingSortHashCS.SetInt("TargetParticleType", Simulator.OnlyRegularCellParticleTypeIndex);

        GPUCountingSortHashCS.SetInt("ParticleCountArgumentOffset", DynamicParticle.ParticleCountArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleSplitPointArgumentOffset", DynamicParticle.DifferParticleSplitPointArgumentOffset);
        GPUCountingSortHashCS.SetInt("DifferParticleCountArgumentOffset", DynamicParticle.DifferParticleCountArgumentOffset);

        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleIndirectArgment_R", voTarget.Argument);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.MainParticle.Position);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vCache.CellIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vCache.InnerSortIndexCache);
        GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", vCache.HashCount);
        GPUCountingSortHashCS.SetTexture(InsertParticleIntoHashGridKernel, "TerrianHeight_R", vGrid.FineGrid.TerrainHeight);
        GPUCountingSortHashCS.SetTexture(InsertParticleIntoHashGridKernel, "TallCellHeight_R", vGrid.FineGrid.TallCellHeight);
        GPUCountingSortHashCS.DispatchIndirect(InsertParticleIntoHashGridKernel, voTarget.Argument, (uint)(DynamicParticle.DifferParticleXGridCountArgumentOffset + Simulator.OnlyRegularCellParticleTypeIndex * 3) * 4);

        vCache.GPUScan.Scan(vCache.HashCount, vCache.HashOffset, vCache.GPUScanHillisCache);

        RearrangePartileData(voTarget, vCache);

        GPUCountingSortHashCS.DisableKeyword("HashRegularCell");
    }

    private void RearrangePartileData(DynamicParticle voTarget, SimulatorGPUCache vCache)
    {
        vCache.GPUScan.Scan(vCache.HashCount, vCache.HashOffset, vCache.GPUScanHillisCache);

        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "HashGridCellParticleOffset_R", vCache.HashOffset);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleIndirectArgment_R", voTarget.Argument);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleCellIndex_R", vCache.CellIndexCache);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleInnerSortIndex_R", vCache.InnerSortIndexCache);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticlePosition_R", voTarget.MainParticle.Position);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleVelocity_R", voTarget.MainParticle.Velocity);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleFilter_R", voTarget.MainParticle.Filter);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticlePosition_RW", vCache.ParticleCache.Position);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticleVelocity_RW", vCache.ParticleCache.Velocity);
        GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticleFilter_RW", vCache.ParticleCache.Filter);
        GPUCountingSortHashCS.DispatchIndirect(CountingSortFullKernel, voTarget.Argument);

        Particle Temp = vCache.ParticleCache;
        vCache.ParticleCache = voTarget.MainParticle;
        voTarget.MainParticle = Temp;
    }

    private GPUBufferClear m_GPUBufferClear;
    private ComputeShader GPUCountingSortHashCS;
    private int InsertParticleIntoHashGridKernel;
    private int CountingSortFullKernel;
}
