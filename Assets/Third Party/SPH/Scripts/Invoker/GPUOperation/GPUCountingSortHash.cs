using UnityEngine;

namespace LODFluid
{
    public class GPUCountingSortHash
    {
        private ComputeShader GPUCountingHashSortCS;
        private int insertParticleIntoHashGridKernel;
        private int countingSortFullKernel;

        ComputeBuffer ParticleCellIndexCache;
        ComputeBuffer ParticleInnerSortIndexCache;

        ParticleBuffer SortedParticleCache;

        GPUScan GPUScanner;
        GPUBufferClear GPUBufferClearer;

        ~GPUCountingSortHash()
        {
            ParticleCellIndexCache.Release();
            ParticleInnerSortIndexCache.Release();
        }

        public GPUCountingSortHash(uint vMaxParticleCount)
        {
            GPUCountingHashSortCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPUCountingSortHash");
            insertParticleIntoHashGridKernel = GPUCountingHashSortCS.FindKernel("insertParticleIntoHashGrid");
            countingSortFullKernel = GPUCountingHashSortCS.FindKernel("countingSortFull");

            SortedParticleCache = new ParticleBuffer(vMaxParticleCount);

            ParticleCellIndexCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            ParticleInnerSortIndexCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));

            GPUScanner = new GPUScan(vMaxParticleCount);
            GPUBufferClearer = new GPUBufferClear();
        }

        public void CountingHashSort(
            ref ParticleBuffer voTarget,
            ComputeBuffer voHashGridParticleCount,
            ComputeBuffer voHashGridParticleOffset,
            ComputeBuffer vArgumentBuffer,
            Vector3 vHashGridMin, float vHashGridCellLength)
        {
            GPUBufferClearer.ClearFloatBufferWithZero(voHashGridParticleCount.count, voHashGridParticleCount);

            GPUCountingHashSortCS.SetVector("HashGridMin", vHashGridMin);
            GPUCountingHashSortCS.SetFloat("HashGridCellLength", vHashGridCellLength);

            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", vArgumentBuffer);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleCellIndex_RW", ParticleCellIndexCache);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", voHashGridParticleCount);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", ParticleInnerSortIndexCache);
            GPUCountingHashSortCS.DispatchIndirect(insertParticleIntoHashGridKernel, vArgumentBuffer);

            GPUScanner.Scan(voHashGridParticleCount, voHashGridParticleOffset);

            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleIndrectArgment_R", vArgumentBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleCellIndex_R", ParticleCellIndexCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleInnerSortIndex_R", ParticleInnerSortIndexCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "HashGridCellParticleOffset_R", voHashGridParticleOffset);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleVelocity_R", voTarget.ParticleVelocityBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleFilter_R", voTarget.ParticleFilterBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleMortonCode_R", voTarget.ParticleMortonCodeBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleLifeTime_R", voTarget.ParticleLifeTimeBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticlePosition_RW", SortedParticleCache.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleVelocity_RW", SortedParticleCache.ParticleVelocityBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleFilter_RW", SortedParticleCache.ParticleFilterBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleLifeTime_RW", SortedParticleCache.ParticleLifeTimeBuffer);
            GPUCountingHashSortCS.DispatchIndirect(countingSortFullKernel, vArgumentBuffer);

            ParticleBuffer Temp = SortedParticleCache;
            SortedParticleCache = voTarget;
            voTarget = Temp;
        }
    }
}
