using UnityEngine;
using UnityEngine.Profiling;

public class GridValuePerLevel
{
    public RenderTexture RegularCellValue { get { return m_RegularCellValue; } }
    public RenderTexture TallCellTopValue { get { return m_TallCellTopValue; } }
    public RenderTexture TallCellBottomValue { get { return m_TallCellBottomValue; } }

    public GridValuePerLevel(Vector2Int vResolutionXZ, int vRegularCellYCount, RenderTextureFormat vDataType)
    {
        m_RegularCellValue = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, vDataType)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellTopValue = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellBottomValue = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, vDataType)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
    }

    public void Release()
    {
        m_RegularCellValue.Release();
        m_TallCellTopValue.Release();
        m_TallCellBottomValue.Release();
    }

    private RenderTexture m_RegularCellValue;
    private RenderTexture m_TallCellTopValue;
    private RenderTexture m_TallCellBottomValue;
}

public class GridPerLevel
{
    public RenderTexture TerrainHeight { get { return m_TerrrianHeight; } }
    public RenderTexture TallCellHeight {  get { return m_TallCellHeight; } set {  m_TallCellHeight = value; } }
    public RenderTexture RegularCellMark { get { return m_RegularCellMark; } }
    public GridValuePerLevel Velocity { get { return m_Velocity; } set { m_Velocity = value; } }
    public GridValuePerLevel Pressure { get { return m_Pressure; } set { m_Pressure = value; } }
    public GridValuePerLevel RigidBodyPercentage { get { return m_RigidBodyPercentage; } }
    public GridValuePerLevel RigidBodyVelocity { get { return m_RigidBodyVelocity; } }
    public Vector2Int ResolutionXZ { get { return m_ResolutionXZ; } }
    public int RegularCellYCount { get { return m_RegularCellYCount; } }

    public float CellLength { get { return m_CellLength; } }

