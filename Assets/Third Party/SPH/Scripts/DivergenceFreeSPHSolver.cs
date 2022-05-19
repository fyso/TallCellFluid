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

        ~ParticleBuffer()
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

        public ParticleBuffer FoamParticle;
        private ParticleBuffer FoamParticleCache;
        private ComputeBuffer FoamParticleScatterOffsetCache;
        public ComputeBuffer FoamParticleDistanceBuffer;
        public ComputeBuffer HashGridCellFoamParticleCountBuffer;
        public ComputeBuffer HashGridCellFoamParticleOffsetBuffer;
        private ComputeBuffer FoamParticleCellIndexCache;
        private ComputeBuffer FoamParticleInnerSortIndexCache;

        public List<CubicMap> SignedDistance;
        public List<CubicMap> Volume;

        public ComputeBuffer Dynamic3DParticleIndirectArgumentBuffer;
        public ComputeBuffer Dynamic3DFoamParticleIndirectArgumentBuffer;

        public ComputeBuffer Dynamic3DParticleFoamParticleCountBuffer;
        public ComputeBuffer Dynamic3DParticleFoamParticleOffsetBuffer;

        public ComputeBuffer Dynamic3DParticleAlphaBuffer;
        public ComputeBuffer Dynamic3DParticleDensityBuffer;
        public ComputeBuffer Dynamic3DParticleDensityChangeBuffer;
        public ComputeBuffer Dynamic3DParticleDensityAdvBuffer;
        public ComputeBuffer Dynamic3DParticleNormalBuffer;
        public ComputeBuffer Dynamic3DParticleClosestPointBuffer;
        public ComputeBuffer Dynamic3DParticleVolumeBuffer;
        public ComputeBuffer Dynamic3DParticleBoundaryVelocityBuffer;

        private DynamicParticle DynamicParticleTool;
        private GPUCountingSortHash CompactNSearchTool;
        private DivergenceFreeSPH DivergenceFreeSPHTool;
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

            CompactNSearchTool = new GPUCountingSortHash(vMaxParticleCount);
            DynamicParticleTool = new DynamicParticle(vMaxParticleCount);
            DivergenceFreeSPHTool = new DivergenceFreeSPH(vMaxParticleCount);

            SignedDistance = new List<CubicMap>();
            Volume = new List<CubicMap>();

            int[] ParticleIndirectArgumentCPU = new int[7] { 1, 1, 1, 6, 0, 0, 0 };
            Dynamic3DParticleIndirectArgumentBuffer = new ComputeBuffer(7, sizeof(int), ComputeBufferType.IndirectArguments);
            Dynamic3DParticleIndirectArgumentBuffer.SetData(ParticleIndirectArgumentCPU);

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

            Dynamic3DParticleFoamParticleCountBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));
            Dynamic3DParticleFoamParticleOffsetBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(uint));

            Dynamic3DParticleDensityBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleAlphaBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleDensityChangeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleDensityAdvBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleNormalBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleClosestPointBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);
            Dynamic3DParticleVolumeBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float));
            Dynamic3DParticleBoundaryVelocityBuffer = new ComputeBuffer((int)vMaxParticleCount, sizeof(float) * 3);


            VolumeMapBoundaryTool = new VolumeMapBoundary();
            VolumeMapBoundaryTool.GenerateBoundaryMapData(
                vBoundaryObjects,
                Volume,
                SignedDistance,
                SearchRadius, CubicZero);
        }

        ~DivergenceFreeSPHSolver()
        {
            Dynamic3DParticleIndirectArgumentBuffer.Release();
            HashGridCellParticleCountBuffer.Release();
            HashGridCellParticleOffsetBuffer.Release();
            Dynamic3DParticleFoamParticleCountBuffer.Release();
            Dynamic3DParticleDensityBuffer.Release();
            Dynamic3DParticleAlphaBuffer.Release();
            Dynamic3DParticleDensityChangeBuffer.Release();
            Dynamic3DParticleDensityAdvBuffer.Release();
            Dynamic3DParticleNormalBuffer.Release();
            Dynamic3DParticleClosestPointBuffer.Release();
            Dynamic3DParticleDistanceBuffer.Release();
            Dynamic3DParticleVolumeBuffer.Release();
            Dynamic3DParticleBoundaryVelocityBuffer.Release();
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

        public void Solve(int DivergenceIterationCount, int PressureIterationCount, float vTimeStep, float vViscosity, float vSurfaceTension, float vGravity)
        {
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

            DivergenceFreeSPHTool.Slove(
                    ref Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer,
                    HashGridCellParticleCountBuffer,
                    HashGridCellParticleOffsetBuffer,
                    Dynamic3DParticleFoamParticleCountBuffer,
                    Dynamic3DParticleDensityBuffer,
                    Dynamic3DParticleAlphaBuffer,
                    Dynamic3DParticleDensityChangeBuffer,
                    Dynamic3DParticleDensityAdvBuffer,
                    Dynamic3DParticleNormalBuffer,
                    Dynamic3DParticleClosestPointBuffer,
                    Dynamic3DParticleVolumeBuffer,
                    Dynamic3DParticleBoundaryVelocityBuffer,
                    HashGridMin, HashGridCellLength, HashGridRes, SearchRadius, ParticleVolume,
                    vTimeStep, vViscosity, vSurfaceTension, vGravity,
                    DivergenceIterationCount, PressureIterationCount
                );

            CompactNSearchTool.GPUScanner.Scan(Dynamic3DParticleFoamParticleCountBuffer, Dynamic3DParticleFoamParticleOffsetBuffer);

            DivergenceFreeSPHTool.GenerateFoam(
                Dynamic3DParticle,
                FoamParticle,
                Dynamic3DParticleIndirectArgumentBuffer,
                Dynamic3DFoamParticleIndirectArgumentBuffer,
                Dynamic3DParticleFoamParticleCountBuffer,
                Dynamic3DParticleFoamParticleOffsetBuffer,
                SearchRadius, vTimeStep, FoamParticle.ParticlePositionBuffer.count);

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

            DivergenceFreeSPHTool.AdvectFoam(
                Dynamic3DParticle,
                FoamParticle,
                HashGridCellParticleCountBuffer,
                HashGridCellParticleOffsetBuffer,
                Dynamic3DFoamParticleIndirectArgumentBuffer,
                Dynamic3DParticleNormalBuffer,
                HashGridMin, HashGridCellLength, HashGridRes, vTimeStep, vGravity, SearchRadius, ParticleVolume, FoamUpdateRate);
        }

        public void Advect(float vTimeStep)
        {
            DivergenceFreeSPHTool.Advect(
                    ref Dynamic3DParticle,
                    Dynamic3DParticleIndirectArgumentBuffer,
                    vTimeStep);
        }
    }
}
