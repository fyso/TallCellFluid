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
        m_Simulator.GenerateRandomVelicty();
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
            m_Simulator.DebugGridShape();
        }
    }
}