    public GridPerLevel(Vector2Int vResolutionXZ, int vRegularCellYCount, float vCellLength)
    {
        m_CellLength = vCellLength;
        m_ResolutionXZ = vResolutionXZ;
        m_RegularCellYCount = vRegularCellYCount;

        m_Utils = new Utils();

        m_TerrrianHeight = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_TallCellHeight = new RenderTexture(vResolutionXZ.x, vResolutionXZ.y, 0, RenderTextureFormat.RFloat)
        {
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_RegularCellMark = new RenderTexture(vResolutionXZ.x, vRegularCellYCount, 0, RenderTextureFormat.RInt)
        {
            dimension = UnityEngine.Rendering.TextureDimension.Tex3D,
            volumeDepth = vResolutionXZ.y,
            enableRandomWrite = true,
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };

        m_RigidBodyPercentage = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);

        m_Pressure = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
        m_PressureCache = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
        m_VectorB = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
        m_Residual = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.RFloat);
        m_Velocity = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat);
        m_RigidBodyVelocity = new GridValuePerLevel(vResolutionXZ, vRegularCellYCount, RenderTextureFormat.ARGBFloat);

        m_SparseBlackRedGaussSeidelMultigridSolverCS = Resources.Load<ComputeShader>(Common.SparseBlackRedGaussSeidelMultigridSolverCSPath);

        computeVectorB_Regular = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("computeVectorB_Regular");
        computeVectorB_Top = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("computeVectorB_Top");
        computeVectorB_Bottom = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("computeVectorB_Bottom");

        applyNopressureForce = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("applyNopressureForce");

        smooth_RBGS_Regular = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("smooth_RBGS_Regular");
        smooth_RBGS_Top = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("smooth_RBGS_Top");
        smooth_RBGS_Bottom = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("smooth_RBGS_Bottom");

        residual_Regular = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("residual_Regular");
        residual_Top = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("residual_Top");
        residual_Bottom = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("residual_Bottom");

        updateVelocity_Regular = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("updateVelocity_Regular");
        updateVelocity_Top = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("updateVelocity_Top");
        updateVelocity_Bottom = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("updateVelocity_Bottom");
    }

    public void Release()
    {
        m_TerrrianHeight.Release();
        m_TallCellHeight.Release();
        m_RegularCellMark.Release();
        m_Pressure.Release();
        m_PressureCache.Release();
        m_VectorB.Release();
        m_Residual.Release();
        m_RigidBodyPercentage.Release();
        m_Velocity.Release();
        m_RigidBodyVelocity.Release();
    }

    public void ApplyNopressureForce(float vGravity, float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("Gravity", vGravity);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "RegularCellMark_R", RegularCellMark);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "RegularCellRigidBodyPercentage_R", RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "TopCellRigidBodyPercentage_R", RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "BottomRigidBodyPercentage_R", RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "RegularCellVelocity_RW", Velocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "TopCellVelocity_RW", Velocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "BottomCellVelocity_RW", Velocity.TallCellBottomValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(applyNopressureForce,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)(RegularCellYCount + 2) / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D));
    }

    public void ComputeVectorB(float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", ResolutionXZ.x, ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        __SetVectorBSampleResource(computeVectorB_Regular);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB_Regular, "VectorB_Regular_RW", m_VectorB.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(computeVectorB_Regular,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)RegularCellYCount / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D));

        __SetVectorBSampleResource(computeVectorB_Top);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB_Top, "VectorB_Top_RW", m_VectorB.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(computeVectorB_Top,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);

        __SetVectorBSampleResource(computeVectorB_Bottom);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB_Bottom, "VectorB_Bottom_RW", m_VectorB.TallCellBottomValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(computeVectorB_Bottom,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);
    }

    public void SmoothRBGS(float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", ResolutionXZ.x, ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        for (int RedBlackTrigger = 0; RedBlackTrigger < 2; RedBlackTrigger++)
        {
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("RedBlackTrigger", RedBlackTrigger);

            __SetPrressureSampleResource(smooth_RBGS_Regular);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth_RBGS_Regular, "VectorB_Regular_R", m_VectorB.RegularCellValue);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth_RBGS_Regular, "RegularCellPressure_Cache_RW", m_PressureCache.RegularCellValue);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(smooth_RBGS_Regular,
                Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
                Mathf.CeilToInt((float)RegularCellYCount / Common.ThreadCount3D),
                Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D));

            __SetPrressureSampleResource(smooth_RBGS_Top);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth_RBGS_Top, "VectorB_Top_R", m_VectorB.TallCellTopValue);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth_RBGS_Top, "TopCellPressure_Cache_RW", m_PressureCache.TallCellTopValue);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(smooth_RBGS_Top,
                Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
                Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);

            __SetPrressureSampleResource(smooth_RBGS_Bottom);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth_RBGS_Bottom, "VectorB_Bottom_R", m_VectorB.TallCellBottomValue);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth_RBGS_Bottom, "BottomCellPressure_Cache_RW", m_PressureCache.TallCellBottomValue);
            m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(smooth_RBGS_Bottom,
                Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
                Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);
        }

        GridValuePerLevel Temp = Pressure;
        Pressure = m_PressureCache;
        m_PressureCache = Temp;
    }

    public void Residual(float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", ResolutionXZ.x, ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        __SetPrressureSampleResource(residual_Regular);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(residual_Regular, "VectorB_Regular_R", m_VectorB.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(residual_Regular, "Residual_Regular_RW", m_Residual.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(residual_Regular,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)RegularCellYCount / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D));

        __SetPrressureSampleResource(residual_Top);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(residual_Top, "VectorB_Top_R", m_VectorB.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(residual_Top, "Residual_Top_RW", m_Residual.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(residual_Top,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);

        __SetPrressureSampleResource(residual_Bottom);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(residual_Bottom, "VectorB_Bottom_R", m_VectorB.TallCellBottomValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(residual_Bottom, "Residual_Bottom_RW", m_Residual.TallCellBottomValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(residual_Bottom,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);
    }

    public void Restrict(GridPerLevel vHigherLevelGrid)
    {

    }

    public void ClearPressure()
    {
        m_Utils.ClearFloatTexture3D(m_Pressure.RegularCellValue);
        m_Utils.ClearFloatTexture2D(m_Pressure.TallCellBottomValue);
        m_Utils.ClearFloatTexture2D(m_Pressure.TallCellTopValue);
    }

    public void Prolong(GridPerLevel vLowerLevelGrid)
    {

    }

    public void UpdateVelocity(float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", ResolutionXZ.x, ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        __SetPrressureSampleResource(updateVelocity_Regular);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity_Regular, "RegularCellVelocity_RW", Velocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(updateVelocity_Regular,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)RegularCellYCount / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D));

        __SetPrressureSampleResource(updateVelocity_Top);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity_Top, "TopCellVelocity_RW", Velocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(updateVelocity_Top,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);

        __SetPrressureSampleResource(updateVelocity_Bottom);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity_Bottom, "BottomCellVelocity_RW", Velocity.TallCellBottomValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(updateVelocity_Bottom,
            Mathf.CeilToInt((float)ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)ResolutionXZ.y / Common.ThreadCount3D), 1);
    }

    private void __SetPrressureSampleResource(int Kernel)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellMark_R", RegularCellMark);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TerrianHeight_R", TerrainHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TallCellHeight_R", TallCellHeight);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellRigidBodyPercentage_R", RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TopCellRigidBodyPercentage_R", RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "BottomRigidBodyPercentage_R", RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellPressure_R", Pressure.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TopCellPressure_R", Pressure.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "BottomCellPressure_R", Pressure.TallCellBottomValue);
    }

    private void __SetVectorBSampleResource(int Kernel)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellMark_R", RegularCellMark);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TerrianHeight_R", TerrainHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TallCellHeight_R", TallCellHeight);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellVelocity_R", Velocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TopCellVelocity_R", Velocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "BottomCellVelocity_R", Velocity.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellRigidBodyPercentage_R", RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TopCellRigidBodyPercentage_R", RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "BottomRigidBodyPercentage_R", RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "RegularCellRigidBodyVelocity_R", RigidBodyVelocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "TopCellRigidBodyVelocity_R", RigidBodyVelocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(Kernel, "BottomRigidBodyVelocity_R", RigidBodyVelocity.TallCellBottomValue);
    }

    private Vector2Int m_ResolutionXZ;
    private int m_RegularCellYCount;
    private float m_CellLength;

    private Utils m_Utils;

    private RenderTexture m_TerrrianHeight;
    private RenderTexture m_TallCellHeight;
    private RenderTexture m_RegularCellMark;
    private GridValuePerLevel m_Velocity;
    private GridValuePerLevel m_Pressure;
    private GridValuePerLevel m_PressureCache;
    private GridValuePerLevel m_VectorB;
    private GridValuePerLevel m_Residual;
    private GridValuePerLevel m_RigidBodyPercentage;
    private GridValuePerLevel m_RigidBodyVelocity;

    private ComputeShader m_SparseBlackRedGaussSeidelMultigridSolverCS;
    private int computeVectorB_Regular;
    private int computeVectorB_Top;
    private int computeVectorB_Bottom;
    private int applyNopressureForce;
    private int smooth_RBGS_Regular;
    private int smooth_RBGS_Top;
    private int smooth_RBGS_Bottom;
    private int residual_Regular;
    private int residual_Top;
    private int residual_Bottom;
    private int updateVelocity_Regular;
    private int updateVelocity_Top;
    private int updateVelocity_Bottom;
}

