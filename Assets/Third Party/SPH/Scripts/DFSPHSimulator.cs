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

            if (Emit && Time.frameCount % 10 == 0)
            {
                DFSPH.AddParticleBlock(
                    WaterGeneratePosition,
                    WaterGenerateResolution,
                    WaterGenerateInitVelocity);
            }
            //if (Input.GetKeyDown(KeyCode.Space))
            //{
            //    DFSPH.AddParticleBlock(
            //        WaterGeneratePosition,
            //        WaterGenerateResolution,
            //        WaterGenerateInitVelocity);
            //}
            DFSPH.Solve(DivergenceIterationCount, PressureIterationCount, TimeStep, Viscosity, SurfaceTension, Gravity);
            DFSPH.Advect(TimeStep);
            SetupDataForReconstruction();
        }

        //private void FixedUpdate()
        //{
        //    DFSPH.Solve(DivergenceIterationCount, PressureIterationCount, TimeStep, Viscosity, SurfaceTension, Gravity);
        //    DFSPH.Advect(TimeStep);
        //    SetupDataForReconstruction();
        //}

        public Simulator2ReconstructionData m_ParticleData;
        public void SetupDataForReconstruction()
        {
            m_ParticleData.ArgumentBuffer = DFSPH.Dynamic3DParticleIndirectArgumentBuffer;

            m_ParticleData.NarrowPositionBuffer = DFSPH.Dynamic3DParticle.ParticlePositionBuffer;
            m_ParticleData.AnisotropyBuffer = null;

            m_ParticleData.MinPos = SimulationRangeMin;
            m_ParticleData.MaxPos = new Vector3(64, 32, 64);  //TODO:
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
    }
}
