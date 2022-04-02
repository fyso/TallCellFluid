using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DParticle
{
    public class Particle
    {
        public ComputeBuffer Position { get { return m_Position; } }
        public ComputeBuffer Velocity { get { return m_Velocity; } }
        public ComputeBuffer Filter { get { return m_Filter; } }

        public Particle(int vMaxCount)
        {
            m_Position = new ComputeBuffer(vMaxCount, sizeof(float) * 3);
            m_Velocity = new ComputeBuffer(vMaxCount, sizeof(float) * 3);
            m_Filter = new ComputeBuffer(vMaxCount, sizeof(uint));
        }

        ~Particle()
        {
            m_Position.Release();
            m_Velocity.Release();
            m_Filter.Release();
        }

        private ComputeBuffer m_Position;
        private ComputeBuffer m_Velocity;
        private ComputeBuffer m_Filter;
    }

    public class DynamicParticle
    {
        public DynamicParticle(int vMaxCount, float vRadius)
        {
            m_MaxSize = vMaxCount;
            m_Radius = vRadius;
            m_Particle = new Particle(vMaxCount);
            m_ParticleCache = new Particle(vMaxCount);
            m_CellIndexCache = new ComputeBuffer(vMaxCount, sizeof(uint));
            m_HashCount = new ComputeBuffer(vMaxCount * 2, sizeof(uint));
            m_HashOffset = new ComputeBuffer(vMaxCount * 2, sizeof(uint));
            m_InnerSortIndexCache = new ComputeBuffer(vMaxCount, sizeof(uint));
            m_ScanCache1 = new ComputeBuffer(Common.ThreadCount1D * Common.ThreadCount1D, sizeof(uint));
            m_ScanCache2 = new ComputeBuffer(Common.ThreadCount1D, sizeof(uint));
            m_ScatterOffsetCache = new ComputeBuffer(vMaxCount, sizeof(uint));
            m_Argument = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            int[] InitArgument = new int[7] { 1, 1, 1, 3, 0, 0, 0 };
            m_Argument.SetData(InitArgument);

            m_SPHVisualMaterial = Resources.Load<Material>("DrawSPHParticle");

            GPUCountingSortHashCS = Resources.Load<ComputeShader>("GPUCountingSortHash");
            InsertParticleIntoHashGridKernel = GPUCountingSortHashCS.FindKernel("insertParticleIntoHashGrid");
            CountingSortFullKernel = GPUCountingSortHashCS.FindKernel("countingSortFull");

            GPUScanCS = Resources.Load<ComputeShader>("GPUScan");
            ScanInBucketKernel = GPUScanCS.FindKernel("scanInBucket");
            ScanBucketResultKernel = GPUScanCS.FindKernel("scanBucketResult");
            ScanAddBucketResultKernel = GPUScanCS.FindKernel("scanAddBucketResult");

            GPUDynamicParticleToolCS = Resources.Load<ComputeShader>("GPUDynamicParticleTool");
            AddParticleBlockKernel = GPUDynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = GPUDynamicParticleToolCS.FindKernel("updateParticleCountArgment");
            ScatterParticleDataKernel = GPUDynamicParticleToolCS.FindKernel("scatterParticleData");
            UpdateParticleNarrowCountArgmentKernel = GPUDynamicParticleToolCS.FindKernel("updateParticleNarrowCountArgment");
            DeleteParticleOutofRangeKernel = GPUDynamicParticleToolCS.FindKernel("deleteParticleOutofRange");

            GPUBufferClearCS = Resources.Load<ComputeShader>("GPUBufferClear");
            ClearUIntBufferWithZeroKernel = GPUBufferClearCS.FindKernel("clearUIntBufferWithZero");
        }

        ~DynamicParticle()
        {
            m_HashCount.Release();
            m_HashOffset.Release();
            m_CellIndexCache.Release();
            m_InnerSortIndexCache.Release();
            m_ScanCache1.Release();
            m_ScanCache2.Release();
            m_ScatterOffsetCache.Release();
            m_Argument.Release();
        }

        public void ZSort(Vector3 vMin, float vCellLength)
        {
            GPUBufferClearCS.SetInt("BufferSize", m_HashCount.count);
            GPUBufferClearCS.SetBuffer(ClearUIntBufferWithZeroKernel, "TargetUIntBuffer_RW", m_HashCount);
            GPUBufferClearCS.Dispatch(ClearUIntBufferWithZeroKernel, (int)Mathf.Ceil(((float)m_HashCount.count / Common.ThreadCount1D)), 1, 1);

            GPUCountingSortHashCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
            GPUCountingSortHashCS.SetFloat("HashGridCellLength", vCellLength);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticlePosition_R", m_Particle.Position);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleCellIndex_RW", m_CellIndexCache);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", m_InnerSortIndexCache);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", m_HashCount);
            GPUCountingSortHashCS.DispatchIndirect(InsertParticleIntoHashGridKernel, m_Argument);

            Scan(m_HashCount, m_HashOffset);

            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleCellIndex_R", m_CellIndexCache);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleInnerSortIndex_R", m_InnerSortIndexCache);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "HashGridCellParticleOffset_R", m_HashOffset);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticlePosition_R", m_Particle.Position);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleVelocity_R", m_Particle.Velocity);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleFilter_R", m_Particle.Filter);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticlePosition_RW", m_ParticleCache.Position);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticleVelocity_RW", m_ParticleCache.Velocity);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticleFilter_RW", m_ParticleCache.Filter);
            GPUCountingSortHashCS.DispatchIndirect(CountingSortFullKernel, m_Argument);

            Particle Temp = m_ParticleCache;
            m_ParticleCache = m_Particle;
            m_Particle = Temp;
        }

        public void OrganizeParticle(Vector3 vMin, float vCellLength, Vector3Int vResolution)
        {
            GPUDynamicParticleToolCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
            GPUDynamicParticleToolCS.SetFloat("HashGridCellLength", vCellLength);
            GPUDynamicParticleToolCS.SetInts("HashGridResolution", vResolution.x, vResolution.y, vResolution.z);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticlePosition_R", m_Particle.Position);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleFilter_RW", m_Particle.Filter);
            GPUDynamicParticleToolCS.DispatchIndirect(DeleteParticleOutofRangeKernel, m_Argument);

            Scan(m_Particle.Filter, m_ScatterOffsetCache);

            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleScatterOffset_R", m_ScatterOffsetCache);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleIndrectArgment_R", m_Argument);

            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticlePosition_R", m_Particle.Position);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleVelocity_R", m_Particle.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleFilter_R", m_Particle.Filter);

            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticlePosition_RW", m_ParticleCache.Position);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleVelocity_RW", m_ParticleCache.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleFilter_RW", m_ParticleCache.Filter);

            GPUDynamicParticleToolCS.DispatchIndirect(ScatterParticleDataKernel, m_Argument);

            GPUDynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleScatterOffset_R", m_ScatterOffsetCache);
            GPUDynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleIndrectArgment_RW", m_Argument);
            GPUDynamicParticleToolCS.Dispatch(UpdateParticleNarrowCountArgmentKernel, 1, 1, 1);

            Particle Temp = m_ParticleCache;
            m_ParticleCache = m_Particle;
            m_Particle = Temp;
        }

        public void AddParticleBlock( Vector3 vPosition, Vector3Int vResolution, Vector3 vVelocity)
        {
            int AddedParticleCount = vResolution.x * vResolution.y * vResolution.z;
            GPUDynamicParticleToolCS.SetFloats("WaterGeneratePos", vPosition.x, vPosition.y, vPosition.z);
            GPUDynamicParticleToolCS.SetInt("WaterBlockResX", vResolution.x);
            GPUDynamicParticleToolCS.SetInt("WaterBlockResY", vResolution.y);
            GPUDynamicParticleToolCS.SetInt("WaterBlockResZ", vResolution.z);
            GPUDynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            GPUDynamicParticleToolCS.SetInt("MaxParticleCount", m_MaxSize);
            GPUDynamicParticleToolCS.SetFloat("ParticleRadius", m_Radius);
            GPUDynamicParticleToolCS.SetFloats("ParticleInitVel", vVelocity.x, vVelocity.y, vVelocity.z);
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleIndrectArgment_RW", m_Argument);
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticlePosition_RW", m_Particle.Position);
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleVelocity_RW", m_Particle.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleFilter_RW", m_Particle.Filter);
            GPUDynamicParticleToolCS.Dispatch(AddParticleBlockKernel, (int)Mathf.Ceil((float)AddedParticleCount / Common.ThreadCount1D), 1, 1);

            GPUDynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            GPUDynamicParticleToolCS.SetInt("MaxParticleCount", m_MaxSize);
            GPUDynamicParticleToolCS.SetBuffer(UpdateParticleCountArgmentKernel, "ParticleIndrectArgment_RW", m_Argument);
            GPUDynamicParticleToolCS.Dispatch(UpdateParticleCountArgmentKernel, 1, 1, 1);
        }

        public void VisualParticle()
        {
            m_SPHVisualMaterial.SetPass(0);
            m_SPHVisualMaterial.SetBuffer("_particlePositionBuffer", m_Particle.Position);
            m_SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", m_Particle.Velocity);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, m_Argument, 12);
        }

        private void Scan(ComputeBuffer vCountBuffer, ComputeBuffer voOffsetBuffer)
        {
            GPUScanCS.SetBuffer(ScanInBucketKernel, "Input", vCountBuffer);
            GPUScanCS.SetBuffer(ScanInBucketKernel, "Output", voOffsetBuffer);
            int GroupCount = (int)Mathf.Ceil((float)vCountBuffer.count / Common.ThreadCount1D);
            GPUScanCS.Dispatch(ScanInBucketKernel, GroupCount, 1, 1);

            GroupCount = (int)Mathf.Ceil((float)GroupCount / Common.ThreadCount1D);
            if (GroupCount > 0)
            {
                GPUScanCS.SetBuffer(ScanBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(ScanBucketResultKernel, "Output", m_ScanCache1);
                GPUScanCS.Dispatch(ScanBucketResultKernel, GroupCount, 1, 1);

                GroupCount = (int)Mathf.Ceil((float)GroupCount / Common.ThreadCount1D);
                if (GroupCount > 0)
                {
                    GPUScanCS.SetBuffer(ScanBucketResultKernel, "Input", m_ScanCache1);
                    GPUScanCS.SetBuffer(ScanBucketResultKernel, "Output", m_ScanCache2);
                    GPUScanCS.Dispatch(ScanBucketResultKernel, GroupCount, 1, 1);

                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input", m_ScanCache1);
                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input1", m_ScanCache2);
                    GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Output", m_ScanCache1);
                    GPUScanCS.Dispatch(ScanAddBucketResultKernel, GroupCount * Common.ThreadCount1D, 1, 1);
                }

                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input", voOffsetBuffer);
                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Input1", m_ScanCache1);
                GPUScanCS.SetBuffer(ScanAddBucketResultKernel, "Output", voOffsetBuffer);
                GPUScanCS.Dispatch(ScanAddBucketResultKernel, (int)Mathf.Ceil(((float)vCountBuffer.count / Common.ThreadCount1D)), 1, 1);
            }
        }

        private float m_Radius;
        private int m_MaxSize;

        private Particle m_Particle;
        private Particle m_ParticleCache;
        private ComputeBuffer m_HashCount;
        private ComputeBuffer m_HashOffset;
        private ComputeBuffer m_CellIndexCache;
        private ComputeBuffer m_InnerSortIndexCache;
        private ComputeBuffer m_ScanCache1;
        private ComputeBuffer m_ScanCache2;
        private ComputeBuffer m_ScatterOffsetCache;
        private ComputeBuffer m_Argument;

        private Material m_SPHVisualMaterial;

        private ComputeShader GPUCountingSortHashCS;
        private int InsertParticleIntoHashGridKernel;
        private int CountingSortFullKernel;

        private ComputeShader GPUScanCS;
        private int ScanInBucketKernel;
        private int ScanBucketResultKernel;
        private int ScanAddBucketResultKernel;

        private ComputeShader GPUDynamicParticleToolCS;
        private int AddParticleBlockKernel;
        private int UpdateParticleCountArgmentKernel;
        private int ScatterParticleDataKernel;
        private int UpdateParticleNarrowCountArgmentKernel;
        private int DeleteParticleOutofRangeKernel;

        private ComputeShader GPUBufferClearCS;
        private int ClearUIntBufferWithZeroKernel;
    }
}
