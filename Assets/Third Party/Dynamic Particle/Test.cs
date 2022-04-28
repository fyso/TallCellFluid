using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace DParticle
{
    public class Test : MonoBehaviour
    {
        public Vector3 m_Min = new Vector3(0, 0, 0);
        public float m_CellLength = 1.0f;
        public Vector3Int m_Resolution = new Vector3Int(128, 128, 128);

        private DynamicParticle m_Particle;

        public Material m_Material;

        void Start()
        {
            m_Particle = new DynamicParticle(250000, 0.25f);
            m_Particle.AddParticleBlock(new Vector3(0, 0, 0), new Vector3Int(64, 64, 64), new Vector3(0, 10, 0));
        }

        void Update()
        {
            if (Input.GetKeyDown(KeyCode.Space))
                m_Particle.AddParticleBlock(new Vector3(0, 0, 0), new Vector3Int(64, 64, 64), new Vector3(0, 10, 0));

            m_Particle.DeleteParticleOutofRange(m_Min, m_Min + (Vector3)m_Resolution * m_CellLength, m_CellLength);
            m_Particle.OrganizeParticle();
        }

        private void OnRenderObject()
        {
            m_Material.SetPass(0);
            m_Material.SetBuffer("_particlePositionBuffer", m_Particle.MainParticle.Position);
            m_Material.SetBuffer("_particleVelocityBuffer", m_Particle.MainParticle.Velocity);
            m_Material.SetBuffer("_particleFilterBuffer", m_Particle.MainParticle.Filter);
            Graphics.DrawProceduralIndirectNow(MeshTopology.Triangles, m_Particle.Argument, 12);
        }
    }
}
