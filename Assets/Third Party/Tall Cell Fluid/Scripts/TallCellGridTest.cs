using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TallCellGridTest : MonoBehaviour
{
    public Texture m_Terrian;
    public Vector3 m_Min;
    public Vector2Int m_ResolutionXZ;
    public int m_RegularCellCount;
    public float m_Celllength;
    public float m_SeaLevel;
    public int m_MaxParticleCount;
    public float m_TimeStep;

    private TallCellGrid m_TallCellGrid;

    void Start()
    {
        m_TallCellGrid = new TallCellGrid(m_Terrian, m_ResolutionXZ, m_RegularCellCount, m_Min, m_Celllength, m_SeaLevel, m_MaxParticleCount);
    }

    // Update is called once per frame
    void Update()
    {
        m_TallCellGrid.Step(m_TimeStep);
    }
}
