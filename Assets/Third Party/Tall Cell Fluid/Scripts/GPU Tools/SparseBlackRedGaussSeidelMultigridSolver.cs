using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SparseBlackRedGaussSeidelMultigridSolver
{
    public SparseBlackRedGaussSeidelMultigridSolver()
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS = Resources.Load<ComputeShader>(Common.SparseBlackRedGaussSeidelMultigridSolverCSPath);
        computeVectorB = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("computeVectorB");
        applyNopressureForce = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("applyNopressureForce");
        smooth = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("smooth");
        updateVelocity = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("updateVelocity");
    }

    public void UpdateVelocity(GridPerLevel vFineLevel, float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", vFineLevel.ResolutionXZ.x, vFineLevel.ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", vFineLevel.CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", vFineLevel.RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "TerrianHeight_R", vFineLevel.TerrainHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "TallCellHeight_R", vFineLevel.TallCellHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "RegularCellMark_R", vFineLevel.RegularCellMark);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "RegularCellPressure_R", vFineLevel.Pressure.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "TopCellPressure_R", vFineLevel.Pressure.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "BottomCellPressure_R", vFineLevel.Pressure.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "RegularCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "TopCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "BottomRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "RegularCellVelocity_RW", vFineLevel.Velocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "TopCellVelocity_RW", vFineLevel.Velocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(updateVelocity, "BottomCellVelocity_RW", vFineLevel.Velocity.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(
            updateVelocity,
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)(vFineLevel.RegularCellYCount + 2) / Common.ThreadCount3D),
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.y / Common.ThreadCount3D));
    }

    public void Smooth(ref GridPerLevel vFineLevel, GridGPUCache vCache, float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", vFineLevel.ResolutionXZ.x, vFineLevel.ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", vFineLevel.CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", vFineLevel.RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "TerrianHeight_R", vFineLevel.TerrainHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "TallCellHeight_R", vFineLevel.TallCellHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "RegularCellMark_R", vFineLevel.RegularCellMark);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "VectorB_R", vCache.VectorBForFineLevel);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "RegularCellPressure_R", vFineLevel.Pressure.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "TopCellPressure_R", vFineLevel.Pressure.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "BottomCellPressure_R", vFineLevel.Pressure.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "RegularCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "TopCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "BottomRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "RegularCellPressure_Cache_RW", vCache.SmoothPressureCache.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "TopCellPressure_Cache_RW", vCache.SmoothPressureCache.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(smooth, "BottomCellPressure_Cache_RW", vCache.SmoothPressureCache.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(
            smooth,
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)(vFineLevel.RegularCellYCount + 2) / Common.ThreadCount3D),
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.y / Common.ThreadCount3D));

        GridValuePerLevel Temp = vFineLevel.Pressure;
        vFineLevel.Pressure = vCache.SmoothPressureCache;
        vCache.SmoothPressureCache = Temp;
    }

    public void ApplyNopressureForce(GridPerLevel vFineLevel, float vGravity, float vTimeStep)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", vFineLevel.ResolutionXZ.x, vFineLevel.ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", vFineLevel.CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", vFineLevel.RegularCellYCount);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("Gravity", vGravity);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("TimeStep", vTimeStep);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "RegularCellMark_R", vFineLevel.RegularCellMark);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "RegularCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "TopCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "BottomRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "RegularCellVelocity_RW", vFineLevel.Velocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "TopCellVelocity_RW", vFineLevel.Velocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(applyNopressureForce, "BottomCellVelocity_RW", vFineLevel.Velocity.TallCellBottomValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(
            applyNopressureForce,
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)(vFineLevel.RegularCellYCount + 2) / Common.ThreadCount3D),
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.y / Common.ThreadCount3D));
    }

    public void ComputeVectorB(GridPerLevel vFineLevel, GridGPUCache vCache)
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInts("XZResolution", vFineLevel.ResolutionXZ.x, vFineLevel.ResolutionXZ.y);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetFloat("CellLength", vFineLevel.CellLength);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetInt("ConstantCellNum", vFineLevel.RegularCellYCount);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "TerrianHeight_R", vFineLevel.TerrainHeight);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "TallCellHeight_R", vFineLevel.TallCellHeight);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "RegularCellVelocity_R", vFineLevel.Velocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "TopCellVelocity_R", vFineLevel.Velocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "BottomCellVelocity_R", vFineLevel.Velocity.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "RegularCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "TopCellRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "BottomRigidBodyPercentage_R", vFineLevel.RigidBodyPercentage.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "RegularCellRigidBodyVelocity_R", vFineLevel.RigidBodyVelocity.RegularCellValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "TopCellRigidBodyVelocity_R", vFineLevel.RigidBodyVelocity.TallCellTopValue);
        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "BottomRigidBodyVelocity_R", vFineLevel.RigidBodyVelocity.TallCellBottomValue);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "RegularCellMark_R", vFineLevel.RegularCellMark);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "VectorB_RW", vCache.VectorBForFineLevel);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(
            computeVectorB,
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)(vFineLevel.RegularCellYCount + 2) / Common.ThreadCount3D),
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.y / Common.ThreadCount3D));
    }

    private ComputeShader m_SparseBlackRedGaussSeidelMultigridSolverCS;
    private int computeVectorB;
    private int applyNopressureForce;
    private int smooth;
    private int updateVelocity;
}