public class Grid
{
    public Grid(Vector2Int vResolutionXZ, int vRegularCellYCount, float vCellLength)
    {
        m_VisualGridMaterial = Resources.Load<Material>("Materials/VisualGrid");

        m_HierarchicalLevel = (int)Mathf.Min(
            Mathf.Log(vResolutionXZ.x, 2),
            Mathf.Min(Mathf.Log(vResolutionXZ.y, 2), Mathf.Log(vRegularCellYCount, 2))) + 1;
        m_GridData = new GridPerLevel[m_HierarchicalLevel];
        for (int i = 0; i < m_HierarchicalLevel; i++)
        {
            Vector2Int LayerResolutionXZ = vResolutionXZ / (int)Mathf.Pow(2, i);
            int RegularCellYCount = vRegularCellYCount / (int)Mathf.Pow(2, i);
            float CellLength = vCellLength * Mathf.Pow(2, i);
            m_GridData[i] = new GridPerLevel(LayerResolutionXZ, RegularCellYCount, CellLength);
        }

        m_GPUCache = new GridGPUCache(vResolutionXZ, vCellLength, vRegularCellYCount);
        m_Utils = new Utils();
        __InitDownSampleTools();
        __InitRemeshTools(vResolutionXZ, vCellLength, vRegularCellYCount);
    }

    public void Release()
    {
        for(int i = 0; i < m_GridData.Length; i++)
        {
            m_GridData[i].Release();
        }
        m_GPUCache.Release();
    }

    public GridPerLevel FineGrid { get { return m_GridData[0]; } }

    public GridGPUCache GPUCache { get { return m_GPUCache; } }

