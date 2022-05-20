using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DivergenceFreeSPH
    {
        private ComputeShader DivergenceFreeSPHSloverCS;
        private int computeFluidPropertyKernel;
        private int computeDensityChangeKernel;
        private int sloveDivergenceIterationKernel;
        private int computeDensityAdvKernel;
        private int slovePressureIterationKernel;
        private int updateVelocityWithNoPressureForceKernel;
        private int advectAndSwapParticleBufferKernel;
        private int generateFoamParticle;
        private int updateFoamParticleCountArgument;
        private int advectFoamParticle;

        private ParticleBuffer BackParticleCache;

        ~DivergenceFreeSPH()
        {

        }

        public DivergenceFreeSPH(uint vMaxParticleCount)
        {
            DivergenceFreeSPHSloverCS = Resources.Load<ComputeShader>("Shaders/Solver/DivergenceFreeSPHSolver");
            computeFluidPropertyKernel = DivergenceFreeSPHSloverCS.FindKernel("computeFluidProperty");
            computeDensityChangeKernel = DivergenceFreeSPHSloverCS.FindKernel("computeDensityChange");
            sloveDivergenceIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solveDivergenceIteration");
            computeDensityAdvKernel = DivergenceFreeSPHSloverCS.FindKernel("computeDensityAdv");
            slovePressureIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solvePressureIteration");
            updateVelocityWithNoPressureForceKernel = DivergenceFreeSPHSloverCS.FindKernel("updateVelocityWithNoPressureForce");
            advectAndSwapParticleBufferKernel = DivergenceFreeSPHSloverCS.FindKernel("advectAndSwapParticleBuffer");
            generateFoamParticle = DivergenceFreeSPHSloverCS.FindKernel("generateFoamParticle");
            updateFoamParticleCountArgument = DivergenceFreeSPHSloverCS.FindKernel("updateFoamParticleCountArgument");
            advectFoamParticle = DivergenceFreeSPHSloverCS.FindKernel("advectFoamParticle");

            BackParticleCache = new ParticleBuffer(vMaxParticleCount);
        }

        public void Advect(
            ref ParticleBuffer voTarget,
            ComputeBuffer vTargetParticleIndirectArgment, 
            float vTimeStep)
        {
            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);

            ///更新位置并Swap ParticleBuffer
            Profiler.BeginSample("Advect and swap particle buffer");
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticlePosition_R", voTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticleVelocity_R", voTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "BackParticleFilter_R", voTarget.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticlePosition_RW", BackParticleCache.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticleVelocity_RW", BackParticleCache.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectAndSwapParticleBufferKernel, "FrontParticleFilter_RW", BackParticleCache.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(advectAndSwapParticleBufferKernel, vTargetParticleIndirectArgment);
            Profiler.EndSample();

            Common.SwapComputeBuffer(ref BackParticleCache.ParticlePositionBuffer, ref voTarget.ParticlePositionBuffer);
            Common.SwapComputeBuffer(ref BackParticleCache.ParticleVelocityBuffer, ref voTarget.ParticleVelocityBuffer);
            Common.SwapComputeBuffer(ref BackParticleCache.ParticleFilterBuffer, ref voTarget.ParticleFilterBuffer);
        }

        public void Slove(
            ref ParticleBuffer voTarget,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vTargetParticleFoamParticleCountCache,
            ComputeBuffer vTargetParticleDensityCache,
            ComputeBuffer vTargetParticleAlphaCache,
            ComputeBuffer vTargetParticleDensityChangeCache,
            ComputeBuffer vTargetParticleDensityAdvCache,
            ComputeBuffer vTargetParticleNormalCache,
            ComputeBuffer vTargetParticleClosestPointCache,
            ComputeBuffer vTargetParticleVolumeCache,
            ComputeBuffer vTargetParticleBoundaryVelocityBufferCache,
            ComputeBuffer vNarrowPositionBuffer,
            ComputeBuffer vAnisotropyBuffer,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vSearchRadius, float vParticleVolume, float vTimeStep, float vViscosity, float vSurfaceTension, float vGravity, 
            int vDivergenceFreeIterationCount = 3, int vPressureIterationCount = 2, 
            bool vUseVolumeMapBoundary = true, bool EnableDivergenceFreeSlover = true, 
            float vMinCurvature = 0.5f, float vMaxCurvature = 15.0f, float vMinRelativeVelLength = 0.5f, float vMaxRelativeVelLength = 15.0f, int vNumTaRate = 10, int vNumWcRate = 100)
        {
            DivergenceFreeSPHSloverCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            DivergenceFreeSPHSloverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionX", vHashGridResolution.x);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionY", vHashGridResolution.y);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionZ", vHashGridResolution.z);
            DivergenceFreeSPHSloverCS.SetFloat("SearchRadius", vSearchRadius);
            DivergenceFreeSPHSloverCS.SetFloat("ParticleVolume", vParticleVolume);
            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHSloverCS.SetFloat("Viscosity", vViscosity);
            DivergenceFreeSPHSloverCS.SetFloat("SurfaceTension", vSurfaceTension);
            DivergenceFreeSPHSloverCS.SetFloat("Gravity", vGravity);
            DivergenceFreeSPHSloverCS.SetBool("UseVolumeMapBoundary", vUseVolumeMapBoundary);
            DivergenceFreeSPHSloverCS.SetBool("ComputeAnisotropyMatrix", vComputeAnisotropyMatrix);
            DivergenceFreeSPHSloverCS.SetInt("IterNum", (int)vIterNum);

            DivergenceFreeSPHSloverCS.SetFloat("MinCurvature", vMinCurvature);
            DivergenceFreeSPHSloverCS.SetFloat("MaxCurvature", vMaxCurvature);
            DivergenceFreeSPHSloverCS.SetFloat("MinRelativeVelLength", vMinRelativeVelLength);
            DivergenceFreeSPHSloverCS.SetFloat("MaxRelativeVelLength", vMaxRelativeVelLength);
            DivergenceFreeSPHSloverCS.SetInt("NumTaRate", vNumTaRate);
            DivergenceFreeSPHSloverCS.SetInt("NumWcRate", vNumWcRate);
            DivergenceFreeSPHSloverCS.SetFloat("MaxKe", Mathf.Pow(0.00001f, 2) * vParticleVolume * 1000.0f * 0.5f);
            DivergenceFreeSPHSloverCS.SetFloat("MinKe", Mathf.Pow(0.0f, 2) * vParticleVolume * 1000.0f * 0.5f);

            ///施加其它力
            Profiler.BeginSample("Update velocity with no pressure force");
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticlePosition_R", voTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleVelocity_RW", voTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "FoamParticleCount_RW", vTargetParticleFoamParticleCountCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "Density_R", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "Normal_R", vTargetParticleNormalCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "ParticleClosestPoint_R", vTargetParticleClosestPointCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "Volume_R", vTargetParticleVolumeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForceKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);
            DivergenceFreeSPHSloverCS.DispatchIndirect(updateVelocityWithNoPressureForceKernel, vTargetParticleIndirectArgment);
            Profiler.EndSample();

            ///预计算迭代不变值（密度与Alpha）
            Profiler.BeginSample("Compute alpha and density");
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "TargetParticlePosition_R", voTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Density_RW", vTargetParticleDensityCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Alpha_RW", vTargetParticleAlphaCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Normal_RW", vTargetParticleNormalCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "ParticleClosestPoint_R", vTargetParticleClosestPointCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "Volume_R", vTargetParticleVolumeCache);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "NarrowPositionBuffer_W", vNarrowPositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidPropertyKernel, "AnisotropyBuffer_W", vAnisotropyBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(computeFluidPropertyKernel, vTargetParticleIndirectArgment);
            Profiler.EndSample();

            ///无散迭代
            if(EnableDivergenceFreeSlover)
            {
                Profiler.BeginSample("Divergence-free iteration");
                for (int i = 0; i < vDivergenceFreeIterationCount; i++)
                {
                    Profiler.BeginSample("Compute density change");
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticlePosition_R", voTarget.ParticlePositionBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "TargetParticleVelocity_R", voTarget.ParticleVelocityBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "DensityChange_RW", vTargetParticleDensityChangeCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "ParticleClosestPoint_R", vTargetParticleClosestPointCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "Volume_R", vTargetParticleVolumeCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChangeKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);
                    DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityChangeKernel, vTargetParticleIndirectArgment);
                    Profiler.EndSample();

                    Profiler.BeginSample("Solve divergence iteration");
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticlePosition_R", voTarget.ParticlePositionBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "TargetParticleVelocity_RW", voTarget.ParticleVelocityBuffer);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Density_R", vTargetParticleDensityCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "DensityChange_R", vTargetParticleDensityChangeCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "ParticleClosestPoint_R", vTargetParticleClosestPointCache);
                    DivergenceFreeSPHSloverCS.SetBuffer(sloveDivergenceIterationKernel, "Volume_R", vTargetParticleVolumeCache);
                    DivergenceFreeSPHSloverCS.DispatchIndirect(sloveDivergenceIterationKernel, vTargetParticleIndirectArgment);
                    Profiler.EndSample();
                }
                Profiler.EndSample();
            }

            ///压力迭代
            Profiler.BeginSample("Pressure iteration");
            for (int i = 0; i < vPressureIterationCount; i++)
            {
                Profiler.BeginSample("Compute density adv");
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticlePosition_R", voTarget.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "TargetParticleVelocity_R", voTarget.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "Density_R", vTargetParticleDensityCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "DensityAdv_RW", vTargetParticleDensityAdvCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "ParticleClosestPoint_R", vTargetParticleClosestPointCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "Volume_R", vTargetParticleVolumeCache);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdvKernel, "ParticleBoundaryVelocity_R", vTargetParticleBoundaryVelocityBufferCache);
                DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityAdvKernel, vTargetParticleIndirectArgment);
                Profiler.EndSample();

                Profiler.BeginSample("Solve pressure iteration");
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticlePosition_R", voTarget.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleVelocity_RW", voTarget.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Density_R", vTargetParticleDensityCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Alpha_R", vTargetParticleAlphaCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "DensityAdv_R", vTargetParticleDensityAdvCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "ParticleClosestPoint_R", vTargetParticleClosestPointCache);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Volume_R", vTargetParticleVolumeCache);
                DivergenceFreeSPHSloverCS.DispatchIndirect(slovePressureIterationKernel, vTargetParticleIndirectArgment);
                Profiler.EndSample();
            }
            Profiler.EndSample();
        }

        public void GenerateFoam(
            ParticleBuffer vTarget,
            ParticleBuffer voFoamParticle,
            ComputeBuffer vTargetParticleIndirectArgment,
            ComputeBuffer vFoamParticleIndirectArgment,
            ComputeBuffer vTargetParticleFoamParticleCountCache,
            ComputeBuffer vTargetParticleFoamParticleOffsetCache, 
            float vSearchRadius, float vTimeStep, int vMaxFoamParticleCount)
        {
            DivergenceFreeSPHSloverCS.SetInt("MaxFoamParticleCount", vMaxFoamParticleCount);
            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHSloverCS.SetFloat("SearchRadius", vSearchRadius);
            Vector3 RandomSeed = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
            DivergenceFreeSPHSloverCS.SetFloats("TimeSeed", RandomSeed.x, RandomSeed.y, RandomSeed.z);

            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleIndirectArgment_R", vFoamParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleCount_R", vTargetParticleFoamParticleCountCache);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleOffset_R", vTargetParticleFoamParticleOffsetCache);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "TargetParticleVelocity_R", vTarget.ParticleVelocityBuffer);

            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticlePosition_RW", voFoamParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleVelocity_RW", voFoamParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleFilter_RW", voFoamParticle.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleLifeTime_RW", voFoamParticle.ParticleLifeTimeBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(generateFoamParticle, vTargetParticleIndirectArgment);

            DivergenceFreeSPHSloverCS.SetBuffer(updateFoamParticleCountArgument, "TargetParticleIndirectArgment_R", vTargetParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(updateFoamParticleCountArgument, "FoamParticleOffset_R", vTargetParticleFoamParticleOffsetCache);
            DivergenceFreeSPHSloverCS.SetBuffer(updateFoamParticleCountArgument, "FoamParticleIndirectArgment_RW", vFoamParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.Dispatch(updateFoamParticleCountArgument, 1, 1, 1);
        }

        public void AdvectFoam(
            ParticleBuffer vTarget,
            ParticleBuffer voFoamParticle,
            ComputeBuffer vHashGridCellParticleCount,
            ComputeBuffer vHashGridCellParticleOffset,
            ComputeBuffer vFoamParticleIndirectArgment,
            ComputeBuffer vTargetParticleNormalCache,
            Vector3 vHashGridMin, float HashGridCellLength, Vector3Int vHashGridResolution,
            float vTimeStep, float vGravity, float vSearchRadius, float vParticleVolume, uint vFoamUpdateRate)
        {
            DivergenceFreeSPHSloverCS.SetFloats("HashGridMin", vHashGridMin.x, vHashGridMin.y, vHashGridMin.z);
            DivergenceFreeSPHSloverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionX", vHashGridResolution.x);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionY", vHashGridResolution.y);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionZ", vHashGridResolution.z);

            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHSloverCS.SetFloat("vGravity", vGravity);
            DivergenceFreeSPHSloverCS.SetFloat("SearchRadius", vSearchRadius);
            DivergenceFreeSPHSloverCS.SetFloat("ParticleVolume", vParticleVolume);
            DivergenceFreeSPHSloverCS.SetFloat("FoamUpdateRate", vFoamUpdateRate);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "HashGridCellParticleCount_R", vHashGridCellParticleCount);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "HashGridCellParticleOffset_R", vHashGridCellParticleOffset);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleIndirectArgment_R", vFoamParticleIndirectArgment);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticlePosition_RW", voFoamParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleVelocity_RW", voFoamParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleLifeTime_RW", voFoamParticle.ParticleLifeTimeBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleFilter_RW", voFoamParticle.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "TargetParticlePosition_R", vTarget.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "TargetParticleVelocity_R", vTarget.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "Normal_R", vTargetParticleNormalCache);

            DivergenceFreeSPHSloverCS.DispatchIndirect(advectFoamParticle, vFoamParticleIndirectArgment);
        }
    }
}
