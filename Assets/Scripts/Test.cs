using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Test : MonoBehaviour
{
    public Vector3 m_Min = new Vector3(0,0,0);
    public float m_CellLength = 1.0f;
    public Vector3Int m_Resolution = new Vector3Int(128, 128, 128);

    private DParticle.DynamicParticle m_Particle;

    void Start()
    {
        m_Particle = new DParticle.DynamicParticle(250000, 0.25f);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
            m_Particle.AddParticleBlock(new Vector3(0, 0, 0), new Vector3Int(64, 64, 64), new Vector3(0, 10, 0));

        m_Particle.OrganizeParticle(m_Min, m_CellLength, m_Resolution);
        m_Particle.ZSort(m_Min, m_CellLength);
    }

    private void OnRenderObject()
    {
        m_Particle.VisualParticle();
    }
}
