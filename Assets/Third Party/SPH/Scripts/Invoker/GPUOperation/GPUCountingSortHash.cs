using UnityEngine;

namespace LODFluid
{
    public class GPUCountingSortHash
    {
        private ComputeShader GPUCountingHashSortCS;
        private int insertParticleIntoHashGridKernel;
        private int countingSortFullKernel;

        public GPUScan GPUScanner;
        public GPUBufferClear GPUBufferClearer;

        public void Release()
        {
        }

        public GPUCountingSortHash()
        {
            GPUCountingHashSortCS = Resources.Load<ComputeShader>("Shaders/GPU Operation/GPUCountingSortHash");
            insertParticleIntoHashGridKernel = GPUCountingHashSortCS.FindKernel("insertParticleIntoHashGrid");
            countingSortFullKernel = GPUCountingHashSortCS.FindKernel("countingSortFull");


            GPUScanner = new GPUScan();
            GPUBufferClearer = new GPUBufferClear();
        }

        public void CountingHashSort(
            ref ParticleBuffer voTarget,
            ref ParticleBuffer voSortedCache,
            ComputeBuffer voHashGridParticleCount,
            ComputeBuffer voHashGridParticleOffset,
            ComputeBuffer vArgumentBuffer,
            ComputeBuffer vParticleCellIndexCache,
            ComputeBuffer vParticleInnerSortIndexCache,
            Vector3 vHashGridMin, float vHashGridCellLength)
        {
            GPUBufferClearer.ClearFloatBufferWithZero(voHashGridParticleCount.count, voHashGridParticleCount);

            GPUCountingHashSortCS.SetVector("HashGridMin", vHashGridMin);
            GPUCountingHashSortCS.SetFloat("HashGridCellLength", vHashGridCellLength);

            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", vArgumentBuffer);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleCellIndex_RW", vParticleCellIndexCache);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", voHashGridParticleCount);
            GPUCountingHashSortCS.SetBuffer(insertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", vParticleInnerSortIndexCache);
            GPUCountingHashSortCS.DispatchIndirect(insertParticleIntoHashGridKernel, vArgumentBuffer);

            GPUScanner.Scan(voHashGridParticleCount, voHashGridParticleOffset);

            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleIndrectArgment_R", vArgumentBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleCellIndex_R", vParticleCellIndexCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleInnerSortIndex_R", vParticleInnerSortIndexCache);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "HashGridCellParticleOffset_R", voHashGridParticleOffset);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleVelocity_R", voTarget.ParticleVelocityBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleFilter_R", voTarget.ParticleFilterBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "ParticleLifeTime_R", voTarget.ParticleLifeTimeBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticlePosition_RW", voSortedCache.ParticlePositionBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleVelocity_RW", voSortedCache.ParticleVelocityBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleFilter_RW", voSortedCache.ParticleFilterBuffer);
            GPUCountingHashSortCS.SetBuffer(countingSortFullKernel, "SortedParticleLifeTime_RW", voSortedCache.ParticleLifeTimeBuffer);
            GPUCountingHashSortCS.DispatchIndirect(countingSortFullKernel, vArgumentBuffer);

            ParticleBuffer Temp = voSortedCache;
            voSortedCache = voTarget;
            voTarget = Temp;
        }
    }
}