    public void VisualGrid(VisualGridInfo VisualGridInfo, Vector3 vMin)
    {
        Vector2Int ResolutionXZ = m_GridData[VisualGridInfo.m_GridLevel].ResolutionXZ;
        Profiler.BeginSample("VisualGrid");

        if (VisualGridInfo.m_ShowTerrainCell)
        {
            m_VisualGridMaterial.SetPass(0);
            m_VisualGridMaterial.SetInt("UseSpecifiedShowRange", VisualGridInfo.m_UseSpecifiedShowRange ? 1: 0);
            m_VisualGridMaterial.SetInt("MinX", VisualGridInfo.m_MinX);
            m_VisualGridMaterial.SetInt("MaxX", VisualGridInfo.m_MaxX);
            m_VisualGridMaterial.SetInt("MinZ", VisualGridInfo.m_MinZ);
            m_VisualGridMaterial.SetInt("MaxZ", VisualGridInfo.m_MaxZ);
            m_VisualGridMaterial.SetVector("MinPos", vMin);
            m_VisualGridMaterial.SetFloat("CellLength", m_GridData[VisualGridInfo.m_GridLevel].CellLength);
            m_VisualGridMaterial.SetInt("ResolutionX", m_GridData[VisualGridInfo.m_GridLevel].ResolutionXZ.x);
            m_VisualGridMaterial.SetTexture("TerrainHeight", m_GridData[VisualGridInfo.m_GridLevel].TerrainHeight);
            Graphics.DrawProceduralNow(VisualGridInfo.m_ShowMode == ShowMode.Entity ? MeshTopology.Triangles : MeshTopology.Lines, 36, ResolutionXZ.x * ResolutionXZ.y);
        }

        if (VisualGridInfo.m_ShowTallCell)
        {
            m_VisualGridMaterial.SetPass(1);
            m_VisualGridMaterial.SetInt("UseSpecifiedShowRange", VisualGridInfo.m_UseSpecifiedShowRange ? 1 : 0);
            m_VisualGridMaterial.SetInt("MinX", VisualGridInfo.m_MinX);
            m_VisualGridMaterial.SetInt("MaxX", VisualGridInfo.m_MaxX);
            m_VisualGridMaterial.SetInt("MinZ", VisualGridInfo.m_MinZ);
            m_VisualGridMaterial.SetInt("MaxZ", VisualGridInfo.m_MaxZ);
            m_VisualGridMaterial.SetVector("MinPos", vMin);
            m_VisualGridMaterial.SetFloat("CellLength", m_GridData[VisualGridInfo.m_GridLevel].CellLength);
            m_VisualGridMaterial.SetInt("ResolutionX", m_GridData[VisualGridInfo.m_GridLevel].ResolutionXZ.x);
            m_VisualGridMaterial.SetTexture("TerrainHeight", m_GridData[VisualGridInfo.m_GridLevel].TerrainHeight);
            m_VisualGridMaterial.SetTexture("TallCellHeight", m_GridData[VisualGridInfo.m_GridLevel].TallCellHeight);

            m_VisualGridMaterial.SetVector("MinShowColor", new Vector4(VisualGridInfo.MinShowColor.r, VisualGridInfo.MinShowColor.g, VisualGridInfo.MinShowColor.b, VisualGridInfo.MinShowValue));
            m_VisualGridMaterial.SetVector("MaxShowColor", new Vector4(VisualGridInfo.MaxShowColor.r, VisualGridInfo.MaxShowColor.g, VisualGridInfo.MaxShowColor.b, VisualGridInfo.MaxShowValue));

            switch (VisualGridInfo.m_ShowInfo)
            {
                case ShowInfo.WaterMark:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", -1);
                    break;
                case ShowInfo.RigidBodyPercentage:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", 0);
                    m_VisualGridMaterial.SetTexture("TopShowValue", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyPercentage.TallCellTopValue);
                    m_VisualGridMaterial.SetTexture("BottomShowValue", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyPercentage.TallCellBottomValue);
                    break;
                case ShowInfo.RigidBodyVelocity:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", 1);
                    m_VisualGridMaterial.SetTexture("TopVelocity", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyVelocity.TallCellTopValue);
                    m_VisualGridMaterial.SetTexture("BottomVelocity", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyVelocity.TallCellBottomValue);
                    break;
                case ShowInfo.RigidBodySpeed:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", 2);
                    m_VisualGridMaterial.SetTexture("TopVelocity", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyVelocity.TallCellTopValue);
                    m_VisualGridMaterial.SetTexture("BottomVelocity", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyVelocity.TallCellBottomValue);
                    break;
                case ShowInfo.Velocity:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", 1);
                    m_VisualGridMaterial.SetTexture("TopVelocity", m_GridData[VisualGridInfo.m_GridLevel].Velocity.TallCellTopValue);
                    m_VisualGridMaterial.SetTexture("BottomVelocity", m_GridData[VisualGridInfo.m_GridLevel].Velocity.TallCellBottomValue);
                    break;
                case ShowInfo.Speed:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", 2);
                    m_VisualGridMaterial.SetTexture("TopVelocity", m_GridData[VisualGridInfo.m_GridLevel].Velocity.TallCellTopValue);
                    m_VisualGridMaterial.SetTexture("BottomVelocity", m_GridData[VisualGridInfo.m_GridLevel].Velocity.TallCellBottomValue);
                    break;
                case ShowInfo.Pressure:
                    m_VisualGridMaterial.SetInt("TallCellShowInfoMode", 0);
                    m_VisualGridMaterial.SetTexture("TopShowValue", m_GridData[VisualGridInfo.m_GridLevel].Pressure.TallCellTopValue);
                    m_VisualGridMaterial.SetTexture("BottomShowValue", m_GridData[VisualGridInfo.m_GridLevel].Pressure.TallCellBottomValue);
                    break;
            }
            Graphics.DrawProceduralNow(VisualGridInfo.m_ShowMode == ShowMode.Entity ? MeshTopology.Triangles : MeshTopology.Lines, 36, ResolutionXZ.x * ResolutionXZ.y);
        }

        if (VisualGridInfo.m_ShowRegularCell)
        {
            m_VisualGridMaterial.SetPass(2);
            m_VisualGridMaterial.SetVector("MinPos", vMin);
            m_VisualGridMaterial.SetInt("UseSpecifiedShowRange", VisualGridInfo.m_UseSpecifiedShowRange ? 1 : 0);
            m_VisualGridMaterial.SetInt("MinX", VisualGridInfo.m_MinX);
            m_VisualGridMaterial.SetInt("MaxX", VisualGridInfo.m_MaxX);
            m_VisualGridMaterial.SetInt("MinZ", VisualGridInfo.m_MinZ);
            m_VisualGridMaterial.SetInt("MaxZ", VisualGridInfo.m_MaxZ);
            m_VisualGridMaterial.SetFloat("CellLength", m_GridData[VisualGridInfo.m_GridLevel].CellLength);
            m_VisualGridMaterial.SetInt("ResolutionX", m_GridData[VisualGridInfo.m_GridLevel].ResolutionXZ.x);
            m_VisualGridMaterial.SetInt("ResolutionY", m_GridData[VisualGridInfo.m_GridLevel].RegularCellYCount);
            m_VisualGridMaterial.SetTexture("TerrainHeight", m_GridData[VisualGridInfo.m_GridLevel].TerrainHeight);
            m_VisualGridMaterial.SetTexture("TallCellHeight", m_GridData[VisualGridInfo.m_GridLevel].TallCellHeight);

            m_VisualGridMaterial.SetInt("ShowInfoMode", 0);
            m_VisualGridMaterial.SetVector("MinShowColor", new Vector4(VisualGridInfo.MinShowColor.r, VisualGridInfo.MinShowColor.g, VisualGridInfo.MinShowColor.b, VisualGridInfo.MinShowValue));
            m_VisualGridMaterial.SetVector("MaxShowColor", new Vector4(VisualGridInfo.MaxShowColor.r, VisualGridInfo.MaxShowColor.g, VisualGridInfo.MaxShowColor.b, VisualGridInfo.MaxShowValue));

            m_VisualGridMaterial.DisableKeyword("_SHOW_MASK");
            switch (VisualGridInfo.m_ShowInfo)
            {
                case ShowInfo.WaterMark:
                    m_VisualGridMaterial.EnableKeyword("_SHOW_MASK");
                    m_VisualGridMaterial.SetTexture("ShowValue", m_GridData[VisualGridInfo.m_GridLevel].RegularCellMark);
                    break;
                case ShowInfo.RigidBodyPercentage:
                    m_VisualGridMaterial.SetTexture("ShowValue", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyPercentage.RegularCellValue);
                    break;
                case ShowInfo.RigidBodyVelocity:
                    m_VisualGridMaterial.SetInt("ShowInfoMode", 1);
                    m_VisualGridMaterial.SetTexture("Velocity", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyVelocity.RegularCellValue);
                    break;
                case ShowInfo.RigidBodySpeed:
                    m_VisualGridMaterial.SetInt("ShowInfoMode", 2);
                    m_VisualGridMaterial.SetTexture("Velocity", m_GridData[VisualGridInfo.m_GridLevel].RigidBodyVelocity.RegularCellValue);
                    break;
                case ShowInfo.Velocity:
                    m_VisualGridMaterial.SetInt("ShowInfoMode", 1);
                    m_VisualGridMaterial.SetTexture("Velocity", m_GridData[VisualGridInfo.m_GridLevel].Velocity.RegularCellValue);
                    break;
                case ShowInfo.Speed:
                    m_VisualGridMaterial.SetInt("ShowInfoMode", 2);
                    m_VisualGridMaterial.SetTexture("Velocity", m_GridData[VisualGridInfo.m_GridLevel].Velocity.RegularCellValue);
                    break;
                case ShowInfo.Pressure:
                    m_VisualGridMaterial.SetTexture("ShowValue", m_GridData[VisualGridInfo.m_GridLevel].Pressure.RegularCellValue);
                    break;
            }

            Graphics.DrawProceduralNow(
                VisualGridInfo.m_ShowMode == ShowMode.Entity ? MeshTopology.Triangles : MeshTopology.Lines,
                36, ResolutionXZ.x * ResolutionXZ.y * m_GridData[VisualGridInfo.m_GridLevel].RegularCellYCount);
        }

        Profiler.EndSample();
    }

