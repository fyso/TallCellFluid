using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace LODFluid
{
    public class DynamicParticle
    {
        private ComputeShader DynamicParticleToolCS;
        private int AddParticleBlockKernel;
        private int UpdateParticleCountArgmentKernel;
        private int ScatterParticleDataKernel;
        private int UpdateParticleNarrowCountArgmentKernel;
        private int DeleteParticleOutofRangeKernel;

        private uint MaxParticleSize;
        private GPUScan GPUScanner;

        ~DynamicParticle()
        {
        }

        public DynamicParticle(uint vMaxParticleSize)
        {
            DynamicParticleToolCS = Resources.Load<ComputeShader>("Shaders/DynamicParticleTool");
            AddParticleBlockKernel = DynamicParticleToolCS.FindKernel("addParticleBlock");
            UpdateParticleCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleCountArgment");
            ScatterParticleDataKernel = DynamicParticleToolCS.FindKernel("scatterParticleData");
            UpdateParticleNarrowCountArgmentKernel = DynamicParticleToolCS.FindKernel("updateParticleNarrowCountArgment");
            DeleteParticleOutofRangeKernel = DynamicParticleToolCS.FindKernel("deleteParticleOutofRange");

            MaxParticleSize = vMaxParticleSize;
            GPUScanner = new GPUScan();
        }

        public void AddParticleBlock(
            ParticleBuffer voTarget,
            ComputeBuffer voParticleIndirectArgumentBuffer,
            Vector3 vWaterGeneratePos,
            Vector3Int vWaterBlockRes,
            Vector3 vInitParticleVel,
            float vParticleRadius)
        {
            int AddedParticleCount = vWaterBlockRes.x * vWaterBlockRes.y * vWaterBlockRes.z;
            DynamicParticleToolCS.SetFloats("WaterGeneratePos", vWaterGeneratePos.x, vWaterGeneratePos.y, vWaterGeneratePos.z);
            DynamicParticleToolCS.SetInt("WaterBlockResX", vWaterBlockRes.x);
            DynamicParticleToolCS.SetInt("WaterBlockResY", vWaterBlockRes.y);
            DynamicParticleToolCS.SetInt("WaterBlockResZ", vWaterBlockRes.z);
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)MaxParticleSize);
            DynamicParticleToolCS.SetFloat("ParticleRadius", vParticleRadius);
            DynamicParticleToolCS.SetFloats("ParticleInitVel", vInitParticleVel.x, vInitParticleVel.y, vInitParticleVel.z);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleIndrectArgment_RW", voParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticlePosition_RW", voTarget.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleVelocity_RW", voTarget.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(AddParticleBlockKernel, "ParticleFilter_RW", voTarget.ParticleFilterBuffer);
            DynamicParticleToolCS.Dispatch(AddParticleBlockKernel, (int)Mathf.Ceil((float)AddedParticleCount / Common.SPHThreadCount), 1, 1);
            
            DynamicParticleToolCS.SetInt("AddedParticleCount", AddedParticleCount);
            DynamicParticleToolCS.SetInt("MaxParticleCount", (int)MaxParticleSize);
            DynamicParticleToolCS.SetBuffer(UpdateParticleCountArgmentKernel, "ParticleIndrectArgment_RW", voParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleCountArgmentKernel, 1, 1, 1);
        }

        public void DeleteParticleOutofRange(
             ParticleBuffer voTarget,
             ComputeBuffer vParticleIndirectArgumentBuffer,
             ComputeBuffer vParticleBoundaryDistanceBuffer,
             Vector3 vHashGridMin,
             float vHashGridCellLength,
             Vector3Int vHashGridResolution,
             float vMaxLifeTime)
        {
            DynamicParticleToolCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            DynamicParticleToolCS.SetFloat("HashGridCellLength", vHashGridCellLength);
            DynamicParticleToolCS.SetInts("HashGridResolution", vHashGridResolution.x, vHashGridResolution.y, vHashGridResolution.z);
            DynamicParticleToolCS.SetFloat("MaxLifeTime", vMaxLifeTime);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticlePosition_R", voTarget.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleLifeTime_R", voTarget.ParticleLifeTimeBuffer);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleBoundaryDiatance_R", vParticleBoundaryDistanceBuffer);
            DynamicParticleToolCS.SetBuffer(DeleteParticleOutofRangeKernel, "ParticleFilter_RW", voTarget.ParticleFilterBuffer);
            DynamicParticleToolCS.DispatchIndirect(DeleteParticleOutofRangeKernel, vParticleIndirectArgumentBuffer);
        }

        public void NarrowParticleData(
            ref ParticleBuffer voTargetParticleBuffer,
            ref ParticleBuffer voNarrowParticleCache,
            ComputeBuffer vParticleScatterOffsetCache,
            ComputeBuffer vParticleIndirectArgumentBuffer)
        {
            GPUScanner.Scan(
                voTargetParticleBuffer.ParticleFilterBuffer,
                vParticleScatterOffsetCache);

            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleScatterOffset_R", vParticleScatterOffsetCache);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "ParticleIndrectArgment_R", vParticleIndirectArgumentBuffer);

            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticlePosition_RW", voNarrowParticleCache.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleVelocity_RW", voNarrowParticleCache.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleFilter_RW", voNarrowParticleCache.ParticleFilterBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "NarrowParticleLifeTime_RW", voNarrowParticleCache.ParticleLifeTimeBuffer);

            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticlePosition_R", voTargetParticleBuffer.ParticlePositionBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleVelocity_R", voTargetParticleBuffer.ParticleVelocityBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleFilter_R", voTargetParticleBuffer.ParticleFilterBuffer);
            DynamicParticleToolCS.SetBuffer(ScatterParticleDataKernel, "TargetParticleLifeTime_R", voTargetParticleBuffer.ParticleLifeTimeBuffer);

            DynamicParticleToolCS.DispatchIndirect(ScatterParticleDataKernel, vParticleIndirectArgumentBuffer);

            DynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleScatterOffset_R", vParticleScatterOffsetCache);
            DynamicParticleToolCS.SetBuffer(UpdateParticleNarrowCountArgmentKernel, "ParticleIndrectArgment_RW", vParticleIndirectArgumentBuffer);
            DynamicParticleToolCS.Dispatch(UpdateParticleNarrowCountArgmentKernel, 1, 1, 1);

            ParticleBuffer Temp = voNarrowParticleCache;
            voNarrowParticleCache = voTargetParticleBuffer;
            voTargetParticleBuffer = Temp;

        }
    }
}
