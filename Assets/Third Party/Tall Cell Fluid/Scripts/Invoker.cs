using UnityEngine;

[System.Serializable]
public enum Resolution
{
    FOUR = 4,
    EIGHT = 8,
    SIXTEEN = 16,
    THIRTY_TWO = 32,
    SIXTY_FOUR = 64,
    ONE_HUNDRED_TWENTY_EIGHT = 128,
}

[System.Serializable]
public struct InitializationParameter
{
    public Texture m_Terrian;
    public Vector3 m_Min;
    public Resolution m_ResolutionXZ;
    public Resolution m_RegularCellYCount;
    public float m_CellLength;
    public float m_SeaLevel;
    public int m_MaxParticleCount;
    public float m_TimeStep;
}

[System.Serializable]
public struct RuntimeParameter
{
    [Range(0.0f, 10f)]
    public int m_PCAIterationNum;
}

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
    public InitializationParameter m_InitializationParam;
    public RuntimeParameter m_RuntimeParam;

    public bool VisualParticle = false;
    public Material VisualParticleMaterial;
    public bool VisualGrid = false;
    public VisualGridInfo VisualGridInfo;

    private Simulator m_Simulator;
    public SimulatorData m_ParticleData;

    void Start()
    {
        m_Simulator = new Simulator(m_InitializationParam);
        m_Simulator.GenerateRandomVelicty();
        m_Simulator.SetupDataForReconstruction(m_ParticleData);
    }

    void Update()
    {
        m_Simulator.Step(m_InitializationParam.m_TimeStep, m_RuntimeParam);
    }

    private void OnRenderObject()
    {
        if(m_Simulator != null && VisualParticle)
            m_Simulator.VisualParticle(VisualParticleMaterial);

        if (m_Simulator != null && VisualGrid)
        {
            if ((int)VisualGridInfo.m_ShowInfo > 1)
                VisualGridInfo.m_GridLevel = 0;
            m_Simulator.VisualGrid(VisualGridInfo);
        }
    }
}
