using UnityEngine;

public class SimulatorTest : MonoBehaviour
{
    public Texture m_Terrian;
    public Vector3 m_Min;
    public Vector2Int m_ResolutionXZ;//TODO: The number with exponent 2
    public int m_RegularCellYCount;//TODO: The number with exponent 2
    public float m_Celllength;
    public float m_SeaLevel;
    public int m_MaxParticleCount;
    public float m_TimeStep;

    public bool ShowDebugInfo = false;

    private Simulator m_Simulator;

    void Start()
    {
        m_Simulator = new Simulator(m_Terrian, m_ResolutionXZ, m_RegularCellYCount, m_Min, m_Celllength, m_SeaLevel, m_MaxParticleCount);
    }

    void Update()
    {
        m_Simulator.Step(m_TimeStep);
    }

    private void OnRenderObject()
    {
        m_Simulator.DynamicParticle.VisualParticle();
    }

    private void OnDrawGizmos()
    {
        if (m_Simulator != null && ShowDebugInfo)
        {
            GridPerLevel FineGrid = m_Simulator.FineGrid;
            Texture2D TerrianHeight = Common.CopyRenderTextureToCPU(FineGrid.TerrrianHeight);
            Texture2D TallCellHeight = Common.CopyRenderTextureToCPU(FineGrid.TallCellHeight);

            for (int i = 0; i < FineGrid.ResolutionXZ.x; i++)
            {
                for (int j = 0; j < FineGrid.ResolutionXZ.y; j++)
                {
                    float CurrTerrianHeight = TerrianHeight.GetPixel(i, j).r;
                    float CurrTallCellHeight = TallCellHeight.GetPixel(i, j).r;

                    Gizmos.color = new Color(0.0f, 0.0f, 1.0f);
                    Vector3 RegularCellCenter = m_Min + new Vector3(i * m_Celllength, CurrTallCellHeight + CurrTerrianHeight + 0.5f * m_Celllength, j * m_Celllength);
                    for (int k = 0; k < m_RegularCellYCount; k++)
                    {
                        Gizmos.DrawWireCube(RegularCellCenter, new Vector3(m_Celllength, m_Celllength, m_Celllength));
                        RegularCellCenter.y += m_Celllength;
                    }

                    Gizmos.color = new Color(1.0f, 0.0f, 0.0f);
                    Vector3 TallCellCenter = m_Min + new Vector3(i * m_Celllength, CurrTallCellHeight * 0.5f + CurrTerrianHeight, j * m_Celllength);
                    Gizmos.DrawWireCube(TallCellCenter, new Vector3(m_Celllength, CurrTallCellHeight, m_Celllength));

                    Gizmos.color = new Color(0.0f, 1.0f, 0.0f);
                    Vector3 TerrianCellCenter = m_Min + new Vector3(i * m_Celllength, CurrTerrianHeight * 0.5f, j * m_Celllength);
                    Gizmos.DrawWireCube(TerrianCellCenter, new Vector3(m_Celllength, CurrTerrianHeight, m_Celllength));
                }
            }
        }
    }
}
