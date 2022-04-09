using System.Collections.Generic;
using UnityEngine;
using GPUDPP;

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
        public ComputeBuffer Argument { get { return m_Argument; } }
        public Particle MainParticle { get { return m_MainParticle; } }

        public DynamicParticle(int vMaxCount, float vRadius)
        {
            m_MaxSize = vMaxCount;
            m_Radius = vRadius;
            m_MainParticle = new Particle(vMaxCount);
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

            m_GPUScan = new GPUScan();
            m_GPUBufferClear = new GPUBufferClear();

            m_SPHVisualMaterial = Resources.Load<Material>("DrawSPHParticle");

            GPUCountingSortHashCS = Resources.Load<ComputeShader>("GPUCountingSortHash");
            InsertParticleIntoHashGridKernel = GPUCountingSortHashCS.FindKernel("insertParticleIntoHashGrid");
            CountingSortFullKernel = GPUCountingSortHashCS.FindKernel("countingSortFull");

            GPUDynamicParticleToolCS = Resources.Load<ComputeShader>("GPUDynamicParticleTool");
            AddParticleBlockKernel = GPUDynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = GPUDynamicParticleToolCS.FindKernel("updateParticleCountArgment");
            ScatterParticleDataKernel = GPUDynamicParticleToolCS.FindKernel("scatterParticleData");
            UpdateParticleNarrowCountArgmentKernel = GPUDynamicParticleToolCS.FindKernel("updateParticleNarrowCountArgment");
            DeleteParticleOutofRangeKernel = GPUDynamicParticleToolCS.FindKernel("deleteParticleOutofRange");
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

        public void SetData(List<Vector3> vPosition, List<Vector3> vVelocity, List<int> vFilter, int vSize)
        {
            m_MainParticle.Position.SetData(vPosition.ToArray(), 0, 0, vSize);
            m_MainParticle.Velocity.SetData(vVelocity.ToArray(), 0, 0, vSize);
            m_MainParticle.Filter.SetData(vFilter.ToArray(), 0, 0, vSize);
            int[] InitArgument = new int[7] { Mathf.CeilToInt(vSize / Common.ThreadCount1D), 1, 1, 3, vSize, 0, 0 };
            m_Argument.SetData(InitArgument);
        }

        public void ZSort(Vector3 vMin, float vCellLength)
        {
            m_GPUBufferClear.ClraeUIntBufferWithZero(m_HashCount);

            GPUCountingSortHashCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
            GPUCountingSortHashCS.SetFloat("HashGridCellLength", vCellLength);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticlePosition_R", m_MainParticle.Position);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleCellIndex_RW", m_CellIndexCache);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "ParticleInnerSortIndex_RW", m_InnerSortIndexCache);
            GPUCountingSortHashCS.SetBuffer(InsertParticleIntoHashGridKernel, "HashGridCellParticleCount_RW", m_HashCount);
            GPUCountingSortHashCS.DispatchIndirect(InsertParticleIntoHashGridKernel, m_Argument);

            m_GPUScan.Scan(m_HashCount, m_HashOffset, m_ScanCache1, m_ScanCache2);

            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleCellIndex_R", m_CellIndexCache);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleInnerSortIndex_R", m_InnerSortIndexCache);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "HashGridCellParticleOffset_R", m_HashOffset);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticlePosition_R", m_MainParticle.Position);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleVelocity_R", m_MainParticle.Velocity);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "ParticleFilter_R", m_MainParticle.Filter);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticlePosition_RW", m_ParticleCache.Position);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticleVelocity_RW", m_ParticleCache.Velocity);
            GPUCountingSortHashCS.SetBuffer(CountingSortFullKernel, "SortedParticleFilter_RW", m_ParticleCache.Filter);
            GPUCountingSortHashCS.DispatchIndirect(CountingSortFullKernel, m_Argument);

            Particle Temp = m_ParticleCache;
            m_ParticleCache = m_MainParticle;
            m_MainParticle = Temp;
        }

        public void OrganizeParticle(Vector3 vMin, Vector3 vMax, float vCellLength)
        {
            GPUDynamicParticleToolCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
            GPUDynamicParticleToolCS.SetFloat("HashGridCellLength", vCellLength);
            Vector3 Res = (vMax - vMin) / vCellLength;
            GPUDynamicParticleToolCS.SetInts("HashGridResolution", Mathf.CeilToInt(Res.x), Mathf.CeilToInt(Res.y), Mathf.CeilToInt(Res.z));
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticlePosition_R", m_MainParticle.Position);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleFilter_RW", m_MainParticle.Filter);
            GPUDynamicParticleToolCS.DispatchIndirect(DeleteParticleOutofRangeKernel, m_Argument);

            m_GPUScan.Scan(m_MainParticle.Filter, m_ScatterOffsetCache, m_ScanCache1, m_ScanCache2);

            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleScatterOffset_R", m_ScatterOffsetCache);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleIndrectArgment_R", m_Argument);

            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticlePosition_R", m_MainParticle.Position);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleVelocity_R", m_MainParticle.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleFilter_R", m_MainParticle.Filter);

            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticlePosition_RW", m_ParticleCache.Position);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleVelocity_RW", m_ParticleCache.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleFilter_RW", m_ParticleCache.Filter);

            GPUDynamicParticleToolCS.DispatchIndirect(ScatterParticleDataKernel, m_Argument);

            GPUDynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleScatterOffset_R", m_ScatterOffsetCache);
            GPUDynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleIndrectArgment_RW", m_Argument);
            GPUDynamicParticleToolCS.Dispatch(UpdateParticleNarrowCountArgmentKernel, 1, 1, 1);

            Particle Temp = m_ParticleCache;
            m_ParticleCache = m_MainParticle;
            m_MainParticle = Temp;
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
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticlePosition_RW", m_MainParticle.Position);
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleVelocity_RW", m_MainParticle.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleFilter_RW", m_MainParticle.Filter);
            GPUDynamicParticleToolCS.Dispatch(AddParticleBlockKernel, (int)Mathf.Ceil((float)AddedParticleCount / Common.ThreadCount1D), 1, 1);

            GPUDynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            GPUDynamicParticleToolCS.SetInt("MaxParticleCount", m_MaxSize);
            GPUDynamicParticleToolCS.SetBuffer(UpdateParticleCountArgmentKernel, "ParticleIndrectArgment_RW", m_Argument);
            GPUDynamicParticleToolCS.Dispatch(UpdateParticleCountArgmentKernel, 1, 1, 1);
        }

        public void VisualParticle()
        {
            m_SPHVisualMaterial.SetPass(0);
            m_SPHVisualMaterial.SetBuffer("_particlePositionBuffer", m_MainParticle.Position);
            m_SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", m_MainParticle.Velocity);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, m_Argument, 12);
        }

        private float m_Radius;
        private int m_MaxSize;

        private Particle m_MainParticle;
        private Particle m_ParticleCache;
        private ComputeBuffer m_HashCount;
        private ComputeBuffer m_HashOffset;
        private ComputeBuffer m_CellIndexCache;
        private ComputeBuffer m_ScanCache1;
        private ComputeBuffer m_ScanCache2;
        private ComputeBuffer m_InnerSortIndexCache;
        private ComputeBuffer m_ScatterOffsetCache;
        private ComputeBuffer m_Argument;

        private GPUScan m_GPUScan;
        private GPUBufferClear m_GPUBufferClear;

        private Material m_SPHVisualMaterial;

        private ComputeShader GPUCountingSortHashCS;
        private int InsertParticleIntoHashGridKernel;
        private int CountingSortFullKernel;

        private ComputeShader GPUDynamicParticleToolCS;
        private int AddParticleBlockKernel;
        private int UpdateParticleCountArgmentKernel;
        private int ScatterParticleDataKernel;
        private int UpdateParticleNarrowCountArgmentKernel;
        private int DeleteParticleOutofRangeKernel;
    }
}
