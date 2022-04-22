using System.Collections.Generic;
using UnityEngine;
using GPUDPP;
using UnityEngine.Profiling;

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
        public static int BucketCount { get { return 5; } }
        public static int ParticleXGridCountArgumentOffset { get { return 0; } }
        public static int ParticleCountArgumentOffset { get { return 4; } }
        public static int DifferParticleSplitPointArgumentOffset { get { return 7; } }
        public static int DifferParticleCountArgumentOffset { get { return 12; } }
        public static int DifferParticleXGridCountArgumentOffset { get { return 17; } }

        public ComputeBuffer Argument { get { return m_Argument; } }
        public Particle MainParticle { get { return m_MainParticle; } set { m_MainParticle = value; } }
        public float Radius { get { return m_Radius; } }

        public DynamicParticle(int vMaxCount, float vRadius)
        {
            m_MaxSize = vMaxCount;
            m_Radius = vRadius;
            m_MainParticle = new Particle(vMaxCount);
            m_ParticleCache = new Particle(vMaxCount);
            m_Argument = new ComputeBuffer(29, sizeof(uint), ComputeBufferType.IndirectArguments);
            uint[] InitArgument = new uint[29] {
                1, 1, 1, //total particle dispatch indirect arguments
                3, 0, 0, 0, //total particle draw indirect arguments
                0, 0, 0, 0,  //different filter type particle split point ( max type count: 4 )
                0, //delete particle split point
                0, 0, 0, 0,  //different filter type particle count ( max type count: 4 )
                0, //delete particle count
                1, 1, 1, //first type particle dispatch indirect arguments
                1, 1, 1, //second type same to...
                1, 1, 1,
                1, 1, 1
            };
            m_Argument.SetData(InitArgument);

            m_MultiSplit = new GPUMultiSplit();
            m_MultiSplitPlan = new GPUMultiSplitPlan(vMaxCount, 32, 32);

            m_SPHVisualMaterial = Resources.Load<Material>("DrawSPHParticle");

            GPUDynamicParticleToolCS = Resources.Load<ComputeShader>("GPUDynamicParticleTool");
            AddParticleBlockKernel = GPUDynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = GPUDynamicParticleToolCS.FindKernel("updateParticleCountArgment");
            UpdateArgmentKernel = GPUDynamicParticleToolCS.FindKernel("updateArgment");
            DeleteParticleOutofRangeKernel = GPUDynamicParticleToolCS.FindKernel("deleteParticleOutofRange");
            RearrangeParticleKernel = GPUDynamicParticleToolCS.FindKernel("rearrangeParticle");
        }

        ~DynamicParticle()
        {
            m_Argument.Release();
        }

        public void SetData(List<Vector3> vPosition, List<Vector3> vVelocity, List<int> vFilter, int vSize)
        {
            m_MainParticle.Position.SetData(vPosition.ToArray(), 0, 0, vSize);
            m_MainParticle.Velocity.SetData(vVelocity.ToArray(), 0, 0, vSize);
            m_MainParticle.Filter.SetData(vFilter.ToArray(), 0, 0, vSize);
            uint[] InitArgument = new uint[29] { 
                (uint)Mathf.CeilToInt(vSize / Common.ThreadCount1D), 1, 1,  //total particle dispatch indirect arguments
                3, (uint)vSize, 0, 0, //total particle draw indirect arguments
                0, 0, 0, 0,  //different filter type particle split point ( max type count: 4 )
                0, //delete particle split point
                0, 0, 0, 0,  //different filter type particle count ( max type count: 4 )
                0, //delete particle split point
                1, 1, 1, //first type particle dispatch indirect arguments
                1, 1, 1, //second type same to...
                1, 1, 1,
                1, 1, 1
            };
            m_Argument.SetData(InitArgument);
        }

        public void DeleteParticleOutofRange(Vector3 vMin, Vector3 vMax, float vCellLength)
        {
            GPUDynamicParticleToolCS.SetFloats("HashGridMin", vMin.x, vMin.y, vMin.z);
            GPUDynamicParticleToolCS.SetFloat("HashGridCellLength", vCellLength);

            Vector3 Res = (vMax - vMin) / vCellLength;
            GPUDynamicParticleToolCS.SetInts("HashGridResolution", Mathf.CeilToInt(Res.x), Mathf.CeilToInt(Res.y), Mathf.CeilToInt(Res.z));

            Profiler.BeginSample("DeleteParticleOutofRangeKernel");
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticlePosition_R", m_MainParticle.Position);
            GPUDynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleFilter_RW", m_MainParticle.Filter);
            GPUDynamicParticleToolCS.DispatchIndirect(DeleteParticleOutofRangeKernel, m_Argument);
            Profiler.EndSample();
        }

        public void OrganizeParticle()
        {
            Profiler.BeginSample("ComputeNewIndex");
            m_MultiSplit.ComputeNewIndex(m_MainParticle.Filter, m_MultiSplitPlan, BucketCount, m_Argument, ParticleCountArgumentOffset, ParticleXGridCountArgumentOffset, DifferParticleSplitPointArgumentOffset);
            Profiler.EndSample();

            Profiler.BeginSample("RearrangeParticleKernel");
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "ParticleIndrectArgment_R", m_Argument);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "NewIndex_R", m_MultiSplitPlan.NewIndex);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "OldPosition_R", m_MainParticle.Position);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "OldVelocity_R", m_MainParticle.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "OldFilter_R", m_MainParticle.Filter);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "RearrangedPosition_RW", m_ParticleCache.Position);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "RearrangedVelocity_RW", m_ParticleCache.Velocity);
            GPUDynamicParticleToolCS.SetBuffer(RearrangeParticleKernel, "RearrangedFilter_RW", m_ParticleCache.Filter);
            GPUDynamicParticleToolCS.DispatchIndirect(RearrangeParticleKernel, m_Argument);
            Profiler.EndSample();

            Profiler.BeginSample("UpdateParticleNarrowCountArgmentKernel");
            GPUDynamicParticleToolCS.SetBuffer(UpdateArgmentKernel, "ParticleIndrectArgment_RW", m_Argument);
            GPUDynamicParticleToolCS.Dispatch(UpdateArgmentKernel, 1, 1, 1);
            Profiler.EndSample();

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
            Profiler.BeginSample("VisualParticle");
            m_SPHVisualMaterial.SetPass(0);
            m_SPHVisualMaterial.SetBuffer("_particlePositionBuffer", m_MainParticle.Position);
            m_SPHVisualMaterial.SetBuffer("_particleVelocityBuffer", m_MainParticle.Velocity);
            m_SPHVisualMaterial.SetBuffer("_particleFilterBuffer", m_MainParticle.Filter);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, m_Argument, 12);
            Profiler.EndSample();
        }

        private float m_Radius;
        private int m_MaxSize;

        private Particle m_MainParticle;
        private Particle m_ParticleCache;
        private ComputeBuffer m_Argument;

        private GPUMultiSplit m_MultiSplit;
        private GPUMultiSplitPlan m_MultiSplitPlan;

        private Material m_SPHVisualMaterial;

        private ComputeShader GPUDynamicParticleToolCS;
        private int AddParticleBlockKernel;
        private int UpdateParticleCountArgmentKernel;
        private int UpdateArgmentKernel;
        private int DeleteParticleOutofRangeKernel;
        private int RearrangeParticleKernel;
    }
}