    public void RestCache()
    {
        m_Utils.ClearIntTexture2D(GPUCache.TallCellParticleCountCahce);
        m_Utils.ClearIntTexture3D(GPUCache.TallCellScalarCahce1);
        m_Utils.ClearIntTexture3D(GPUCache.TallCellScalarCahce2);
        m_Utils.ClearIntTexture3D(GPUCache.TallCellVectorCahce1);
        m_Utils.ClearIntTexture3D(GPUCache.TallCellVectorCahce2);

        m_Utils.ClearIntTexture3D(GPUCache.RegularCellScalarCahce);
        m_Utils.ClearIntTexture3D(GPUCache.RegularCellVectorXCache);
        m_Utils.ClearIntTexture3D(GPUCache.RegularCellVectorYCache);
        m_Utils.ClearIntTexture3D(GPUCache.RegularCellVectorZCache);
    }

    public void InitMesh(Texture vTerrian, float vSeaLevel)
    {
        __ComputeTerrianHeight(vTerrian, 40.0f);
        __DownSampleTerrainHeight();

        __ComputeH1H2WithSeaLevel(vSeaLevel);
        __ComputeTallCellHeightFromH1H2();
        __DownSampleTallCellHeight();
    }

    public void Remesh()
    {
        __SwapFineGridVelocityWithCache();
        __ComputeTallCellHeightFromH1H2();
        __DownSampleTallCellHeight();
    }

    public void UpdateGridValue()
    {
        __UpdateFineGridVelocity();
        __UpdateSolidInfos();
        __DownSampleValue();
    }

    public void SparseMultiGridRedBlackGaussSeidel(float vTimeStep, int vFullCycleIterationCount, int vVCycleIterationCount, int vSmoothIterationCount)
    {
        m_GridData[0].ApplyNopressureForce(9.8f, vTimeStep);
        m_GridData[0].ComputeVectorB(vTimeStep);

        m_GridData[0].ClearPressure();
        //for (int c = 0; c < vVCycleIterationCount; c++)
        //    __VCycle(vTimeStep, 0, vSmoothIterationCount);

        for (int c = 0; c < vSmoothIterationCount; c++)
            m_GridData[0].SmoothRBGS(vTimeStep);

        m_GridData[0].UpdateVelocity(vTimeStep);
    }

    #region LevelSet

    #endregion

    #region DownSample
    private ComputeShader m_DownsampleCS;
    private int downSampleTerrainHeight;
    private int downSampleTallCellHeight;
    private int m_DownSampleRegularCellKernelIndex;
    private int m_DownSampleTallCellKernelIndex;

    private void __InitDownSampleTools()
    {
        m_DownsampleCS = Resources.Load<ComputeShader>(Common.DownsampleToolsCSPath);
        downSampleTerrainHeight = m_DownsampleCS.FindKernel("downSampleTerrainHeight");
        downSampleTallCellHeight = m_DownsampleCS.FindKernel("downSampleTallCellHeight");
        m_DownSampleRegularCellKernelIndex = m_DownsampleCS.FindKernel("downSampleRegularCell");
        m_DownSampleTallCellKernelIndex = m_DownsampleCS.FindKernel("downSampleTallCell");
    }

