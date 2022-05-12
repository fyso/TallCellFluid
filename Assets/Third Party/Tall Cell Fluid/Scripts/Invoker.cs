using UnityEngine;

[System.Serializable]
public enum ShowMode
{
    Entity = 0,
    Wireframe = 1
}

[System.Serializable]
public enum ShowInfo
{
    WaterMark = 0, // per level
    RigidBodyPercentage = 1, // per level
    RigidBodyVelocity = 2,
    RigidBodySpeed = 3,
    Velocity = 4,
    Speed = 5,
    Pressure = 6
}

[System.Serializable]
public struct VisualGridInfo
{
    public ShowMode m_ShowMode;
    public ShowInfo m_ShowInfo;
    public float MinShowValue;
    public Color MinShowColor;
    public float MaxShowValue;
    public Color MaxShowColor;
    public int m_GridLevel;
    public bool m_ShowRegularCell;
    public bool m_ShowTallCell;
    public bool m_ShowTerrainCell;
    public bool m_UseSpecifiedShowRange;
    public int m_MinX;
    public int m_MaxX;
    public int m_MinZ;
    public int m_MaxZ;
}

public class Invoker : MonoBehaviour
{
    public Texture m_Terrian;
    public Vector3 m_Min;
    public Vector2Int m_ResolutionXZ;//TODO: The number with exponent 2
    public int m_RegularCellYCount;//TODO: The number with exponent 2
    public float m_CellLength;
    public float m_SeaLevel;
    public int m_MaxParticleCount;
    public float m_TimeStep;

    public bool VisualParticle = false;
    public bool ShowGridDebugInfo = false;
    public VisualGridInfo VisualGridInfo;
    public Material VisualParticleMaterial;

    private Simulator m_Simulator;
    public ParticleData m_ParticleData;
    public Bounding m_Bounding;

    void Start()
    {
        m_Simulator = new Simulator(
            m_Terrian, 
            m_ResolutionXZ, 
            m_RegularCellYCount, 
            m_Min,
            m_CellLength, 
            m_SeaLevel, 
            m_MaxParticleCount
        );
        m_Simulator.SetupBounding(m_Bounding);
        m_Simulator.GenerateRandomVelicty();
        m_Simulator.SetupParticleDataForReconstruction(m_ParticleData);
    }

    void Update()
    {
        m_Simulator.Step(m_TimeStep);
    }

    private void OnRenderObject()
    {
        if(m_Simulator != null && VisualParticle)
            m_Simulator.VisualParticle(VisualParticleMaterial);

        if (m_Simulator != null && ShowGridDebugInfo)
        {
            if ((int)VisualGridInfo.m_ShowInfo > 1)
                VisualGridInfo.m_GridLevel = 0;
            m_Simulator.VisualGrid(VisualGridInfo);
        }
    }
}
