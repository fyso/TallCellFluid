using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class ParticleBuffer
    {
        public ComputeBuffer ParticlePositionBuffer;
        public ComputeBuffer ParticleVelocityBuffer;
        public ComputeBuffer ParticleFilterBuffer;
        public ComputeBuffer ParticleLifeTimeBuffer;

        public ParticleBuffer(uint vParticleBufferSize)
        {
            ParticlePositionBuffer = new ComputeBuffer((int)vParticleBufferSize, 3 * sizeof(float));
            ParticleVelocityBuffer = new ComputeBuffer((int)vParticleBufferSize, 3 * sizeof(float));
            ParticleFilterBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(uint));
            ParticleLifeTimeBuffer = new ComputeBuffer((int)vParticleBufferSize, sizeof(float));
        }

        public void Release()
        {
            ParticlePositionBuffer.Release();
            ParticleVelocityBuffer.Release();
            ParticleFilterBuffer.Release();
            ParticleLifeTimeBuffer.Release();
        }
    }

    public class DivergenceFreeSPHSolver
    {
        public ParticleBuffer Dynamic3DParticle;
        private ParticleBuffer Dynamic3DParticleCache;
        private ComputeBuffer Dynamic3DParticleScatterOffsetCache;
        public ComputeBuffer Dynamic3DParticleDistanceBuffer;
        public ComputeBuffer HashGridCellParticleCountBuffer;
        public ComputeBuffer HashGridCellParticleOffsetBuffer;
        private ComputeBuffer ParticleCellIndexCache;
        private ComputeBuffer ParticleInnerSortIndexCache;
        public ComputeBuffer Dynamic3DParticleIndirectArgumentBuffer;

        public ParticleBuffer FoamParticle;
        private ParticleBuffer FoamParticleCache;
        private ComputeBuffer FoamParticleScatterOffsetCache;
        public ComputeBuffer FoamParticleDistanceBuffer;
        public ComputeBuffer HashGridCellFoamParticleCountBuffer;
        public ComputeBuffer HashGridCellFoamParticleOffsetBuffer;
        private ComputeBuffer FoamParticleCellIndexCache;
        private ComputeBuffer FoamParticleInnerSortIndexCache;
        public ComputeBuffer Dynamic3DFoamParticleIndirectArgumentBuffer;

        public List<CubicMap> SignedDistance;
        public List<CubicMap> Volume;

        public ComputeBuffer NarrowParticleIndirectArgumentBuffer;

        public ComputeBuffer Dynamic3DParticleAlphaBuffer;
        public ComputeBuffer Dynamic3DParticleDensityBuffer;
        public ComputeBuffer Dynamic3DParticleDensityChangeBuffer;
        public ComputeBuffer Dynamic3DParticleDensityAdvBuffer;
        public ComputeBuffer Dynamic3DParticleNormalBuffer;
        public ComputeBuffer Dynamic3DParticleClosestPointBuffer;
        public ComputeBuffer Dynamic3DParticleVolumeBuffer;
        public ComputeBuffer Dynamic3DParticleBoundaryVelocityBuffer;
        public ComputeBuffer Dynamic3DParticleFoamParticleCountBuffer;
        public ComputeBuffer Dynamic3DParticleFoamParticleOffsetBuffer;
        public ComputeBuffer Dynamic3DParticleParticleVdiffBuffer;
        public ComputeBuffer Dynamic3DParticleParticleCurvatureBuffer;
        public ComputeBuffer Dynamic3DParticleParticleKeBuffer;

        public ComputeBuffer NarrowPositionBuffer;
        public ComputeBuffer AnisotropyBuffer;
        public ComputeBuffer ReductionBuffer;

        private DynamicParticle DynamicParticleTool;
        private GPUCountingSortHash CompactNSearchTool;
        private VolumeMapBoundary VolumeMapBoundaryTool;
        private List<GameObject> BoundaryObjects;

        public Vector3 SimulationRangeMin;
        public Vector3Int SimulationRangeRes;
        public float ParticleRadius;

        public Vector3 HashGridMin { get { return SimulationRangeMin; } }
        public Vector3Int HashGridRes { get { return SimulationRangeRes; } }
        public float HashGridCellLength { get { return ParticleRadius * 4.0f; } }
        public float SearchRadius { get { return ParticleRadius * 4.0f; } }
        public float ParticleVolume { get { return 0.8f * Mathf.Pow(2.0f * ParticleRadius, 3.0f); } }
        public float CubicZero { get { return 8.0f / (Mathf.PI * Mathf.Pow(SearchRadius, 3.0f)); } }

        private uint FoamUpdateRate = 1;

        public DivergenceFreeSPHSolver(
            List<GameObject> vBoundaryObjects, 
            uint vMaxParticleCount,
            uint vMaxFoamParticleCount,
            Vector3 vSimulationRangeMin,
            Vector3Int vSimulationRangeRes,
            float vParticleRadius)
        {
            BoundaryObjects = vBoundaryObjects;
            ParticleRadius = vParticleRadius;
            SimulationRangeMin = vSimulationRangeMin;
            SimulationRangeRes = vSimulationRangeRes;

            SignedDistance = new List<CubicMap>();
            Volume = new List<CubicMap>();

            int[] ParticleIndirectArgumentCPU = new int[7] { 1, 1, 1, 6, 0, 0, 0 };
            Dynamic3DParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            NarrowParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            Dynamic3DParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);
            NarrowParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);

            Dynamic3DFoamParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            Dynamic3DFoamParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);

            Dynamic3DParticle = new ParticleBuffer(vMaxParticleCount);
            Dynamic3DParticleCache = new ParticleBuffer(vMaxParticleCount);
            Dynamic3DParticleScatterOffsetCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            Dynamic3DParticleDistanceBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            HashGridCellParticleCountBuffer = new ComputeBuffer((int)vMaxParticleCount * 2, sizeof(uint));
            HashGridCellParticleOffsetBuffer = new ComputeBuffer((int)vMaxParticleCount * 2, sizeof(uint));
            ParticleCellIndexCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            ParticleInnerSortIndexCache = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));

            FoamParticle = new ParticleBuffer(vMaxFoamParticleCount);
            FoamParticleCache = new ParticleBuffer(vMaxFoamParticleCount);
            FoamParticleScatterOffsetCache = new ComputeBuffer((int)vMaxFoamParticleCount, sizeof(uint));
            FoamParticleDistanceBuffer = new ComputeBuffer((int)vMaxFoamParticleCount, sizeof(float));
            float[] MaxData = new float[vMaxFoamParticleCount];
            for (int i = 0; i < vMaxFoamParticleCount; i++)
                MaxData[i] = float.MaxValue;
            FoamParticleDistanceBuffer.SetData(MaxData);
            HashGridCellFoamParticleCountBuffer = new ComputeBuffer((int)vMaxFoamParticleCount * 2, sizeof(uint));
            HashGridCellFoamParticleOffsetBuffer = new ComputeBuffer((int)vMaxFoamParticleCount * 2, sizeof(uint));
            FoamParticleCellIndexCache = new ComputeBuffer((int)vMaxFoamParticleCount, sizeof(uint));
            FoamParticleInnerSortIndexCache = new ComputeBuffer((int)vMaxFoamParticleCount, sizeof(uint));

            Dynamic3DParticleDensityBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleAlphaBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleDensityChangeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleDensityAdvBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleNormalBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleClosestPointBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleVolumeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleBoundaryVelocityBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleFoamParticleCountBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            Dynamic3DParticleFoamParticleOffsetBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            Dynamic3DParticleParticleVdiffBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleParticleCurvatureBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleParticleKeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));

            NarrowPositionBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            AnisotropyBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint) * 2);

            int reductionCount = Mathf.CeilToInt((float)vMaxParticleCount / 1024);
            ReductionBuffer = new ComputeBuffer(reductionCount, sizeof(float) * 3);

            VolumeMapBoundaryTool = new VolumeMapBoundary();
            CompactNSearchTool = new GPUCountingSortHash();
            DynamicParticleTool = new DynamicParticle(vMaxParticleCount);

            VolumeMapBoundaryTool.GenerateBoundaryMapData(
                vBoundaryObjects,
                Volume,
                SignedDistance,
                SearchRadius, CubicZero);

            __InitFluidTools();
        }

        public void Release()
        {
            Dynamic3DParticle.Release();
            Dynamic3DParticleCache.Release();
            Dynamic3DParticleScatterOffsetCache.Release();
            Dynamic3DParticleDistanceBuffer.Release();
            HashGridCellParticleCountBuffer.Release();
            HashGridCellParticleOffsetBuffer.Release();
            ParticleCellIndexCache.Release();
            ParticleInnerSortIndexCache.Release();
            Dynamic3DParticleIndirectArgumentBuffer.Release();
            NarrowParticleIndirectArgumentBuffer.Release();

            FoamParticle.Release();
            FoamParticleCache.Release();
            FoamParticleScatterOffsetCache.Release();
            FoamParticleDistanceBuffer.Release();
            HashGridCellFoamParticleCountBuffer.Release();
            HashGridCellFoamParticleOffsetBuffer.Release();
            FoamParticleCellIndexCache.Release();
            FoamParticleInnerSortIndexCache.Release();
            Dynamic3DFoamParticleIndirectArgumentBuffer.Release();

            Dynamic3DParticleAlphaBuffer.Release();
            Dynamic3DParticleDensityBuffer.Release();
            Dynamic3DParticleDensityChangeBuffer.Release();
            Dynamic3DParticleDensityAdvBuffer.Release();
            Dynamic3DParticleNormalBuffer.Release();
            Dynamic3DParticleClosestPointBuffer.Release();
            Dynamic3DParticleVolumeBuffer.Release();
            Dynamic3DParticleBoundaryVelocityBuffer.Release();
            Dynamic3DParticleFoamParticleCountBuffer.Release();
            Dynamic3DParticleFoamParticleOffsetBuffer.Release();
            Dynamic3DParticleParticleVdiffBuffer.Release();
            Dynamic3DParticleParticleCurvatureBuffer.Release();
            Dynamic3DParticleParticleKeBuffer.Release();

            NarrowPositionBuffer.Release();
            AnisotropyBuffer.Release();

            foreach (CubicMap Map in Volume)
                Map.Release();

            foreach (CubicMap Map in SignedDistance)
                Map.Release();

            Dynamic3DParticle.Release();
            Dynamic3DParticleCache.Release();
            ReductionBuffer.Release();
            FoamParticle.Release();
            FoamParticleCache.Release();

            DynamicParticleTool.Relaese();
            CompactNSearchTool.Release();
            VolumeMapBoundaryTool.Release();
        }

        public void AddParticleBlock(Vector3 WaterGeneratePosition, Vector3Int WaterGenerateResolution, Vector3 WaterGenerateInitVelocity)
        {
            DynamicParticleTool.AddParticleBlock(
                Dynamic3DParticle,
                Dynamic3DParticleIndirectArgumentBuffer,
                WaterGeneratePosition,
                WaterGenerateResolution,
                WaterGenerateInitVelocity,
                ParticleRadius);
        }

        private ComputeShader DivergenceFreeSPHSloverCS;
        private int computeFluidProperty;
        private int computeDensityChange;
        private int solveDivergenceIteration;
        private int computeDensityAdv;
        private int slovePressureIterationKernel;
        private int updateVelocityWithNoPressureForce;
        private int computeFoamParticleCountPerWaterParticle;
        private int advectWaterParticle;
        private int generateFoamParticle;
        private int updateFoamParticleCountArgument;
        private int advectFoamParticle;

        private void __InitFluidTools()
        {
            DivergenceFreeSPHSloverCS = Resources.Load<ComputeShader>("Shaders/Solver/DivergenceFreeSPHSolver");
            computeFluidProperty = DivergenceFreeSPHSloverCS.FindKernel("computeFluidProperty");
            computeDensityChange = DivergenceFreeSPHSloverCS.FindKernel("computeDensityChange");
            solveDivergenceIteration = DivergenceFreeSPHSloverCS.FindKernel("solveDivergenceIteration");
            computeDensityAdv = DivergenceFreeSPHSloverCS.FindKernel("computeDensityAdv");
            slovePressureIterationKernel = DivergenceFreeSPHSloverCS.FindKernel("solvePressureIteration");
            updateVelocityWithNoPressureForce = DivergenceFreeSPHSloverCS.FindKernel("updateVelocityWithNoPressureForce");
            computeFoamParticleCountPerWaterParticle = DivergenceFreeSPHSloverCS.FindKernel("computeFoamParticleCountPerWaterParticle");
            advectWaterParticle = DivergenceFreeSPHSloverCS.FindKernel("advectWaterParticle");
            generateFoamParticle = DivergenceFreeSPHSloverCS.FindKernel("generateFoamParticle");
            updateFoamParticleCountArgument = DivergenceFreeSPHSloverCS.FindKernel("updateFoamParticleCountArgument");
            advectFoamParticle = DivergenceFreeSPHSloverCS.FindKernel("advectFoamParticle");
        }

        public void Step(
            float vTimeStep, float vViscosity, float vSurfaceTension, float vGravity, 
            bool vComputeAnisotropyMatrix, uint vIterNum,
            int vDivergenceFreeIterationCount = 3, int vPressureIterationCount = 2, float vFoamScale = 1.0f,
            float vMinCurvature = 0.0f, float vMaxCurvature = 15.0f, float vMinRelativeVelLength = 0.0f, float vMaxRelativeVelLength = 15.0f, int vNumTaRate = 10, int vNumWcRate = 100)
        {
            DivergenceFreeSPHSloverCS.SetFloats("HashGridMin", HashGridMin.x, HashGridMin.y, HashGridMin.z);
            DivergenceFreeSPHSloverCS.SetFloat("HashGridCellLength", HashGridCellLength);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionX", HashGridRes.x);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionY", HashGridRes.y);
            DivergenceFreeSPHSloverCS.SetInt("HashGridResolutionZ", HashGridRes.z);
            DivergenceFreeSPHSloverCS.SetFloat("SearchRadius", SearchRadius);
            DivergenceFreeSPHSloverCS.SetFloat("ParticleVolume", ParticleVolume);
            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHSloverCS.SetFloat("Viscosity", vViscosity);
            DivergenceFreeSPHSloverCS.SetFloat("SurfaceTension", vSurfaceTension);
            DivergenceFreeSPHSloverCS.SetFloat("Gravity", vGravity);
            DivergenceFreeSPHSloverCS.SetBool("ComputeAnisotropyMatrix", vComputeAnisotropyMatrix);
            DivergenceFreeSPHSloverCS.SetInt("IterNum", (int)vIterNum);

            DivergenceFreeSPHSloverCS.SetFloat("MinCurvature", vMinCurvature);
            DivergenceFreeSPHSloverCS.SetFloat("MaxCurvature", vMaxCurvature);
            DivergenceFreeSPHSloverCS.SetFloat("MinRelativeVelLength", vMinRelativeVelLength);
            DivergenceFreeSPHSloverCS.SetFloat("MaxRelativeVelLength", vMaxRelativeVelLength);
            DivergenceFreeSPHSloverCS.SetInt("NumTaRate", vNumTaRate);
            DivergenceFreeSPHSloverCS.SetInt("NumWcRate", vNumWcRate);
            DivergenceFreeSPHSloverCS.SetFloat("MaxKe", Mathf.Pow(0.00001f, 2) * ParticleVolume * 1000.0f * 0.5f);
            DivergenceFreeSPHSloverCS.SetFloat("MinKe", Mathf.Pow(0.0f, 2) * ParticleVolume * 1000.0f * 0.5f);

            DynamicParticleTool.DeleteParticleOutofRange(
                    Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer,
                    Dynamic3DParticleDistanceBuffer,
                    HashGridMin,
                    HashGridCellLength,
                    HashGridRes,
                    vTimeStep);

            DynamicParticleTool.NarrowParticleData(
                    ref Dynamic3DParticle,
                    ref Dynamic3DParticleCache,
                    Dynamic3DParticleScatterOffsetCache,
                    Dynamic3DParticleIndirectArgumentBuffer);

            CompactNSearchTool.CountingHashSort(
                ref Dynamic3DParticle,
                ref Dynamic3DParticleCache,
                HashGridCellParticleCountBuffer,
                HashGridCellParticleOffsetBuffer,
                Dynamic3DParticleIndirectArgumentBuffer,
                ParticleCellIndexCache,
                ParticleInnerSortIndexCache,
                HashGridMin,
                HashGridCellLength);

            VolumeMapBoundaryTool.QueryClosestPointAndVolume(
                    Dynamic3DParticleIndirectArgumentBuffer,
                    Dynamic3DParticle,
                    BoundaryObjects,
                    Volume,
                    SignedDistance,
                    Dynamic3DParticleClosestPointBuffer,
                    Dynamic3DParticleDistanceBuffer,
                    Dynamic3DParticleVolumeBuffer,
                    Dynamic3DParticleBoundaryVelocityBuffer,
                    SearchRadius,
                    ParticleRadius);

            ///施加其它力
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "Density_R", Dynamic3DParticleDensityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "Normal_R", Dynamic3DParticleNormalBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "ParticleClosestPoint_R", Dynamic3DParticleClosestPointBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "Volume_R", Dynamic3DParticleVolumeBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "ParticleBoundaryVelocity_R", Dynamic3DParticleBoundaryVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "TargetParticleVelocity_RW", Dynamic3DParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "ParticleVelDiff_RW", Dynamic3DParticleParticleVdiffBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "ParticleCurvature_RW", Dynamic3DParticleParticleCurvatureBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateVelocityWithNoPressureForce, "ParticleKe_RW", Dynamic3DParticleParticleKeBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(updateVelocityWithNoPressureForce, Dynamic3DParticleIndirectArgumentBuffer);

            ///预计算迭代不变值（密度与Alpha）
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "NarrowParticleIndirectArgment_R", NarrowParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "Density_RW", Dynamic3DParticleDensityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "Alpha_RW", Dynamic3DParticleAlphaBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "Normal_RW", Dynamic3DParticleNormalBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "ParticleClosestPoint_R", Dynamic3DParticleClosestPointBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "Volume_R", Dynamic3DParticleVolumeBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "NarrowPositionBuffer_W", NarrowPositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFluidProperty, "AnisotropyBuffer_W", AnisotropyBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(computeFluidProperty, Dynamic3DParticleIndirectArgumentBuffer);

            ///无散迭代
            for (int i = 0; i < vDivergenceFreeIterationCount; i++)
            {
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "TargetParticleVelocity_R", Dynamic3DParticle.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "DensityChange_RW", Dynamic3DParticleDensityChangeBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "ParticleClosestPoint_R", Dynamic3DParticleClosestPointBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "Volume_R", Dynamic3DParticleVolumeBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityChange, "ParticleBoundaryVelocity_R", Dynamic3DParticleBoundaryVelocityBuffer);
                DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityChange, Dynamic3DParticleIndirectArgumentBuffer);

                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "TargetParticleVelocity_RW", Dynamic3DParticle.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "Density_R", Dynamic3DParticleDensityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "Alpha_R", Dynamic3DParticleAlphaBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "DensityChange_R", Dynamic3DParticleDensityChangeBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "ParticleClosestPoint_R", Dynamic3DParticleClosestPointBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(solveDivergenceIteration, "Volume_R", Dynamic3DParticleVolumeBuffer);
                DivergenceFreeSPHSloverCS.DispatchIndirect(solveDivergenceIteration, Dynamic3DParticleIndirectArgumentBuffer);
            }

            ///压力迭代
            for (int i = 0; i < vPressureIterationCount; i++)
            {
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "TargetParticleVelocity_R", Dynamic3DParticle.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "Density_R", Dynamic3DParticleDensityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "DensityAdv_RW", Dynamic3DParticleDensityAdvBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "ParticleClosestPoint_R", Dynamic3DParticleClosestPointBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "Volume_R", Dynamic3DParticleVolumeBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(computeDensityAdv, "ParticleBoundaryVelocity_R", Dynamic3DParticleBoundaryVelocityBuffer);
                DivergenceFreeSPHSloverCS.DispatchIndirect(computeDensityAdv, Dynamic3DParticleIndirectArgumentBuffer);

                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "TargetParticleVelocity_RW", Dynamic3DParticle.ParticleVelocityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Density_R", Dynamic3DParticleDensityBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Alpha_R", Dynamic3DParticleAlphaBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "DensityAdv_R", Dynamic3DParticleDensityAdvBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "ParticleClosestPoint_R", Dynamic3DParticleClosestPointBuffer);
                DivergenceFreeSPHSloverCS.SetBuffer(slovePressureIterationKernel, "Volume_R", Dynamic3DParticleVolumeBuffer);
                DivergenceFreeSPHSloverCS.DispatchIndirect(slovePressureIterationKernel, Dynamic3DParticleIndirectArgumentBuffer);
            }

            DivergenceFreeSPHSloverCS.SetBuffer(computeFoamParticleCountPerWaterParticle, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFoamParticleCountPerWaterParticle, "ParticleVelDiff_R", Dynamic3DParticleParticleVdiffBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFoamParticleCountPerWaterParticle, "ParticleCurvature_R", Dynamic3DParticleParticleCurvatureBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFoamParticleCountPerWaterParticle, "ParticleKe_R", Dynamic3DParticleParticleKeBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(computeFoamParticleCountPerWaterParticle, "FoamParticleCount_RW", Dynamic3DParticleFoamParticleCountBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(computeFoamParticleCountPerWaterParticle, Dynamic3DParticleIndirectArgumentBuffer);

            CompactNSearchTool.GPUScanner.Scan(Dynamic3DParticleFoamParticleCountBuffer, Dynamic3DParticleFoamParticleOffsetBuffer);

            DivergenceFreeSPHSloverCS.SetInt("MaxFoamParticleCount", FoamParticle.ParticlePositionBuffer.count);
            DivergenceFreeSPHSloverCS.SetFloat("TimeStep", vTimeStep);
            DivergenceFreeSPHSloverCS.SetFloat("SearchRadius", SearchRadius);
            DivergenceFreeSPHSloverCS.SetFloat("FoamScale", vFoamScale);
            Vector3 RandomSeed = new Vector3(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f));
            DivergenceFreeSPHSloverCS.SetFloats("TimeSeed", RandomSeed.x, RandomSeed.y, RandomSeed.z);

            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleCount_R", Dynamic3DParticleFoamParticleCountBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleOffset_R", Dynamic3DParticleFoamParticleOffsetBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "TargetParticleVelocity_R", Dynamic3DParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleIndirectArgment_R", Dynamic3DFoamParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticlePosition_RW", FoamParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleVelocity_RW", FoamParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleFilter_RW", FoamParticle.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(generateFoamParticle, "FoamParticleLifeTime_RW", FoamParticle.ParticleLifeTimeBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(generateFoamParticle, Dynamic3DParticleIndirectArgumentBuffer);

            DivergenceFreeSPHSloverCS.SetBuffer(updateFoamParticleCountArgument, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateFoamParticleCountArgument, "FoamParticleOffset_R", Dynamic3DParticleFoamParticleOffsetBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(updateFoamParticleCountArgument, "FoamParticleIndirectArgment_RW", Dynamic3DFoamParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.Dispatch(updateFoamParticleCountArgument, 1, 1, 1);

            DynamicParticleTool.NarrowParticleData(
                    ref FoamParticle,
                    ref FoamParticleCache,
                    FoamParticleScatterOffsetCache,
                    Dynamic3DFoamParticleIndirectArgumentBuffer);

            CompactNSearchTool.CountingHashSort(
                ref FoamParticle,
                ref FoamParticleCache,
                HashGridCellFoamParticleCountBuffer,
                HashGridCellFoamParticleOffsetBuffer,
                Dynamic3DFoamParticleIndirectArgumentBuffer,
                FoamParticleCellIndexCache,
                FoamParticleInnerSortIndexCache,
                HashGridMin,
                HashGridCellLength);

            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "HashGridCellParticleCount_R", HashGridCellParticleCountBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "HashGridCellParticleOffset_R", HashGridCellParticleOffsetBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleIndirectArgment_R", Dynamic3DFoamParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticlePosition_RW", FoamParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleVelocity_RW", FoamParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleLifeTime_RW", FoamParticle.ParticleLifeTimeBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "FoamParticleFilter_RW", FoamParticle.ParticleFilterBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "TargetParticlePosition_R", Dynamic3DParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "TargetParticleVelocity_R", Dynamic3DParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectFoamParticle, "Normal_R", Dynamic3DParticleNormalBuffer);

            DivergenceFreeSPHSloverCS.DispatchIndirect(advectFoamParticle, Dynamic3DFoamParticleIndirectArgumentBuffer);

            ///更新位置并Swap ParticleBuffer
            DivergenceFreeSPHSloverCS.SetBuffer(advectWaterParticle, "TargetParticleIndirectArgment_R", Dynamic3DParticleIndirectArgumentBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectWaterParticle, "TargetParticlePosition_RW", Dynamic3DParticle.ParticlePositionBuffer);
            DivergenceFreeSPHSloverCS.SetBuffer(advectWaterParticle, "TargetParticleVelocity_R", Dynamic3DParticle.ParticleVelocityBuffer);
            DivergenceFreeSPHSloverCS.DispatchIndirect(advectWaterParticle, Dynamic3DParticleIndirectArgumentBuffer);
        }

        public void SetUpBoundingBox(ComputeBuffer vNarrowPosBuffer, int vParticleCount, ref SBoundingBox voBoundingBox)
        {
            DivergenceFreeSPHTool.SetUpBoundingBox(vNarrowPosBuffer, ReductionBuffer, vParticleCount, ref voBoundingBox);
        }
    }
}
