using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;

namespace LODFluid
{
    public class DFSPHSimulator : MonoBehaviour
    {
        public Vector3 SimulationRangeMin = new Vector3(0, 0, 0);
        public Vector3Int SimulationRangeRes = new Vector3Int(64, 64, 64);
        public Vector3 WaterGeneratePosition = new Vector3(0, 0, 0);
        public Vector3Int WaterGenerateResolution = new Vector3Int(8, 1, 8);
        public Vector3 WaterGenerateInitVelocity = new Vector3(0, 0, 0);
        public Material WaterVisualMaterial;
        public Material FoamVisualMaterial;
        public List<GameObject> BoundaryObjects;

        [Range(0.025f, 0.25f)]
        public float ParticleRadius = 0.25f;

        [Range(0.005f, 0.05f)]
        public float TimeStep = 0.05f;

        [Range(0, 0.03f)]
        public float Viscosity = 0.03f;

        [Range(0, 0.1f)]
        public float SurfaceTension = 0.1f;

        [Range(0, 10f)]
        public float Gravity = 9.8f;

        [Range(100000, 300000)]
        public uint MaxParticleCount = 250000;

        [Range(1000000, 10000000)]
        public uint MaxFoamParticleCount = 1000000;

        public int DivergenceIterationCount = 3;
        public int PressureIterationCount = 1;

        public bool ComputeAnisotropyMatrix = true;
        [Range(0, 10)]
        public uint IterNum = 3;

        private DivergenceFreeSPHSolver DFSPH;

        private void OnDrawGizmos()
        {
            Vector3 SimulationMin = SimulationRangeMin;
            Vector3 SimulationMax = SimulationRangeMin + (Vector3)SimulationRangeRes * ParticleRadius * 4.0f;
            Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
            Gizmos.DrawWireCube((SimulationMin + SimulationMax) * 0.5f, SimulationMax - SimulationMin);

            Vector3 WaterGenerateBlockMax = WaterGeneratePosition + new Vector3(WaterGenerateResolution.x * ParticleRadius * 2.0f, WaterGenerateResolution.y * ParticleRadius * 2.0f, WaterGenerateResolution.z * ParticleRadius * 2.0f);
            Gizmos.color = new Color(1.0f, 1.0f, 0.0f);
            Gizmos.DrawWireCube((WaterGeneratePosition + WaterGenerateBlockMax) * 0.5f, WaterGenerateBlockMax - WaterGeneratePosition);
        }

        void Start()
        {
            DFSPH = new DivergenceFreeSPHSolver(BoundaryObjects, MaxParticleCount, MaxFoamParticleCount, SimulationRangeMin, SimulationRangeRes, ParticleRadius);
        }

        private bool Emit = true;
        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                Emit = !Emit;
        }

        private void FixedUpdate()
        {
            if (Emit)
            {
                DFSPH.AddParticleBlock(
                    WaterGeneratePosition,
                    WaterGenerateResolution,
                    WaterGenerateInitVelocity);
            }

            DFSPH.Step(TimeStep, Viscosity, SurfaceTension, Gravity, ComputeAnisotropyMatrix, IterNum, DivergenceIterationCount, PressureIterationCount);
            SetupDataForReconstruction();
        }

        public Simulator2ReconstructionData ParticleData;
        public void SetupDataForReconstruction()
        {
            ParticleData.FoamArgumentBuffer = DFSPH.Dynamic3DFoamParticleIndirectArgumentBuffer;
            ParticleData.FoamPositionBuffer = DFSPH.FoamParticle.ParticlePositionBuffer;
            ParticleData.FoamVelocityBuffer = DFSPH.FoamParticle.ParticleVelocityBuffer;
            ParticleData.FoamLifeTimeBuffer = DFSPH.FoamParticle.ParticleLifeTimeBuffer;
            if (ComputeAnisotropyMatrix)
            {
                ParticleData.PositionBuffer = DFSPH.NarrowPositionBuffer;
                ParticleData.ParticleArgumentBuffer = DFSPH.NarrowParticleIndirectArgumentBuffer;
                ParticleData.AnisotropyBuffer = DFSPH.AnisotropyBuffer;
            }
            else
            {
                ParticleData.PositionBuffer = DFSPH.Dynamic3DParticle.ParticlePositionBuffer;
                ParticleData.ParticleArgumentBuffer = DFSPH.Dynamic3DParticleIndirectArgumentBuffer;
                ParticleData.AnisotropyBuffer = null;
            }

            int[] particleCount = new int[5] { 0, 0, 0, 0, 0 };
            ParticleData.ParticleArgumentBuffer.GetData(particleCount, 0, 0, 5);
            SBoundingBox boundingBox = new SBoundingBox { };
            DFSPH.SetUpBoundingBox(ParticleData.PositionBuffer, particleCount[4], ref boundingBox);
            ParticleData.MinPos = boundingBox.MinPos;
            ParticleData.MaxPos = boundingBox.MaxPos;
        }

        void OnRenderObject()
        {
            WaterVisualMaterial.SetPass(0);
            WaterVisualMaterial.SetBuffer("_particlePositionBuffer", DFSPH.Dynamic3DParticle.ParticlePositionBuffer);
            WaterVisualMaterial.SetBuffer("_particleVelocityBuffer", DFSPH.Dynamic3DParticle.ParticleVelocityBuffer);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, DFSPH.Dynamic3DParticleIndirectArgumentBuffer, 12);

            FoamVisualMaterial.SetPass(0);
            FoamVisualMaterial.SetBuffer("_particlePositionBuffer", DFSPH.FoamParticle.ParticlePositionBuffer);
            FoamVisualMaterial.SetBuffer("_particleVelocityBuffer", DFSPH.FoamParticle.ParticleVelocityBuffer);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, DFSPH.Dynamic3DFoamParticleIndirectArgumentBuffer, 12);
        }

        private void OnDestroy()
        {
            DFSPH.Release();
        }
    }
}
