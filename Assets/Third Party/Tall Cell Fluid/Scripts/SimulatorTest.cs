using UnityEngine;

[System.Serializable]
public struct VisualGridInfo
{
    public Mesh mesh;
}

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

    public bool VisualParticle = false;
    public bool ShowGridDebugInfo = false;
    public VisualGridInfo VisualGridInfo;

    private Simulator m_Simulator;

    void Start()
    {
        m_Simulator = new Simulator(
            m_Terrian, 
            m_ResolutionXZ, 
            m_RegularCellYCount, 
            m_Min,
            m_Celllength, 
            m_SeaLevel, 
            m_MaxParticleCount
        );
        m_Simulator.GenerateRandomVelicty();
    }

    void Update()
    {
        m_Simulator.Step(m_TimeStep);
    }

    private void OnRenderObject()
    {
        if(m_Simulator != null && VisualParticle)
            m_Simulator.VisualParticle();

        if (m_Simulator != null && ShowGridDebugInfo)
            m_Simulator.VisualGrid(VisualGridInfo);
    }
}