    private void __DownSampleTallCellHeight(int vSrcLevel, int LeftLevel)
    {
        m_DownsampleCS.SetTexture(downSampleTallCellHeight, "SrcTex", m_GridData[vSrcLevel].TallCellHeight);
        m_DownsampleCS.SetTexture(downSampleTallCellHeight, "SrcTerrain", m_GridData[vSrcLevel].TerrainHeight);
        m_DownsampleCS.SetInts("SrcResolution", m_GridData[vSrcLevel].ResolutionXZ.x, m_GridData[vSrcLevel].ResolutionXZ.y);
        m_DownsampleCS.SetInt("NumMipLevels", LeftLevel);
        for (int i = 1; i <= 4; i++)
        {
            if (i <= LeftLevel) m_DownsampleCS.SetTexture(downSampleTallCellHeight, "OutMip" + i, m_GridData[vSrcLevel + i].TallCellHeight);
            else m_DownsampleCS.SetTexture(downSampleTallCellHeight, "OutMip" + i, m_GridData[vSrcLevel].TallCellHeight);
        }
        m_DownsampleCS.Dispatch(downSampleTallCellHeight, Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.x / 8, 1), Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.y / 8, 1), 1);
    }

    private void __DownSampleTallCellHeight()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            __DownSampleTallCellHeight(i, m_HierarchicalLevel - i - 1);
        }
    }

    private void __DownSampleTerrainHeight(int vSrcLevel, int LeftLevel)
    {
        m_DownsampleCS.SetTexture(downSampleTerrainHeight, "SrcTex", m_GridData[vSrcLevel].TerrainHeight);
        m_DownsampleCS.SetInts("SrcResolution", m_GridData[vSrcLevel].ResolutionXZ.x, m_GridData[vSrcLevel].ResolutionXZ.y);
        m_DownsampleCS.SetInt("NumMipLevels", LeftLevel);
        for (int i = 1; i <= 4; i++)
        {
            if (i <= LeftLevel) m_DownsampleCS.SetTexture(downSampleTerrainHeight, "OutMip" + i, m_GridData[vSrcLevel + i].TerrainHeight);
            else m_DownsampleCS.SetTexture(downSampleTallCellHeight, "OutMip" + i, m_GridData[vSrcLevel].TallCellHeight);
        }
        m_DownsampleCS.Dispatch(downSampleTerrainHeight, Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.x / 8, 1), Mathf.Max(m_GridData[vSrcLevel + 1].ResolutionXZ.y / 8, 1), 1);
    }
    
    private void __DownSampleTerrainHeight()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i += 4)
        {
            __DownSampleTerrainHeight(i, m_HierarchicalLevel - i - 1);
        }
    }

    private void __DownSampleValue()
    {
        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            if (i < 4) m_DownsampleCS.SetInt("SaveMoreAir", 1);
            else m_DownsampleCS.SetInt("SaveMoreAir", 0);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "NextLevelTerrainHeight", m_GridData[i + 1].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "NextLevelTallCellHeight", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "TerrainHeight", m_GridData[i].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "TallCellHeight", m_GridData[i].TallCellHeight);
            m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "SrcRegularMark", m_GridData[i].RegularCellMark);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "SrcRegularCellRigidBodyPercentage", m_GridData[i].RigidBodyPercentage.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "OutRegularMark", m_GridData[i + 1].RegularCellMark);
            m_DownsampleCS.SetTexture(m_DownSampleRegularCellKernelIndex, "OutRegularCellRigidBodyPercentage", m_GridData[i + 1].RigidBodyPercentage.RegularCellValue);
            m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
            m_DownsampleCS.Dispatch(m_DownSampleRegularCellKernelIndex,
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 4, 1),
                Mathf.Max(m_GridData[i + 1].RegularCellYCount / 4, 1),
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 4, 1));
        }

        for (int i = 0; i < m_HierarchicalLevel - 1; i++)
        {
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTerrainHeight", m_GridData[i + 1].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "NextLevelTallCellHeight", m_GridData[i + 1].TallCellHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "TerrainHeight", m_GridData[i].TerrainHeight);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "TallCellHeight", m_GridData[i].TallCellHeight);
            m_DownsampleCS.SetFloat("SrcRegularCellLength", m_GridData[i].CellLength);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcRegularCellRigidBodyPercentage", m_GridData[i].RigidBodyPercentage.RegularCellValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellTop", m_GridData[i].RigidBodyPercentage.TallCellTopValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "SrcTallCellBottom", m_GridData[i].RigidBodyPercentage.TallCellBottomValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellTop", m_GridData[i + 1].RigidBodyPercentage.TallCellTopValue);
            m_DownsampleCS.SetTexture(m_DownSampleTallCellKernelIndex, "OutTallCellBottom", m_GridData[i + 1].RigidBodyPercentage.TallCellBottomValue);
            m_DownsampleCS.SetInts("OutResolution", m_GridData[i + 1].ResolutionXZ.x, m_GridData[i + 1].RegularCellYCount, m_GridData[i + 1].ResolutionXZ.y);
            m_DownsampleCS.Dispatch(m_DownSampleTallCellKernelIndex,
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.x / 8, 1),
                Mathf.Max(m_GridData[i + 1].ResolutionXZ.y / 8, 1),
                1);
        }
    }
    #endregion

    #region Remesh
    private ComputeShader m_RemeshToolsCS;
    private int computeTerrianHeight;
    private int computeH1H2WithSeaLevel;
    private int computeTallCellHeight;
    private int smoothTallCellHeight;
    private int enforceDCondition;
    private int subTerrianHeight;
    private int updateRegularCellVelocity;
    private int updateTallCellVelocity;
    private int updateRegularCellSolidInfos;
    private int updateTallCellTopSolidInfos;
    private int updateTallCellBottomSolidInfos;

    private void __InitRemeshTools(Vector2Int vResolutionXZ, float vCellLength, int vRegularCellYCount)
    {
        m_RemeshToolsCS = Resources.Load<ComputeShader>(Common.RemeshToolsCSPath);
        computeTerrianHeight = m_RemeshToolsCS.FindKernel("computeTerrianHeight");
        computeH1H2WithSeaLevel = m_RemeshToolsCS.FindKernel("computeH1H2WithSeaLevel");
        computeTallCellHeight = m_RemeshToolsCS.FindKernel("computeTallCellHeight");
        smoothTallCellHeight = m_RemeshToolsCS.FindKernel("smoothTallCellHeight");
        enforceDCondition = m_RemeshToolsCS.FindKernel("enforceDCondition");
        subTerrianHeight = m_RemeshToolsCS.FindKernel("subTerrianHeight");
        updateRegularCellVelocity = m_RemeshToolsCS.FindKernel("updateRegularCellVelocity");
        updateTallCellVelocity = m_RemeshToolsCS.FindKernel("updateTallCellVelocity");
        updateRegularCellSolidInfos = m_RemeshToolsCS.FindKernel("updateRegularCellSolidInfos");
        updateTallCellTopSolidInfos = m_RemeshToolsCS.FindKernel("updateTallCellTopSolidInfos");
        updateTallCellBottomSolidInfos = m_RemeshToolsCS.FindKernel("updateTallCellBottomSolidInfos");

        m_RemeshToolsCS.SetInts("XZResolution", vResolutionXZ.x, vResolutionXZ.y);
        m_RemeshToolsCS.SetFloat("CellLength", vCellLength);
        m_RemeshToolsCS.SetInt("ConstantCellNum", vRegularCellYCount);
    }

    public void __ComputeTerrianHeight(Texture vTerrian, float vHeightScale = 1.0f)
    {
        m_RemeshToolsCS.SetFloat("HeightScale", vHeightScale);
        m_RemeshToolsCS.SetTexture(computeTerrianHeight, "TerrianTexture_R", vTerrian);
        m_RemeshToolsCS.SetTexture(computeTerrianHeight, "TerrianHeight_RW", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.Dispatch(computeTerrianHeight, 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D), 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);
    }

    public void __ComputeH1H2WithSeaLevel(float vSeaLevel = 0)
    {
        m_RemeshToolsCS.SetFloat("SeaLevel", vSeaLevel);
        m_RemeshToolsCS.SetTexture(computeH1H2WithSeaLevel, "TerrianHeight_R", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.SetTexture(computeH1H2WithSeaLevel, "WaterSurfaceH1H2_RW", m_GPUCache.H1H2Cahce);
        m_RemeshToolsCS.Dispatch(computeH1H2WithSeaLevel, 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D), 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);
    }

    private void __ComputeTallCellHeightFromH1H2()
    {

        m_RemeshToolsCS.SetInt("GridAbove", m_GridAbove);
        m_RemeshToolsCS.SetInt("GridLow", m_GridLow);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "TerrianHeight_R", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "WaterSurfaceH1H2_R", m_GPUCache.H1H2Cahce);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "TallCellHeightMaxMin_RW", m_GPUCache.MaxMinCahce);
        m_RemeshToolsCS.SetTexture(computeTallCellHeight, "TallCellHeight_RW", m_GridData[0].TallCellHeight);
        m_RemeshToolsCS.Dispatch(computeTallCellHeight, 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D), 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);

        for (int i = 0; i < 9; i++)
        {
            m_RemeshToolsCS.SetFloat("BlurSigma", m_BlurSigma);
            m_RemeshToolsCS.SetFloat("BlurRadius", m_BlurRadius);
            m_RemeshToolsCS.SetTexture(smoothTallCellHeight, "TallCellHeightMaxMin_R", m_GPUCache.MaxMinCahce);
            m_RemeshToolsCS.SetTexture(smoothTallCellHeight, "TallCellHeight_R", m_GridData[0].TallCellHeight);
            m_RemeshToolsCS.SetTexture(smoothTallCellHeight, "TallCellHeightCache_RW", m_GPUCache.BackTallCellHeightCahce);
            m_RemeshToolsCS.Dispatch(smoothTallCellHeight, 
                Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D), 
                Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);

            RenderTexture Temp = m_GPUCache.BackTallCellHeightCahce;
            m_GPUCache.BackTallCellHeightCahce = m_GridData[0].TallCellHeight;
            m_GridData[0].TallCellHeight = Temp;
        }

        for(int i = 0; i < 9; i++)
        {
            m_RemeshToolsCS.SetInt("D", m_D);
            m_RemeshToolsCS.SetTexture(enforceDCondition, "TerrianHeight_R", FineGrid.TerrainHeight);
            m_RemeshToolsCS.SetTexture(enforceDCondition, "TallCellHeight_R", m_GridData[0].TallCellHeight);
            m_RemeshToolsCS.SetTexture(enforceDCondition, "TallCellHeightCache_RW", m_GPUCache.BackTallCellHeightCahce);
            m_RemeshToolsCS.Dispatch(enforceDCondition, 
                Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D), 
                Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);

            RenderTexture Temp = m_GPUCache.BackTallCellHeightCahce;
            m_GPUCache.BackTallCellHeightCahce = m_GridData[0].TallCellHeight;
            m_GridData[0].TallCellHeight = Temp;
        }

        m_RemeshToolsCS.SetTexture(subTerrianHeight, "TerrianHeight_R", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.SetTexture(subTerrianHeight, "TallCellHeight_RW", m_GridData[0].TallCellHeight);
        m_RemeshToolsCS.Dispatch(subTerrianHeight, 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D), 
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);
    }

    public void __UpdateFineGridVelocity()
    {
        m_RemeshToolsCS.SetInt("RegularCellYCount", m_GridData[0].Velocity.RegularCellValue.height);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcTallCellHeight", m_GPUCache.LastFrameTallCellHeightCache);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "TallCellHeight", m_GridData[0].TallCellHeight);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcRegularCellVelocity", m_GPUCache.LastFrameVelocityCache.RegularCellValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcTallCellTopVelocity", m_GPUCache.LastFrameVelocityCache.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "SrcTallCellBottomVelocity", m_GPUCache.LastFrameVelocityCache.TallCellBottomValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellVelocity, "RegularCellVelocity", m_GridData[0].Velocity.RegularCellValue);
        m_RemeshToolsCS.Dispatch(updateRegularCellVelocity,
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)m_GridData[0].RegularCellYCount / Common.ThreadCount3D),
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount3D));

        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcTallCellHeight", m_GPUCache.LastFrameTallCellHeightCache);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TallCellHeight", m_GridData[0].TallCellHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcRegularCellVelocity", m_GPUCache.LastFrameVelocityCache.RegularCellValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcTallCellTopVelocity", m_GPUCache.LastFrameVelocityCache.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "SrcTallCellBottomVelocity", m_GPUCache.LastFrameVelocityCache.TallCellBottomValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TallCellTopVelocity", m_GridData[0].Velocity.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateTallCellVelocity, "TallCellBottomVelocity", m_GridData[0].Velocity.TallCellBottomValue);
        m_RemeshToolsCS.Dispatch(updateTallCellVelocity,
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D),
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);
    }

    public void __UpdateSolidInfos()
    {
        if (!GameObject.FindGameObjectsWithTag("Simulator")[0].GetComponent<RigidBodyDataManager>().hasRigidBody()) return;

        GameObject.FindGameObjectsWithTag("Simulator")[0].GetComponent<RigidBodyDataManager>().UploadRigidBodyDataToGPU(m_RemeshToolsCS, updateRegularCellSolidInfos);
        m_RemeshToolsCS.SetTexture(updateRegularCellSolidInfos, "TerrianHeight_R", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.SetTexture(updateRegularCellSolidInfos, "TallCellHeight_R", m_GridData[0].TallCellHeight);
        m_RemeshToolsCS.SetTexture(updateRegularCellSolidInfos, "OutRegularCellRigidBodyPercentage", m_GridData[0].RigidBodyPercentage.RegularCellValue);
        m_RemeshToolsCS.SetTexture(updateRegularCellSolidInfos, "OutRegularCellRigidbodyVelocity", m_GridData[0].RigidBodyVelocity.RegularCellValue);
        m_RemeshToolsCS.Dispatch(updateRegularCellSolidInfos,
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)m_GridData[0].RegularCellYCount / Common.ThreadCount3D),
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount3D));

        GameObject.FindGameObjectsWithTag("Simulator")[0].GetComponent<RigidBodyDataManager>().UploadRigidBodyDataToGPU(m_RemeshToolsCS, updateTallCellTopSolidInfos);
        m_RemeshToolsCS.SetTexture(updateTallCellTopSolidInfos, "TerrianHeight_R", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellTopSolidInfos, "TallCellHeight_R", m_GridData[0].TallCellHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellTopSolidInfos, "OutTallCellTopRigidBodyPercentage", m_GridData[0].RigidBodyPercentage.TallCellTopValue);
        m_RemeshToolsCS.SetTexture(updateTallCellTopSolidInfos, "OutTallCellTopRigidbodyVelocity", m_GridData[0].RigidBodyVelocity.TallCellTopValue);
        m_RemeshToolsCS.Dispatch(updateTallCellTopSolidInfos,
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D),
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);

        GameObject.FindGameObjectsWithTag("Simulator")[0].GetComponent<RigidBodyDataManager>().UploadRigidBodyDataToGPU(m_RemeshToolsCS, updateTallCellBottomSolidInfos);
        m_RemeshToolsCS.EnableKeyword("_RIGIDBODY_FLAG");
        m_RemeshToolsCS.SetTexture(updateTallCellBottomSolidInfos, "TerrianHeight_R", m_GridData[0].TerrainHeight);
        m_RemeshToolsCS.SetTexture(updateTallCellBottomSolidInfos, "OutTallCellBottomRigidBodyPercentage", m_GridData[0].RigidBodyPercentage.TallCellBottomValue);
        m_RemeshToolsCS.SetTexture(updateTallCellBottomSolidInfos, "OutTallCellBottomRigidbodyVelocity", m_GridData[0].RigidBodyVelocity.TallCellBottomValue);
        m_RemeshToolsCS.Dispatch(updateTallCellBottomSolidInfos,
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.x / Common.ThreadCount2D),
            Mathf.CeilToInt((float)m_GridData[0].ResolutionXZ.y / Common.ThreadCount2D), 1);
        m_RemeshToolsCS.DisableKeyword("_RIGIDBODY_FLAG");
    }

    #endregion

    #region LinerSolver

    private void __VCycle(float vTimeStep, int vCurrLevel, int vIterationCount)
    {
        if (vCurrLevel == m_HierarchicalLevel)
        {
            m_GridData[vCurrLevel].SmoothRBGS(vTimeStep);
            return;
        }
        else
        {
            for (int c = 0; c < vIterationCount; c++)
                m_GridData[vCurrLevel].SmoothRBGS(vTimeStep);

            m_GridData[vCurrLevel].Residual(vTimeStep);

            m_GridData[vCurrLevel - 1].Restrict(m_GridData[vCurrLevel]);
            m_GridData[vCurrLevel - 1].ClearPressure();
            __VCycle(vTimeStep, vCurrLevel + 1, vIterationCount);

            m_GridData[vCurrLevel].Prolong(m_GridData[vCurrLevel - 1]);

            for (int c = 0; c < vIterationCount; c++)
                m_GridData[vCurrLevel].SmoothRBGS(vTimeStep);
        }
    }

    private void __FullCycle()
    {

    }

    #endregion

    private void __SwapFineGridVelocityWithCache()
    {
        RenderTexture LastFrameTallCellHeightCache = m_GPUCache.LastFrameTallCellHeightCache;
        GridValuePerLevel LastFrameVelocityCache = m_GPUCache.LastFrameVelocityCache;

        m_GPUCache.LastFrameTallCellHeightCache = FineGrid.TallCellHeight;
        m_GPUCache.LastFrameVelocityCache = FineGrid.Velocity;
        FineGrid.TallCellHeight = LastFrameTallCellHeightCache;
        FineGrid.Velocity = LastFrameVelocityCache;
    }

    private int m_HierarchicalLevel; 
    private int m_GridAbove = 8; 
    private int m_GridLow = 8;
    private int m_D = 6; 
    private float m_BlurSigma = 1.5f;  
    private float m_BlurRadius = 3.0f;
    private GridPerLevel[] m_GridData;
    private Utils m_Utils;

    private GridGPUCache m_GPUCache;

    private Material m_VisualGridMaterial;
}
