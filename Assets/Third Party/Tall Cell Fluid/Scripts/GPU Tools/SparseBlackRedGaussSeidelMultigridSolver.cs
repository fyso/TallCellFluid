using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SparseBlackRedGaussSeidelMultigridSolver
{
    public SparseBlackRedGaussSeidelMultigridSolver()
    {
        m_SparseBlackRedGaussSeidelMultigridSolverCS = Resources.Load<ComputeShader>(Common.SparseBlackRedGaussSeidelMultigridSolverCSPath);
        computeVectorB = m_SparseBlackRedGaussSeidelMultigridSolverCS.FindKernel("computeVectorB");
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

        m_SparseBlackRedGaussSeidelMultigridSolverCS.SetTexture(computeVectorB, "VectorB_RW", vCache.VectorBForFineLevel);

        m_SparseBlackRedGaussSeidelMultigridSolverCS.Dispatch(
            computeVectorB,
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.x / Common.ThreadCount3D),
            Mathf.CeilToInt((float)(vFineLevel.RegularCellYCount + 2) / Common.ThreadCount3D),
            Mathf.CeilToInt((float)vFineLevel.ResolutionXZ.y / Common.ThreadCount3D));
    }

    private ComputeShader m_SparseBlackRedGaussSeidelMultigridSolverCS;
    private int computeVectorB;
}
